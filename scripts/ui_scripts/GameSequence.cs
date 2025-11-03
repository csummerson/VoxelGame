using Godot;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Xml.XPath;
using System.Collections.Generic;

public partial class GameSequence : Node
{
    [Export] public VBoxContainer TerminalBox;
    [Export] public PackedScene PlayerOutScene;
    [Export] public PackedScene TorOutScene;
    [Export] public Typewriter typewriter;
    [Export] public LineEdit terminalLine;

    [Export] public bool loaded = false;

    private List<Label> labels = new List<Label>();

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
            await RunTerminalEndSequence();
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
        await AddLabelTimer(TorOutScene, "BOOTING.....................COMPLETE", 0.1f);
        await AddLabelTimer(TorOutScene, "Connect to a server? (y/n):", 0.05f, 0);

        string yorn;
        yorn = await PlayerResponse();
        while (yorn != "y" && yorn != "n")
        {
            await AddLabelTimer(TorOutScene, "Invalid response, please input 'y' or 'n':", 0.05f, 0);
            yorn = await PlayerResponse();
        }

        if (yorn == "n")
        {
            await AddLabelTimer(TorOutScene, "Understood. Starting singleplayer instance.");
            await AddLabelTimer(TorOutScene, "Press z at any time to return to this window.");
            await AddLabelTimer(TorOutScene, $"Loading...", 0.2f);
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            GameManager.Instance.hasSeenTerminal = true;
            GameManager.Instance.StartSinglePlayer();

            Input.MouseMode = Input.MouseModeEnum.Captured;
            GetParent().QueueFree();
            return;
        }

        await AddLabelTimer(TorOutScene, "Understood. Please input a username:", 0.05f, 0);
        user = await PlayerResponse();

        await AddLabelTimer(TorOutScene, $"Assigning you identifier '{user}'.");
        GameManager.Instance.username = user;
        await AddLabelTimer(TorOutScene, "Please input server address:", 0.05f, 0);
        address = await PlayerResponse();

        await AddLabelTimer(TorOutScene, $"Attempting to connect...");

        bool succeeded = NetworkManager.instance.StartClient(address);
        while (succeeded == false)
        {
            await AddLabelTimer(TorOutScene, $"Connection failed. Please re-input server address:", 0.05f, 0);
            address = await PlayerResponse();
            succeeded = NetworkManager.instance.StartClient(address);
        }

        await AddLabelTimer(TorOutScene, $"Connection succeeded. Press z at any time to return to this window.");
        await AddLabelTimer(TorOutScene, $"Loading...", 0.2f);
        GameSettings.Instance.hasSeenTerminal = true;

        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        

        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetParent().QueueFree();
    }

    private async Task RunTerminalEndSequence()
    {
        await AddLabelTimer(TorOutScene, $"Thank you for contributing to this test.");
        await AddLabelTimer(TorOutScene, $"Development is continuing as expected.");   
        await AddLabelTimer(TorOutScene, $"TERMINATING...........................................");

        GetTree().Quit();
    }

    private async Task AddLabelTimer(PackedScene scene, string text, float delay = 0.05f, float pause = 1f)
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
