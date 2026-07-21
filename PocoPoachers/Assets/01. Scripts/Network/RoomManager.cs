using Google.FlatBuffers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : Singleton<RoomManager>
{
    [Header("STUN")]
    [SerializeField] string stunHost = "stun.l.google.com";
    [SerializeField] int    stunPort = 19302;

    public static bool IsHost => Instance != null && Instance._isHost;
    public static bool HasGuests => Instance != null && Instance._hasGuests;
    public static int CurrentUdpSenderId => Instance != null ? Instance._udpSenderId : 0;

    public bool _hasGuests => _guests.Count > 0;
    private bool _isHost = true;
    int _udpSenderId;

    public string SessionCode { get; set; }

    public event Action          OnGameStarted;
    public event Action<string>  OnSessionCodeReceived;
    public event Action<int>     OnRoomJoined;
    public event Action<string>  OnRoomJoinFailed;
    public event Action<string>  OnSyncFailed;
    public static event Action<int> OnGuestLeft;
    public static event Action      OnHostLeft;
    public static event Action<int> OnPlayerCountChanged;

    private int _memberCount = 1;
    public static int MemberCount =>
        Instance == null      ? 1 :
        Instance._isHost      ? Instance._guests.Count + 1 :
                                Instance._memberCount;

    public void NotifyGameStarted()          => OnGameStarted?.Invoke();
    public void NotifySessionCodeReceived(string code) => OnSessionCodeReceived?.Invoke(code);

    UdpSession _udpSession;
    UdpReliable _reliable;
    IPEndPoint _myPublicEp;
    IPEndPoint _hostEp;

    readonly ConcurrentDictionary<int, IPEndPoint>     _guests        = new();
    readonly ConcurrentDictionary<int, UdpHolePuncher> _punchers      = new();
    readonly ConcurrentDictionary<int, long>           _guestLastSeen = new();
    readonly ConcurrentDictionary<int, NetInfoT>       _waitingGuests = new();
    long _hostLastSeen;

    const long TIMEOUT_MS = 30_000L;
    const long KEEPALIVE_INTERVAL_MS = 5_000L;
    long _lastKeepaliveSent;

    // 씬 준비가 끝난 뒤 월드 오브젝트(박스/적) 스냅샷을 기다리는 게스트 목록
    readonly HashSet<int> _sceneReadyGuests = new();
    bool _worldObjectsReady;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        OnGameStarted += LoadShelterIfOnTitle;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void LoadShelterIfOnTitle()
    {
        if (SceneManager.GetActiveScene().name != SceneName.Title) return;

        GameManager.Instance?.SetSpawnId(SpawnId.FromTitle);
        SceneLoader.Instance?.LoadShelterScene();
    }

    public void StartAsHost() => CreateOrJoinRoom(true, null);
    public void StartAsGuest(string code) => CreateOrJoinRoom(false, code);

    // TEMP: 마스터 서버 없이 로컬에서 즉시 호스트로 시작
    public void StartLocalHost()
    {
        _isHost = true;
        _guests.Clear();
        _punchers.Clear();
        _guestLastSeen.Clear();
        _waitingGuests.Clear();
        _sceneReadyGuests.Clear();
        _hostLastSeen = 0;
        NotifyGameStarted();
    }

    private void CreateOrJoinRoom(bool isHost, string code)
    {
        CloseUdpSession();

        _isHost = isHost;
        _memberCount = 1;
        _guests.Clear();
        _guestLastSeen.Clear();
        _waitingGuests.Clear();
        _sceneReadyGuests.Clear();
        _hostLastSeen = 0;
        _hostEp = null;
        _worldObjectsReady = IsGameplayScene(SceneManager.GetActiveScene().name);

        ThreadPool.QueueUserWorkItem(_ => {
            _udpSession = new UdpSession();
            _udpSession.OnReceived += OnUdpReceived;
            _udpSession.OnKeepaliveReceived += OnUdpKeepaliveReceived;

            if (!_udpSession.Bind())
            {
                HandleFailure("network.init_failed_message");
                return;
            }
            if (!StunClient.TryGetPublicEndPoint(stunHost, stunPort, _udpSession.Socket, out _myPublicEp))
            {
                HandleFailure("network.internet_failed_message");
                return;
            }
            _udpSession.StartReceive();
            _reliable = new UdpReliable(_udpSession);
            _reliable.OnDeliveryFailed += OnReliableDeliveryFailed;
            var myInfo = GetMySessionInfo();
            MainThreadDispatcher.Enqueue(() => {
                if (_isHost)
                    PacketBuilder.SendToMaster(new C_CreateRoomT { MyInfo = myInfo }, C_CreateRoom.Pack, PacketType.C_CreateRoom);
                else
                    PacketBuilder.SendToMaster(new C_JoinRoomT { SessionCode = code, MyInfo = myInfo }, C_JoinRoom.Pack, PacketType.C_JoinRoom);
            });
        });
    }

    public void StartUdpPunch(NetInfoT target)
    {
        if (_udpSession == null)
        {
            HandleFailure("network.init_not_ready_message");
            return;
        }

        int id = target.PlayerId;
        IPEndPoint ep = SelectEndPoint(target);

        if (_isHost)
            RegisterWaitingGuest(target);

        if (!_isHost)
        {
            _hostEp = ep;
            _hostLastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        var puncher = new UdpHolePuncher(_udpSession);
        _punchers[id] = puncher;

        puncher.OnSuccess += _ => {
            _guests[id] = ep;
            _guestLastSeen[id] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _punchers.TryRemove(id, out UdpHolePuncher _);
            _waitingGuests.TryRemove(id, out NetInfoT _);
            MainThreadDispatcher.Enqueue(() => {
                if (_isHost)
                {
                    OnPlayerCountChanged?.Invoke(_guests.Count + 1);
                    OnRoomJoined?.Invoke(id);
                    SpawnGuestPlayer(id);
                    SendWorldStateToGuest(id);
                }
                else
                {
                    OnGameStarted?.Invoke();
                    // 이미 게임 씬에 있으면 씬 로드 이벤트가 없으므로 즉시 준비 완료를 알린다
                    if (IsGameplayScene(SceneManager.GetActiveScene().name))
                        RoomSync.SceneReady();
                }
            });
        };
        puncher.OnFailed += (_, reason) => {
            _punchers.TryRemove(id, out _);
            if (_isHost)
            {
                // 호스트: 펀칭 타임아웃은 치명적 실패가 아님. G_Move 수신 시 자동 등록.
                Debug.LogWarning($"[RoomManager] Punch failed for {id}: {reason}");
                return;
            }

            MainThreadDispatcher.Enqueue(() =>
                HandleFailure("network.udp_punch_failed_message"));
        };
        puncher.Start(ep);
    }

    public void UdpSendToHost<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
    {
        if (_udpSession == null || _hostEp == null) return;
        _udpSession.Send(PacketBuilder.BuildSegment(data, pack, type), _hostEp);
    }

    public void UdpSendReliableToHost<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
    {
        if (_reliable == null || _hostEp == null) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _reliable.Send(PacketBuilder.BuildSegment(data, pack, type), _hostEp, now, type);
    }

    public void UdpBroadcastToGuests<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type, int skipPlayerId = -1)
        where TTable : struct where TObj : class
    {
        if (_guests.IsEmpty || _udpSession == null) return;
        var segment = PacketBuilder.BuildSegment(data, pack, type);
        foreach (var kv in _guests)
        {
            if (kv.Key == skipPlayerId) continue;
            _udpSession.Send(segment, kv.Value);
        }
    }

    public void UdpSendToGuest<TTable, TObj>(int playerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
    {
        if (_udpSession == null || !_guests.TryGetValue(playerId, out var ep)) return;
        _udpSession.Send(PacketBuilder.BuildSegment(data, pack, type), ep);
    }

    public void UdpSendReliableToGuest<TTable, TObj>(int playerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
    {
        if (_reliable == null || !_guests.TryGetValue(playerId, out var ep)) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _reliable.Send(PacketBuilder.BuildSegment(data, pack, type), ep, now, type);
    }

    public void UdpBroadcastReliableToGuests<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type, int skipPlayerId = -1)
        where TTable : struct where TObj : class
    {
        if (_guests.IsEmpty || _reliable == null) return;
        var segment = PacketBuilder.BuildSegment(data, pack, type);
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var kv in _guests)
        {
            if (kv.Key == skipPlayerId) continue;
            _reliable.Send(segment, kv.Value, now, type);
        }
    }

    public void HandleReliablePacket(ArraySegment<byte> data, IPEndPoint sender)
    {
        int guestId = 0;
        foreach (var kv in _guests)
            if (kv.Value.Equals(sender)) { guestId = kv.Key; break; }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_isHost && guestId != 0)
            _guestLastSeen[guestId] = now;
        else if (!_isHost && _hostEp != null && sender.Equals(_hostEp))
            _hostLastSeen = now;

        int capturedGuestId = guestId;
        IPEndPoint capturedEp = sender;
        MainThreadDispatcher.Enqueue(() =>
        {
            _udpSenderId = capturedGuestId;
            _currentSenderEndPoint = capturedEp;
            PacketManager.HandlePacket(data);
            _udpSenderId = 0;
            _currentSenderEndPoint = null;
        });
    }

    void OnUdpKeepaliveReceived(IPEndPoint sender)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_isHost)
        {
            foreach (var kv in _guests)
            {
                if (!kv.Value.Equals(sender)) continue;
                _guestLastSeen[kv.Key] = now;
                return;
            }
        }
        else if (_hostEp != null && _hostEp.Equals(sender))
        {
            _hostLastSeen = now;
        }
    }

    private void OnUdpReceived(ArraySegment<byte> data, IPEndPoint sender)
    {
        int guestId = 0;
        foreach (var kv in _guests)
            if (kv.Value.Equals(sender)) { guestId = kv.Key; break; }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_isHost && guestId != 0)
            _guestLastSeen[guestId] = now;
        else if (!_isHost && _hostEp != null && sender.Equals(_hostEp))
            _hostLastSeen = now;

        int capturedGuestId = guestId;
        IPEndPoint capturedEp = sender;
        MainThreadDispatcher.Enqueue(() =>
        {
            _udpSenderId = capturedGuestId;
            _currentSenderEndPoint = capturedEp;
            PacketManager.HandlePacket(data);
            _udpSenderId = 0;
            _currentSenderEndPoint = null;
        });
    }

    public static bool TryGetGuestIdFromPacket(int playerIdInPacket, bool autoRegister, out int guestId)
    {
        guestId = 0;
        if (!IsHost || Instance == null) return false;

        int sender = CurrentUdpSenderId;
        if (sender > 0)
        {
            if (playerIdInPacket > 0 && playerIdInPacket != sender)
                return false;
            guestId = sender;
            return true;
        }

        if (!autoRegister || playerIdInPacket <= 0)
            return false;

        if (!Instance.TryRegisterLateGuest(playerIdInPacket))
            return false;

        guestId = playerIdInPacket;
        return true;
    }

    IPEndPoint _currentSenderEndPoint;
    public static IPEndPoint CurrentSenderEndPoint => Instance?._currentSenderEndPoint;

    public void RegisterWaitingGuest(NetInfoT info)
    {
        if (info == null || info.PlayerId == 0) return;
        _waitingGuests[info.PlayerId] = info;
    }

    // 펀칭 타이밍 미스로 _guests에 없는 게스트가 게임 패킷을 보낼 때 자동 등록
    public bool TryRegisterLateGuest(int playerId)
    {
        if (!_isHost || _guests.ContainsKey(playerId) || _currentSenderEndPoint == null) return false;
        if (!_waitingGuests.TryGetValue(playerId, out var expected) || !MatchesNetInfo(expected, _currentSenderEndPoint))
            return false;

        _guests[playerId] = _currentSenderEndPoint;
        _guestLastSeen[playerId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _waitingGuests.TryRemove(playerId, out NetInfoT _);
        if (_punchers.TryRemove(playerId, out UdpHolePuncher puncher))
            puncher.Stop();
        OnPlayerCountChanged?.Invoke(_guests.Count + 1);
        OnRoomJoined?.Invoke(playerId);
        SpawnGuestPlayer(playerId);
        SendWorldStateToGuest(playerId);
        return true;
    }

    void SpawnGuestPlayer(int guestId)
    {
        if (!_isHost) return;

        var objectManager = ObjectManager.Instance;
        if (objectManager == null || objectManager.TryGet(ObjectKind.Player, guestId, out _)) return;

        var localT = PlayerMovement.LocalTransform;
        Vector3 pos = localT != null ? localT.position : Vector3.zero;
        float yaw = localT != null ? localT.eulerAngles.y : 0f;
        objectManager.QueueMove(ObjectKind.Player, guestId, pos, yaw, 0);
    }

    public void SetMemberCount(int count)
    {
        _memberCount = count;
        OnPlayerCountChanged?.Invoke(_memberCount);
    }

    public void AddMember()
    {
        _memberCount++;
        OnPlayerCountChanged?.Invoke(_memberCount);
    }

    public void RemoveMember()
    {
        if (_memberCount > 1) _memberCount--;
        OnPlayerCountChanged?.Invoke(_memberCount);
    }

    private IPEndPoint SelectEndPoint(NetInfoT info)
    {
        if (!string.IsNullOrEmpty(info.PrivateIp) && info.PrivatePort != 0 && IsOnSameLan(info.PrivateIp))
            return new IPEndPoint(IPAddress.Parse(info.PrivateIp), info.PrivatePort);
        return new IPEndPoint(IPAddress.Parse(info.PublicIp), info.PublicPort);
    }

    static bool MatchesNetInfo(NetInfoT info, IPEndPoint ep)
    {
        if (info == null || ep == null) return false;
        try
        {
            if (!string.IsNullOrEmpty(info.PublicIp) && info.PublicPort != 0)
            {
                var pub = new IPEndPoint(IPAddress.Parse(info.PublicIp), info.PublicPort);
                if (pub.Address.Equals(ep.Address) && pub.Port == ep.Port) return true;
            }
            if (!string.IsNullOrEmpty(info.PrivateIp) && info.PrivatePort != 0)
            {
                var priv = new IPEndPoint(IPAddress.Parse(info.PrivateIp), info.PrivatePort);
                if (priv.Address.Equals(ep.Address) && priv.Port == ep.Port) return true;
            }
        }
        catch (FormatException) { }
        return false;
    }

    private bool IsOnSameLan(string otherPrivateIp)
    {
        string myIp = GetLocalPrivateIp();
        var my = myIp.Split('.');
        var other = otherPrivateIp.Split('.');
        if (my.Length < 3 || other.Length < 3) return false;
        return my[0] == other[0] && my[1] == other[1] && my[2] == other[2];
    }

    private string GetLocalPrivateIp()
    {
        using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        s.Connect("8.8.8.8", 65530);
        return (s.LocalEndPoint as IPEndPoint).Address.ToString();
    }

    public NetInfoT GetMySessionInfo() => new NetInfoT
    {
        PlayerId   = NetworkManager.Instance.MyPlayerId,
        PublicIp   = _myPublicEp.Address.ToString(),
        PublicPort = (ushort)_myPublicEp.Port,
        PrivateIp  = GetLocalPrivateIp(),
        PrivatePort = (ushort)_udpSession.LocalEndPoint.Port,
    };

    private void SendWorldStateToGuest(int newGuestId)
    {
        var objectManager = ObjectManager.Instance;
        if (objectManager == null) return;

        var infos = objectManager?.GetAllPlayerInfos(newGuestId) ?? new List<PlayerInfoT>();

        var localT = PlayerMovement.LocalTransform;
        if (localT != null)
        {
            var pos = localT.position;
            infos.Add(new PlayerInfoT
            {
                PlayerId = NetworkManager.Instance.MyPlayerId,
                Pos = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
                Rotation = localT.eulerAngles.y,
            });
        }

        if (infos.Count > 0)
            PacketBuilder.SendToGuest(newGuestId, new H_GuestJoinedT { Info = infos }, H_GuestJoined.Pack, PacketType.H_GuestJoined);

        PacketBuilder.BroadcastToGuests(newGuestId, new H_GuestJoinedT
        {
            Info = new List<PlayerInfoT> { new PlayerInfoT { PlayerId = newGuestId } }
        }, H_GuestJoined.Pack, PacketType.H_GuestJoined);

        SendHostEquipToGuest(newGuestId);

        var shelterMgr = ShelterManager.GetInstance();
        if (shelterMgr != null)
            PacketBuilder.SendToGuest(newGuestId,
                new H_ShelterLevelT { Level = shelterMgr.CurrentLevel },
                H_ShelterLevel.Pack, PacketType.H_ShelterLevel);

        SendAllPlayerStatsToGuest(newGuestId);
    }

    static bool IsGameplayScene(string sceneName) =>
        sceneName == SceneName.Shelter || sceneName.StartsWith("SC_Raid_");

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 레이드 진입 시 통계 시작 / 그 외 씬이면 멈춤 (각 클라이언트가 자기 진행시간을 집계)
        if (scene.name.StartsWith("SC_Raid_"))
            RaidStats.Instance.StartRaid();
        else
            RaidStats.Instance.StopRaid();

        if (!IsGameplayScene(scene.name))
        {
            _worldObjectsReady = false;
            return;
        }

        if (_isHost)
            StartCoroutine(MarkWorldObjectsReady());
        else if (_hostEp != null)
            RoomSync.SceneReady();
    }

    // 스포너(ItemSpawner/EnemySpawner)의 Start가 실행된 다음 프레임에 스냅샷 전송 가능 상태로 전환
    IEnumerator MarkWorldObjectsReady()
    {
        yield return null;
        _worldObjectsReady = true;
        foreach (var guestId in _sceneReadyGuests)
            SendWorldObjectsToGuest(guestId);
        _sceneReadyGuests.Clear();
    }

    // 게스트가 씬 로드를 마쳤다고 알려온 시점에 호출. 호스트 자신이 아직 스폰 전이면 대기시킨다.
    public void HandleGuestSceneReady(int guestId)
    {
        if (!_isHost) return;

        // 씬 전환 때는 게스트 입장 경로(SendWorldStateToGuest)를 타지 않으므로 장비를 여기서 다시 보낸다
        SendHostEquipToGuest(guestId);

        // 방 세계에 저장된 이 게스트의 상태를 복원 전송한다 (없으면 빈손 유지)
        SendGuestRoomRestore(guestId);

        if (_worldObjectsReady)
            SendWorldObjectsToGuest(guestId);
        else
            _sceneReadyGuests.Add(guestId);
    }

    void SendWorldObjectsToGuest(int guestId)
    {
        EnemyNetSync.SendAllToGuest(guestId);

        var objectManager = ObjectManager.Instance;
        if (objectManager == null) return;

        foreach (var original in objectManager.SpawnedBoxes)
        {
            var currentItemIds   = new List<int>();
            var currentItemCounts = new List<int>();
            var currentItemUids  = new List<int>();
            var currentItemSlots = new List<int>();
            if (objectManager.TryGet(ObjectKind.ItemBox, original.Uid, out var boxObj))
            {
                var inv = boxObj.GetComponent<Inventory>();
                if (inv != null)
                    for (int i = 0; i < inv.Slots.Count; i++)
                    {
                        var slot = inv.Slots[i];
                        if (slot.IsEmpty) continue;
                        currentItemIds.Add(slot.ItemData.Id);
                        currentItemCounts.Add(slot.Amount);
                        currentItemUids.Add(slot.Uid);
                        currentItemSlots.Add(i);
                    }
            }
            PacketBuilder.SendReliableToGuest(guestId, new H_ItemSpawnT
            {
                Uid       = original.Uid,
                TypeId    = original.TypeId,
                Pos       = original.Pos,
                Rotation  = original.Rotation,
                ItemIds   = currentItemIds,
                ItemCount = currentItemCounts,
                ItemUids  = currentItemUids,
                ItemSlots = currentItemSlots,
            }, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
        }
    }

    void SendAllPlayerStatsToGuest(int guestId)
    {
        var objectManager = ObjectManager.Instance;
        if (objectManager == null) return;

        var hostStat = FindFirstObjectByType<PlayerStat>();
        if (hostStat != null)
        {
            PacketBuilder.SendToGuest(guestId, new H_StatSyncT
            {
                PlayerId = NetworkManager.Instance.MyPlayerId,
                Hp       = hostStat.CurrentHp,
                MaxHp    = hostStat.MaxHp,
                Stamina  = hostStat.CurrentStamina,
                Battery  = hostStat.CurrentBattery,
            }, H_StatSync.Pack, PacketType.H_StatSync);
        }

        foreach (var kv in objectManager.GetAllPlayerInfos(guestId))
        {
            if (!objectManager.TryGet(ObjectKind.Player, kv.PlayerId, out var worldObj)) continue;
            var stat = worldObj.GetComponent<StatBase>();
            if (stat == null) continue;

            float stamina = 0f;
            float battery = 0f;
            float defense = 0f;
            if (stat is RemotePlayerStat remote)
            {
                stamina = remote.CurrentStamina;
                battery = remote.CurrentBattery;
                defense = remote.ArmorDefenseRate;
            }

            PacketBuilder.SendToGuest(guestId, new H_StatSyncT
            {
                PlayerId = kv.PlayerId,
                Hp       = stat.CurrentHp,
                MaxHp    = stat.MaxHp,
                Stamina  = stamina,
                Battery  = battery,
                Defense  = defense,
            }, H_StatSync.Pack, PacketType.H_StatSync);
        }
    }

    private void SendHostEquipToGuest(int guestId)
    {
        int myId = NetworkManager.Instance?.MyPlayerId ?? 0;

        // 마운트는 RemotePlayer 프리팹에도 있으므로 씬 전체 검색(FindFirstObjectByType)을 쓰면
        // 남의 장비를 내 것으로 보낼 수 있다. 로컬 플레이어(PlayerMovement 보유)에서만 읽는다.
        var localRoot = PlayerMovement.LocalTransform;
        if (localRoot == null) return;

        // 무기 (슬롯 0, 1)
        var weaponMount = localRoot.GetComponentInChildren<WeaponMount>(true);
        if (weaponMount != null)
        {
            for (int slot = 0; slot < 2; slot++)
            {
                int itemId = weaponMount.GetEquippedItemId(slot);
                if (itemId == 0) continue;

                var gun = weaponMount.GetGun(slot);
                int uid = gun?.Uid ?? 0;

                PacketBuilder.SendToGuest(guestId,
                    new H_EquipT { PlayerId = myId, ItemId = itemId, ItemUid = uid, SlotIndex = slot },
                    H_Equip.Pack, PacketType.H_Equip);

                if (uid != 0 && gun != null)
                {
                    var (cur, max) = WorldEquipmentManager.GetOrCreate(uid, itemId, gun.MaxDurability);
                    PacketBuilder.SendToGuest(guestId,
                        new H_DurabilityT { ItemUid = uid, Current = cur, Max = max },
                        H_Durability.Pack, PacketType.H_Durability);
                }
            }
        }

        // 방어구 (슬롯 2)
        var armorMount = localRoot.GetComponentInChildren<ArmorMount>(true);
        if (armorMount != null)
        {
            int itemId = armorMount.GetEquippedItemId();
            if (itemId != 0)
            {
                var armor = armorMount.GetArmor();
                int uid = armor?.Uid ?? 0;

                PacketBuilder.SendToGuest(guestId,
                    new H_EquipT { PlayerId = myId, ItemId = itemId, ItemUid = uid, SlotIndex = 2 },
                    H_Equip.Pack, PacketType.H_Equip);

                if (uid != 0 && armor != null)
                {
                    var (cur, max) = WorldEquipmentManager.GetOrCreate(uid, itemId, armor.MaxDurability);
                    PacketBuilder.SendToGuest(guestId,
                        new H_DurabilityT { ItemUid = uid, Current = cur, Max = max },
                        H_Durability.Pack, PacketType.H_Durability);
                }
            }
        }

        // 가방 (슬롯 4)
        var bagMount = localRoot.GetComponentInChildren<BagMount>(true);
        if (bagMount != null)
        {
            int itemId = bagMount.GetEquippedItemId();
            if (itemId != 0)
            {
                int uid = bagMount.GetEquippedUid();
                PacketBuilder.SendToGuest(guestId,
                    new H_EquipT { PlayerId = myId, ItemId = itemId, ItemUid = uid, SlotIndex = 4 },
                    H_Equip.Pack, PacketType.H_Equip);
            }
        }
    }

    void Update()
    {
        if (_udpSession == null) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (_isHost)
        {
            var timedOut = new System.Collections.Generic.List<int>();
            foreach (var kv in _guestLastSeen)
                if (now - kv.Value > TIMEOUT_MS) timedOut.Add(kv.Key);
            foreach (var id in timedOut)
                RemoveGuest(id);
        }
        else if (_hostEp != null && _hostLastSeen > 0 && now - _hostLastSeen > TIMEOUT_MS)
        {
            HandleHostLeft();
        }

        SendKeepalivesIfDue(now);
        _reliable?.Tick(now);
    }

    void SendKeepalivesIfDue(long now)
    {
        if (_udpSession == null) return;
        if (now - _lastKeepaliveSent < KEEPALIVE_INTERVAL_MS) return;
        _lastKeepaliveSent = now;

        if (_isHost)
        {
            foreach (var kv in _guests)
                _udpSession.SendKeepalive(kv.Value);
        }
        else if (_hostEp != null)
        {
            _udpSession.SendKeepalive(_hostEp);
        }
    }

    public void RemoveGuest(int guestId)
    {
        if (!_guests.TryRemove(guestId, out IPEndPoint _)) return;
        _guestLastSeen.TryRemove(guestId, out long _);
        _waitingGuests.TryRemove(guestId, out NetInfoT _);

        // 트래커를 비우기 전에 게스트 상태를 방 세계 세이브에 영속화 (재접속 시 복원용)
        PersistGuestRoomState(guestId);

        GuestInventoryTracker.ClearGuest(guestId);
        RemoteEquipState.ClearPlayer(guestId);

        ObjectManager.Instance?.Despawn(ObjectKind.Player, guestId);
        OnPlayerCountChanged?.Invoke(_guests.Count + 1);
        OnGuestLeft?.Invoke(guestId);

        PacketBuilder.BroadcastToGuests(
            new H_LeaveT { PlayerId = guestId, IsHost = false },
            H_Leave.Pack, PacketType.H_Leave);
    }

    // 호스트 전용 — 게스트의 현재 상태(인벤/로드아웃/총·방어구 인스턴스상태)를 호스트가 보유한
    // 트래커에서 수집해 방 세계 세이브에 저장한다. playerId를 키로 쓴다(현재는 세션 id).
    void PersistGuestRoomState(int guestId)
    {
        if (!_isHost) return;

        var state = new SaveManager.GuestRoomState { playerId = guestId };
        var uids = new HashSet<int>();

        foreach (var (slot, itemId, amount, uid) in GuestInventoryTracker.GetSlots(guestId))
        {
            state.inventory.Add(new SaveManager.SlotSaveEntry { slotIndex = slot, itemId = itemId, amount = amount, uid = uid });
            if (uid != 0) uids.Add(uid);
        }

        foreach (var (slot, itemId, uid) in RemoteEquipState.GetSlots(guestId))
        {
            state.equipSlots.Add(new SaveManager.EquipSlotEntry { slotIndex = slot, itemId = itemId, uid = uid });
            if (uid != 0) uids.Add(uid);
        }

        state.equipment = WorldEquipmentManager.ExportSubset(uids);

        SaveManager.GetInstance()?.SaveGuestRoomState(state);
    }

    // 호스트 전용 — 방 세계에 저장된 게스트 상태를 접속 시 그 게스트에게 복원 전송한다.
    // 총 내구도/파츠/탄약은 담지 않는다 — 게스트가 재장착하면 기존 H_Durability/H_GunState 흐름이 채운다
    // (호스트 WorldEquipmentManager에 그 uid 상태가 이미 로드돼 있으므로).
    void SendGuestRoomRestore(int guestId)
    {
        var state = SaveManager.GetInstance()?.LoadGuestRoomState(guestId);
        if (state == null) return; // 저장된 게 없으면 빈손 시작

        var t = new H_GuestRestoreT
        {
            PlayerId = guestId,
            Inventory = new List<GuestInvEntryT>(),
            Equips = new List<GuestEquipEntryT>(),
        };

        foreach (var inv in state.inventory)
        {
            // 호스트 미러도 저장분으로 선반영 — 게스트가 채울 인벤과 일치시켜 교환 검증 정합성 유지
            GuestInventoryTracker.SetSlot(guestId, inv.slotIndex, inv.itemId, inv.amount, inv.uid);
            t.Inventory.Add(new GuestInvEntryT { SlotIndex = inv.slotIndex, ItemId = inv.itemId, Amount = inv.amount, ItemUid = inv.uid });
        }
        foreach (var eq in state.equipSlots)
            t.Equips.Add(new GuestEquipEntryT { SlotIndex = eq.slotIndex, ItemId = eq.itemId, ItemUid = eq.uid });

        PacketBuilder.SendReliableToGuest(guestId, t, H_GuestRestore.Pack, PacketType.H_GuestRestore);
    }

    public void HandleHostLeft()
    {
        if (_hostEp == null) return;
        _hostEp = null;
        _hostLastSeen = 0;
        LeaveRoom();
        OnHostLeft?.Invoke();
    }

    public void LeaveRoom()
    {
        RoomSync.Leave();
        CloseUdpSession();

        // 아직 호스트일 때(_isHost=false 및 Clear 이전) 장비 상태를 최종 저장 — 이후 빈 상태로 덮어쓰기 방지
        SaveManager.GetInstance()?.SaveEquipmentState();

        _guests.Clear();
        _guestLastSeen.Clear();
        _hostLastSeen = 0;
        _hostEp = null;
        _isHost = false;
        _waitingGuests.Clear();

        ObjectManager.Instance?.Clear();
        WorldEquipmentManager.Clear();
        RemoteEquipState.Clear();
    }

    void CloseUdpSession()
    {
        foreach (var p in _punchers.Values)
            p.Stop();
        _punchers.Clear();

        if (_udpSession == null) return;

        if (_reliable != null)
            _reliable.OnDeliveryFailed -= OnReliableDeliveryFailed;
        _reliable?.Unsubscribe();
        _reliable?.Clear();
        _reliable = null;

        _udpSession.OnReceived -= OnUdpReceived;
        _udpSession.OnKeepaliveReceived -= OnUdpKeepaliveReceived;
        _udpSession.Close();
        _udpSession = null;
        _lastKeepaliveSent = 0;
    }

    public void HandleFailure(string messageKey)
    {
        MainThreadDispatcher.Enqueue(() => OnRoomJoinFailed?.Invoke(messageKey));
    }

    void OnReliableDeliveryFailed(PacketType type, IPEndPoint endPoint)
    {
        Debug.LogWarning($"[RoomManager] Reliable delivery gave up: type={type}, to={endPoint}");
        MainThreadDispatcher.Enqueue(() => OnSyncFailed?.Invoke("network.sync_failed_message"));
    }

    public static string GetNetworkFailureTitleKey(string messageKey)
    {
        if (string.IsNullOrEmpty(messageKey)) return null;
        return messageKey switch
        {
            "network.invite_failed_message" => "network.invite_failed_title",
            "network.join_failed_message"   => "network.join_failed_title",
            _                               => "network.connect_failed_title",
        };
    }

    protected override void OnDestroy()
    {
        OnGameStarted -= LoadShelterIfOnTitle;
        CloseUdpSession();
        base.OnDestroy();
    }
}

