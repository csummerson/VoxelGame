using Godot;
using System;
using System.Collections.Generic;

public static class MaterialLibrary
{
    public static readonly Dictionary<byte, Material> Materials = new()
    {
        { 1, ResourceLoader.Load<Material>("res://materials/stone.tres") },
        { 2, ResourceLoader.Load<Material>("res://materials/sand.tres") },
        { 3, ResourceLoader.Load<Material>("res://materials/grass.tres") }
    };
}
