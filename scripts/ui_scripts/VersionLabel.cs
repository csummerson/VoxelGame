using Godot;
using System;

public partial class VersionLabel : Label
{
	private string version;
	
	public override void _Ready()
	{
		version = "0xDEC " + (string)ProjectSettings.GetSetting("application/config/version");
	}

    public override void _Process(double delta)
    {
		Text = version + "\nFPS: " + Engine.GetFramesPerSecond();
    }

}
