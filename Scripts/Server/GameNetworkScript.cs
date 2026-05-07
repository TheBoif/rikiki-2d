using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameNetworkScript : Node
{

	public static GameNetworkScript Instance { get; private set; }
	public GameController gameController {get; set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	#region Server Methods

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void playerLoadedToGameReq(string lobbyID, long peerUID)
	{
		LobbyScript.Instance.lobbies[lobbyID].players[peerUID].isReady = true;
		foreach(var peer in LobbyScript.Instance.lobbies[lobbyID].players.Values)
		{
			PlayersReadyReq(lobbyID);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void PlayersReadyReq(string lobbyID)
	{
		List<long> ids = new List<long>();
		List<int> readyStatus = new List<int>();

		foreach(var peer in LobbyScript.Instance.lobbies[lobbyID].players.Values)
		{

			ids.Add(peer.peerUID);
			readyStatus.Add(peer.isReady ? 1 : 0);

		}

		foreach(var peer in LobbyScript.Instance.lobbies[lobbyID].players.Values)
		{
			RpcId(peer.peerUID, nameof(playersReadyResp), ids.ToArray(), readyStatus.ToArray());
		}
	}

	#endregion
	#region Player Methods

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void playersReadyResp(long[] peerUID, int[] readyStatus)
	{
		for (int i = 0; i < peerUID.Length; i++)
		{
			LobbyScript.Instance.properties.players[peerUID[i]].isReady = readyStatus[i] == 1;
		}
	}

	#endregion
}
