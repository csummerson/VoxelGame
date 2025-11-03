using Godot;
using System;

public partial class SliderValueTracker : Label
{
    [Export] public HSlider sliderToTrack;

    public override void _Ready()
    {
        sliderToTrack.ValueChanged += UpdateText;
        UpdateText(sliderToTrack.Value);
    }

    private void UpdateText(double value)
    {
        int intValue = (int)value;
        Text = "   " + intValue.ToString("D2");
    }
}
