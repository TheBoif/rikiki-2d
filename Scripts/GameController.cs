using Godot;
using System;
using System.Collections.Generic;

public partial class GameController : Node
{
	public PackedScene PlayerTemplate = GD.Load<PackedScene>("res://Scenes/Player.tscn");
	Dictionary<long, Vector2> playerPositions = new Dictionary<long, Vector2>();
	Dictionary<long, float> playerRotations = new Dictionary<long, float>();
	Dictionary<long, Node> playerHands = new Dictionary<long, Node>();
	public override void _Ready()
	{
		dividePlayerPositions();
		RpcId(1, nameof(LobbyScript.playerLoadedToGameReq), LobbyScript.Instance.properties.LobbyID, GlobalScript.Instance.peer.GetUniqueId());
	}
	public override void _Process(double delta)
	{
		
	}

	public void dividePlayerPositions()
	{
		int playerCount = LobbyScript.Instance.properties.players.Count;
		int index = 0;
		foreach(var peer in LobbyScript.Instance.properties.players.Values)
		{
			float angle = (float)index / playerCount * Mathf.Pi * 2;
			playerPositions[peer.peerUID] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 200;
			playerRotations[peer.peerUID] = angle + Mathf.Pi / 2;
			index++;
		}
	}

	public void showPlayer(long peerUID)
	{
		
	}
}