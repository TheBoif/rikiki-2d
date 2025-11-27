using Godot;
using System;

public partial class GlobalScript : Node
{
    public static GlobalScript Instance { get; private set; }

	public ENetMultiplayerPeer peer;
	
	public override void _Ready()
	{
		peer = new ENetMultiplayerPeer();
		Instance = this;
	}
}
