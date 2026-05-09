using Godot;
using System;
using System.IO;

public partial class GlobalScript : Node
{
	public static GlobalScript Instance { get; private set; }

	public ConfigFile config = new ConfigFile();
	public ENetMultiplayerPeer peer;
	public string playerName;
	public Texture2D redCardBackTexture;
	public Texture2D blueCardBackTexture;
	public override void _Ready()
	{
		Instance = this;
		Error err = config.Load("user://userdata.json");
		if(err == Error.Ok) playerName = (string) config.GetValue("playerName", "PlayerName");
		else playerName = "";

		redCardBackTexture = GD.Load<Texture2D>("res://Textures/cards/RedBack.jpg");
		blueCardBackTexture = GD.Load<Texture2D>("res://Textures/cards/BlueBack.jpg");
	}
}
