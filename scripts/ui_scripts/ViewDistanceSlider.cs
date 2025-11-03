using Godot;
using System;
using System.Collections.Generic;

public partial class ViewDistanceSlider : HSlider
{
    public override void _Ready()
    {
        Value = GameSettings.Instance.viewDistance;

        ValueChanged += OnValueChanged;
    }

    private void OnValueChanged(double value)
    {
        GameSettings.Instance.viewDistance = (int)value;
    }
}
