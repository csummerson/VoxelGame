using Godot;
using System;
using System.Collections.Generic;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }
    private ConfigFile loadedConfig;

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            SetProcess(false);
            //LoadData();
        }
        else
        {
            QueueFree();
        }
    }

    // The Settings!
    [Export] public string savePath = "user://settings.cfg";
    private Dictionary<string, ISaveable> saveables = new();

    public void RegisterSaveable(ISaveable obj)
    {
        if (!saveables.ContainsKey(obj.SaveID))
        {
            saveables.Add(obj.SaveID, obj);
        }
    }

    public void UnregisterSaveable(ISaveable obj)
    {
        if (saveables.ContainsKey(obj.SaveID))
        {
            saveables.Remove(obj.SaveID);
        }
    }

    public void LoadData()
    {
        loadedConfig = new ConfigFile();
        if (loadedConfig.Load(savePath) != Error.Ok)
        {
            loadedConfig = null;
            return;
        }
    }

    public Dictionary<string, Variant> GetDataFor(string id)
    {
        if (loadedConfig == null || !loadedConfig.HasSection(id))
        {
            return null;
        }

        var dict = new Dictionary<string, Variant>();
        foreach (string key in loadedConfig.GetSectionKeys(id))
        {
            dict[key] = loadedConfig.GetValue(id, key);
        }

        return dict;
    }

    public void SaveData()
    {
        ConfigFile config = new ConfigFile();

        foreach (var pair in saveables)
        {
            string id = pair.Key;
            var data = pair.Value.Save();

            foreach (var kv in data)
            {
                config.SetValue(id, kv.Key, kv.Value);
            }
        }

        config.Save(savePath);
        GD.Print("Settings saved to " + savePath);
    }
}

public interface ISaveable
{
    string SaveID { get; }
    Dictionary<string, Variant> Save();
    void Load(Dictionary<string, Variant> data);
}
