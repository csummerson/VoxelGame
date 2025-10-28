using Godot;
using System;
using System.Threading.Tasks;

public partial class Typewriter : Node
{
    public async Task TypeText(Label label, string fullText, float delay = 0.05f)
    {
        label.Text = "Hi";
        
        string currText = "";
        foreach (char c in fullText)
        {
            currText += c;
            label.Text = currText;

            await ToSignal(GetTree().CreateTimer(delay), "timeout");
        }
    }
    
    public async Task TypeText(Label label, string fullText, string startText, float delay = 0.05f) {
        label.Text = "Hi.";

        string currText = startText;
        foreach (char c in fullText.Substring(startText.Length))
        {
            currText += c;
            label.Text = currText;

            await ToSignal(GetTree().CreateTimer(delay), "timeout");
        }
    }
}
