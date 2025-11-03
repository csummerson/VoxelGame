using Godot;
using System;

public partial class MouseSensitivitySlider : HSlider
{
    public override void _Ready()
    {
        Value = GameSettings.Instance.mouseSensitivity;

        ValueChanged += OnValueChanged;
    }

    private void OnValueChanged(double value)
    {
        GameSettings.Instance.mouseSensitivity = (int)value;
    }
}
