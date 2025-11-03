using Godot;
using System;

public partial class ThreadSlider : HSlider
{
    public override void _Ready()
    {
        int maxThreads = System.Environment.ProcessorCount;
        MaxValue = maxThreads;
        Value = GameSettings.Instance.threadCount;

        ValueChanged += OnValueChanged;
    }

    private void OnValueChanged(double value)
    {
        GameSettings.Instance.threadCount = (int)value;
    }
}
