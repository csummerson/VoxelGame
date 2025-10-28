using Godot;
using System;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set;}

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

    [Export] public string savePath;


    /// <summary>
    /// Loads save data from disk.
    /// </summary>
    /// <returns>
    /// Returns:
    /// <list type="bullet">
    /// <item><description><b>0</b> - Error loading file</description></item>
    /// <item><description><b>1</b> - Load file not found </description></item>
    /// <item><description><b>2</b> - Load file outdated</description></item>
    /// <item><description><b>3</b> - Load successful</description></item>
    /// </list> 
    /// </returns>
    public int LoadData()
    {
        if (!FileAccess.FileExists(savePath))
        {
            GD.Print("No save file found.");
            return 1;
        }

        ConfigFile config = new ConfigFile();
        Error err = config.Load(savePath);

        if (err != Error.Ok)
        {
            GD.Print("Error loading save file.");
            return 0;
        }

        int outVal = 3;
        // Load vals
        string version_string = (String)config.GetValue("Version", "version_string");
        if (version_string != (string)ProjectSettings.GetSetting("application/config/version"))
        {
            outVal = 2;
        }

        // Generator settings
        GameManager ins = GameManager.Instance;
        ins.MODEL = (int)config.GetValue("Generator", "MODEL");
        ins.RD = (int)config.GetValue("Generator", "RD");
        ins.SD = (int)config.GetValue("Generator", "SD");
        ins.SIZE = (int)config.GetValue("Generator", "SIZE");

        return outVal;
    }

    public void SaveData()
    {
        ConfigFile config = new ConfigFile();

        string version = (string)ProjectSettings.GetSetting("application/config/version");

        // Version things
        config.SetValue("Version", "version_string", version);

        // Generator settings
        GameManager ins = GameManager.Instance;
        config.SetValue("Generator", "MODEL", ins.MODEL);
        config.SetValue("Generator", "RD", ins.RD);
        config.SetValue("Generator", "SD", ins.SD);
        config.SetValue("Generator", "SIZE", ins.SIZE);

        config.Save(savePath);
    }
}
