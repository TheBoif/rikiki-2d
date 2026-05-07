using Godot;
using System;
using System.IO;

public partial class GlobalScript : Node
{
	public static GlobalScript Instance { get; private set; }

	public ConfigFile config = new ConfigFile();
	public ENetMultiplayerPeer peer;
	public string playerName;
	public override void _Ready()
	{
		Instance = this;
		Error err = config.Load("user://userdata.json");
		if(err == Error.Ok) playerName = (string) config.GetValue("playerName", "PlayerName");
		else playerName = "";
	}
}
