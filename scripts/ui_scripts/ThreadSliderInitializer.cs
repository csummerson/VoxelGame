using Godot;
using System;

public partial class ThreadSliderInitializer : Node
{
    [Export] HSlider slider;

    public override void _Ready()
    {
        int maxThreads = System.Environment.ProcessorCount;
        slider.MaxValue = maxThreads;
    }

}
