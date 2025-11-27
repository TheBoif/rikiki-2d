using Godot;
using System;

public partial class MenuScript : Node
{
    public override void _Process(double delta)
    {
        if (!OS.HasFeature("dedicated_server"))
        {
            GetTree().ChangeSceneToFile("res://Scenes/ServerScene.tscn");
        }
    }
}