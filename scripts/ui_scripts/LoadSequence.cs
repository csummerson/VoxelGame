using Godot;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Xml.XPath;
using System.Collections.Generic;

public partial class LoadSequence : Node
{
    [Export] public VBoxContainer TerminalBox;
    [Export] public PackedScene PlayerOutScene;
    [Export] public PackedScene TorOutScene;
    [Export] public Typewriter typewriter;
    [Export] public LineEdit terminalLine;

    private List<Label> labels = new List<Label>();

    [Export] public PackedScene firstPlanet;

    string version = (string)ProjectSettings.GetSetting("application/config/version");

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _ = RunSequence();
        terminalLine.Connect("text_submitted", new Callable(this, nameof(OnPlayerInputSubmitted)));
    }

    private async Task RunSequence()
    {
        if (GameManager.Instance.hasSeenTerminal)
        {
            LoadPlanet();
        }
        else
        {
            await RunTerminalStartSequence();
        }
    }

    public string user;
    public string address;

    private async Task RunTerminalStartSequence()
    {
        await AddLabelTimer(TorOutScene, $"[NULL] v{version}", 0, 0);
        await AddLabelTimer(TorOutScene, "(c) [REDACTED] Archive. All rites observed.", 0, 0);
        await AddLabelTimer(TorOutScene, " ", 0, 3f);

        await AddLabelTimer(TorOutScene, "ERROR: NOT RECOGNIZED AS A VALID TEST.");
        await AddLabelTimer(TorOutScene, "LOADING EXPERIMENTAL DEMONSRTATION...");

        await AddLabelTimer(TorOutScene, $"...", 0.2f);

        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        GameManager.Instance.hasSeenTerminal = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        LoadPlanet();
    }

    private void LoadPlanet()
    {
        GetTree().ChangeSceneToPacked(firstPlanet);
    }

    private async Task AddLabelTimer(PackedScene scene, string text, float delay = 0.025f, float pause = 1f)
    {
        Label label = InstantAndAdd(scene);
        labels.Add(label);
        await typewriter.TypeText(label, text, delay);
        await ToSignal(GetTree().CreateTimer(pause), "timeout");
    }

    private Label InstantAndAdd(PackedScene scene)
    {
        Node instance = scene.Instantiate();
        TerminalBox.AddChild(instance);
        Label instanceLab = instance.GetNode<Label>(".");
        return instanceLab;
    }

    private async Task<string> PlayerResponse(bool makeLabel = true)
    {
        await ToSignal(GetTree().CreateTimer(0.1), "timeout");

        terminalLine.GrabFocus();
        
        await ToSignal(GetTree().CreateTimer(0.1), "timeout");

        string playerInput = await WaitForInputAsync();
        terminalLine.Clear();

        if (makeLabel)
        {
            Node instance = PlayerOutScene.Instantiate();
            TerminalBox.AddChild(instance);
            Label instanceLab = instance.GetNode<Label>(".");
            instanceLab.Text = " > " + playerInput;
        }

        return playerInput;
    }

    private TaskCompletionSource<string> _inputCompletionSource;

    private Task<string> WaitForInputAsync()
    {
        _inputCompletionSource = new TaskCompletionSource<string>();
        return _inputCompletionSource.Task;
    }

    public void OnPlayerInputSubmitted(string input)
    {
        if (_inputCompletionSource != null)
        {
            _inputCompletionSource.SetResult(input);
            _inputCompletionSource = null;
        }
    }
}
