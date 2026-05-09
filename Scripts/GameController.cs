using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

public partial class GameController : Node
{
	[Export] public PackedScene PlayerTemplate;
	Dictionary<long, Node2D> players = new Dictionary<long, Node2D>();
	Dictionary<long, Node> playerHands = new Dictionary<long, Node>();
	LobbyProperties currentLobby = LobbyScript.Instance.properties;
	[Export] Sprite2D Table;
	[Export] Curve arrowAnimCurve;
	Sprite2D Arrow;
	int myPlayerIndex;
	int currentPlayerIndex;
	private float arrowVisualProgress = 0f;
	private float arrowStart = 0f;
	private float arrowTarget = 0f;
	private float arrowAnimProgress = 0f;
	private float arrowAnimDuration = 0.4f;
	private float arrowAnimSpeed = 2.0f;
	private Color startColor;
	private Color endColor;
	public static GameController Instance { get; private set; }
	public override void _Ready()
	{
		Instance = this;
		Arrow = Table.GetNode<Sprite2D>("Arrow");
		GameNetworkScript.Instance.RpcId(1, nameof(GameNetworkScript.Instance.playerLoadedToGameReq), currentLobby.LobbyID, GlobalScript.Instance.peer.GetUniqueId());
		myPlayerIndex = currentLobby.playerOrder.IndexOf(GlobalScript.Instance.peer.GetUniqueId());
		currentPlayerIndex = 0;
		pointAtPlayer(currentPlayerIndex, currentLobby.players.Count);
	}
	public override void _Process(double delta)
	{
		if (arrowAnimProgress < 1.0f)
		{
			arrowAnimProgress += (float)delta / arrowAnimDuration;
			arrowAnimProgress = Mathf.Min(arrowAnimProgress, 1.0f);

			float curveValue = arrowAnimCurve.Sample(arrowAnimProgress);
			float interpolatedProgress = Mathf.Lerp(arrowStart, arrowTarget, curveValue);

			Arrow.Modulate = startColor.Lerp(endColor, curveValue);
			
			arrowVisualProgress = Mathf.PosMod(interpolatedProgress, 1.0f);

			Vector2 pos;
			float rot;
			(pos, rot) = getTablePathTransform(arrowVisualProgress);

			Vector2 direction = Vector2.FromAngle(rot + Mathf.Pi / 2);

			Arrow.Position = pos - direction * 100f;
			Arrow.Rotation = rot;
		}
	}

	public void showLoadedPlayers()
	{
		foreach(var peer in currentLobby.players.Values)
		{
			if(peer.isReady && !players.ContainsKey(peer.peerUID) && peer.peerUID != GlobalScript.Instance.peer.GetUniqueId())
			{
				GD.Print("Showing player: " + peer.name);
				showPlayer(peer.peerUID);
			}
		}
	}

	public void showPlayer(long peerUID)
	{
		Node2D player = PlayerTemplate.Instantiate<Node2D>();
		player.GetNode<Label>("Name").Text = currentLobby.players[peerUID].name;
		player.GetNode<Sprite2D>("Head").Modulate = Functions.PlayerColors[currentLobby.players[peerUID].colorIndex];
		Table.AddChild(player);
		players.Add(peerUID, player);

		Vector2 pos;
		float rot;

		(pos, rot) = getTablePathTransform(currentLobby.playerOrder.IndexOf(peerUID), currentLobby.players.Count);

		Vector2 direction = Vector2.FromAngle(rot + Mathf.Pi / 2);

		player.Position = pos + direction * 60f;
		player.Rotation = rot;
	}

	public void pointAtPlayer(int index, int playerCount)
	{
		float target = (float) ((index + (playerCount - myPlayerIndex)) % playerCount) / playerCount;
		startColor = Arrow.Modulate;
		endColor = Functions.PlayerColors[currentLobby.players[currentLobby.playerOrder[index]].colorIndex];


		if(target <= arrowVisualProgress)
		{
			target += 1.0f;
		}

		arrowStart = arrowVisualProgress;
		arrowTarget = target;
		arrowAnimProgress = 0f;
	}

	public (Vector2, float) getTablePathTransform(int index, int playerCount)
	{
		return getTablePathTransform((float) ((index + (playerCount - myPlayerIndex)) % playerCount) / playerCount);
	}

	public (Vector2, float) getTablePathTransform(float progress)
	{
		// 1. Dimensions
		float radius = Table.Texture.GetSize().Y / 2;
		float side = Table.Texture.GetSize().X - (radius * 2);
		float arcLength = Mathf.Pi * radius;

		float totalLength = (side * 2) + (arcLength * 2);
		float targetDist = progress * totalLength;

		float pos1 = side / 2;               // End of first bottom straight (center to right)
		float pos2 = pos1 + arcLength;         // End of right arc
		float pos3 = pos2 + side;              // End of top straight
		float pos4 = pos3 + arcLength;         // End of left arc

		Vector2 pos;
		float rot;

		if (targetDist < pos1) {
			// Bottom Left Straight
			pos = new Vector2(-targetDist, radius);
			rot = 0;
		}
		else if (targetDist < pos2) {
			// Left Arc
			float arcProgress = (targetDist - pos1) / arcLength;
			float angle = (arcProgress * Mathf.Pi) + (Mathf.Pi / 2);
			pos = new Vector2(-side / 2 + Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
			rot = angle - Mathf.Pi / 2;
		}
		else if (targetDist < pos3) {
			// Top Straight
			float localDist = targetDist - pos2;
			pos = new Vector2(-side / 2 + localDist, -radius);
			rot = Mathf.Pi;
		}
		else if (targetDist < pos4) {
			// Right Arc
			float arcProgress = (targetDist - pos3) / arcLength;
			float angle = (arcProgress * Mathf.Pi) - (Mathf.Pi / 2);
			pos = new Vector2(side / 2 + Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
			rot = angle - Mathf.Pi / 2;
		}
		else {
			// Bottom right Straight
			float localDist = targetDist - pos4;
			pos = new Vector2(side / 2 - localDist, radius);
			rot = 0;
		}

		return (pos, rot);
	}

	public void nextPlayer()
	{
		currentPlayerIndex = (currentPlayerIndex + 1) % currentLobby.players.Count;
		pointAtPlayer(currentPlayerIndex, currentLobby.players.Count);
	}
}