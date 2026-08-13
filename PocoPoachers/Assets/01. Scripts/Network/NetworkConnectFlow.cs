using System;
using System.Collections;
using UnityEngine;

// 마스터 서버 로그인을 기다렸다가 이어서 실행하는 공용 흐름.
// 타이틀(불러오기/협동)과 캐릭터 생성(새 게임)이 같은 대기 규칙을 쓰도록 한 곳에 모았다.
public static class NetworkConnectFlow
{
    public const float ConnectTimeout = 5f;

    public static IEnumerator Run(Action onSuccess, Action onFail)
    {
        var nm = NetworkManager.Instance;
        if (nm == null)
        {
            onFail?.Invoke();
            yield break;
        }

        if (!nm.IsLoggedIn)
        {
            if (nm.Session == null || !nm.Session.IsConnected)
                nm.Reconnect();

            float elapsed = 0f;
            while (!nm.IsLoggedIn && elapsed < ConnectTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!nm.IsLoggedIn)
            {
                onFail?.Invoke();
                yield break;
            }
        }

        onSuccess?.Invoke();
    }
}
