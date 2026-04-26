using System.Text;

namespace Server;

public static class GameDataLoader
{
    public static void Load()
    {
        LoadItemTable();
    }

    static void LoadItemTable()
    {
        string jsonPath = ServerPaths.GeneratedJson("item.json");
        if (!File.Exists(jsonPath))
        {
            LOG_E($"item.json 없음: {jsonPath}");
            return;
        }

        string json = File.ReadAllText(jsonPath, Encoding.UTF8);
        ItemTable.Load(json);
        LOG($"ItemTable 로드: {ItemTable.All.Count}종");
    }
}
