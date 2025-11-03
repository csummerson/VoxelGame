using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsManager : Control
{
    [Export] FlatController flatController;
    
    public override void _Ready()
    {
        Visible = false;
        CallDeferred(nameof(DeferredLoad));
    }

    private void DeferredLoad()
    {
        SaveManager.Instance.LoadData();
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
		{
			Toggle();
		}
    }

    private bool toggled = false;
    private void Toggle()
    {
        toggled = !toggled;

        if (toggled)
        {
            Visible = true;
        }
        else
        {
            Visible = false;
            if (flatController != null)
            {
                flatController.UpdateSettings();
            }
        }
    }
}
