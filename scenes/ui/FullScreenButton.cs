using Godot;
using System;
using System.Collections.Generic;

public partial class FullScreenButton : CheckBox
{
    public override void _Ready()
    {
        ButtonPressed = GameSettings.Instance.fullScreen;
        Toggled += ToggleFullscreen;

        if (GameSettings.Instance.fullScreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}    
    }

    public void ToggleFullscreen(bool toggle)
    {
        GameSettings.Instance.fullScreen = toggle;
        
        if (toggle)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
    }
}
