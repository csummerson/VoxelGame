using Godot;
using System;
using System.Collections.Generic;

public partial class LineEditSaver : LineEdit, ISaveable
{
    [Export] private string _saveID;
    public string SaveID => _saveID;

    int seed;

    public override void _Ready()
    {
        SaveManager.Instance.RegisterSaveable(this);
        //SaveManager.Instance.LoadData();
        TextSubmitted += SubmitSeed;
    }

    private void SubmitSeed(string text)
    {
        if (int.TryParse(text, out int parsedValue))
        {
            seed = parsedValue;
        }
        else
        {
            seed = (int)text.Hash();
        }

        if (seed == 0)
        {
            seed = 3564;
        }

        if (!string.IsNullOrEmpty(_saveID))
        {
            var field = typeof(GameSettings).GetField(_saveID);
            if (field != null)
            {
                field.SetValue(GameSettings.Instance, Convert.ChangeType(seed, field.FieldType));
            }
        }

        Clear();
        ReleaseFocus();
        PlaceholderText = seed.ToString();
    }
    
    public Dictionary<string, Variant> Save()
    {
        return new Dictionary<string, Variant>()
        {
            { "value", seed }
        };
    }
    
    public void Load(Dictionary<string, Variant> data)
    {
        if (data.TryGetValue("value", out Variant val))
        {
            seed = (int)val;
            PlaceholderText = seed.ToString();
        }
    }

}
