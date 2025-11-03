using Godot;
using System;

public partial class UtilsController : Control
{
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_stats"))
        {
            ToggleMe();
        }
    }

    private bool toggled = true;
    private void ToggleMe()
    {
        toggled = !toggled;

        if (toggled)
        {
            Visible = true;
        } else
        {
            Visible = false;
        }
    }

}
