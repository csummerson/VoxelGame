using Godot;
using System;

public partial class TerminateButton : Button
{
    public override void _Ready()
    {
        Pressed += Terminate;
    }

    private void Terminate()
    {
        GameManager.Instance.QuitGame();
    }
}
