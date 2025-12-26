using Godot;
using System;

public partial class GlobalScript : Node
{
	public static GlobalScript Instance { get; private set; }

	public ENetMultiplayerPeer peer;
	public String playerName;
	public override void _Ready()
	{
		Instance = this;
	}
}
