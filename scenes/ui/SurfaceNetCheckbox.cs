using Godot;
using System;

public partial class SurfaceNetCheckbox : CheckBox
{
    public override void _Ready()
    {
        ButtonPressed = GameSettings.Instance.surfaceNets;
        Toggled += ToggleSurfaceNets;    
    }

    public void ToggleSurfaceNets(bool toggle)
    {
        GameSettings.Instance.surfaceNets = toggle;
        GameSettings.Instance.MarkDirty();
    }
}
