using Godot;
using System;
using System.Collections.Generic;

public partial class GameSettings : Node, ISaveable
{
    public static GameSettings Instance { get; private set; }

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            SetProcess(false);
        }
        else
        {
            QueueFree();
        }
    }

    // public override void _Ready()
    // {
    //     SaveManager.Instance.RegisterSaveable(this);

    //     var data = SaveManager.Instance.GetDataFor(SaveID);
    //     if (data != null)
    //     {
    //         Load(data);
    //     }
    // }
    
    public void ManualLoad()
    {
        SaveManager.Instance.RegisterSaveable(this);
        
        var data = SaveManager.Instance.GetDataFor(SaveID);
        if (data != null)
        {
            Load(data);
        }
    }

    public string SaveID => "GameSettings";

    public Dictionary<string, Variant> Save()
    {
        return new Dictionary<string, Variant>
        {
            { "mouse_sensitivity", mouseSensitivity },
            { "view_distance", viewDistance },
            { "thread_count", threadCount },
            { "surface_nets", surfaceNets },
            { "full_screen", fullScreen },
            { "seed", seed },
            { "has_seen_terminal", hasSeenTerminal },
        };
    }

    public void Load(Dictionary<string, Variant> data)
    {
        if (data.TryGetValue("mouse_sensitivity", out var ms)) mouseSensitivity = (int)ms;
        if (data.TryGetValue("view_distance", out var vd)) viewDistance = (int)vd;
        if (data.TryGetValue("thread_count", out var tc)) threadCount = (int)tc;
        if (data.TryGetValue("surface_nets", out var sn)) surfaceNets = (bool)sn;
        if (data.TryGetValue("full_screen", out var fs)) fullScreen = (bool)fs;
        if (data.TryGetValue("seed", out var sd)) seed = (int)sd;
        if (data.TryGetValue("has_seen_terminal", out var st)) hasSeenTerminal = (bool)st;
    }

    // preferences
    public int mouseSensitivity = 50;

    // performance
    public int viewDistance = 12;
    public int threadCount = 6;
    
    // toggles
    public bool surfaceNets = true;
    public bool fullScreen = true;

    // world
    public int seed = 3564;

    public bool hasSeenTerminal = false;

    public bool dirty { get; private set; } = false;

    public void MarkDirty()
    {
        dirty = true;
    }

    public void ScrubbyScrubby()
    {
        dirty = false;
    }
}
