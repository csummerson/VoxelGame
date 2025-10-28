using Godot;
using System;

public partial class AutoScroller : ScrollContainer
{
    VScrollBar vScrollBar;

    public override void _Ready()
    {
        vScrollBar = GetVScrollBar();

        vScrollBar.Connect("changed", new Callable(this, nameof(OnScrollBarValueChanged)));
    }

    private void OnScrollBarValueChanged()
    {
        vScrollBar.Value = vScrollBar.MaxValue;
    }
}
