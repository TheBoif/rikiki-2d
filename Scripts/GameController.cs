using Godot;
using System.Collections.Generic;
using System.Collections.Specialized;

public partial class GameController : Node
{
	public PackedScene PlayerTemplate = GD.Load<PackedScene>("res://Scenes/Player.tscn");
	Dictionary<long, Node2D> players = new Dictionary<long, Node2D>();
	Dictionary<long, Node> playerHands = new Dictionary<long, Node>();
	LobbyProperties currentLobby = LobbyScript.Instance.properties;
	public override void _Ready()
	{
		GameNetworkScript.Instance.gameController = this;
		GameNetworkScript.Instance.RpcId(1, nameof(GameNetworkScript.Instance.playerLoadedToGameReq), currentLobby.LobbyID, GlobalScript.Instance.peer.GetUniqueId());
	}
	public override void _Process(double delta)
	{
		
	}

	public void setPlayerPosition(long playerUID)
	{
		players.Add(playerUID, PlayerTemplate.Instantiate() as Node2D);
		Sprite2D head = players[playerUID].GetNode<Sprite2D>("Head");
		head.Modulate = Functions.PlayerColors[currentLobby.players[playerUID].colorIndex];
		players[playerUID].GetNode<Label>("Name").Text = currentLobby.players[playerUID].name;
		int playerCount = LobbyScript.Instance.properties.players.Count;
		int index = 0;
		foreach(var peer in LobbyScript.Instance.properties.players.Values)
		{
			if(peer.peerUID == playerUID)
			{
				float angle = (float)index / playerCount * Mathf.Pi * 2;
				players[peer.peerUID].Position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 200;
				players[peer.peerUID].Rotation = angle + Mathf.Pi / 2;
				return;
			}
			index++;
		}
	}
	public void showPlayer(long peerUID)
	{
		setPlayerPosition(peerUID);
	}
}