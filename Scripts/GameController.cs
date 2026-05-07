using Godot;
using System.Collections.Generic;
using System.Collections.Specialized;

public partial class GameController : Node
{
	public PackedScene PlayerTemplate = GD.Load<PackedScene>("res://Scenes/Player.tscn");
	Dictionary<long, Node2D> players = new Dictionary<long, Node2D>();
	Dictionary<long, Node> playerHands = new Dictionary<long, Node>();
	LobbyProperties currentLobby = LobbyScript.Instance.properties;
	[Export] Sprite2D Table;
	Sprite2D Arrow;
	public override void _Ready()
	{
		GameNetworkScript.Instance.gameController = this;
		Arrow = Table.GetNode<Sprite2D>("Arrow");
		GameNetworkScript.Instance.RpcId(1, nameof(GameNetworkScript.Instance.playerLoadedToGameReq), currentLobby.LobbyID, GlobalScript.Instance.peer.GetUniqueId());
		pointAtPlayer(0);
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

	public void pointAtPlayer(int index)
	{
		float radius = Table.Texture.GetSize().Y * Table.Scale.Y / 2;
		float sidelength = (Table.Texture.GetSize().X * Table.Scale.X) - (radius * 2);
		float totalLength = sidelength + (radius * radius * Mathf.Pi);

		float distance = 1 / LobbyScript.Instance.properties.players.Count * index;

		if((sidelength/2) / totalLength > distance || totalLength - ((sidelength/2) / totalLength) < distance)
		{
			Arrow.Position = new Vector2((distance / (sidelength/2) / totalLength) * (sidelength/2), radius);
			Arrow.Rotation = 0;
		}
	}
}