using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameNetworkScript : Node
{

	public static GameNetworkScript Instance { get; private set; }
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
		UpdatePlayersReadyReq(lobbyID);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void UpdatePlayersReadyReq(string lobbyID)
	{
		List<int> readyStatus = new List<int>();

		foreach(var peer in LobbyScript.Instance.lobbies[lobbyID].players.Values)
		{
			readyStatus.Add(peer.isReady ? 1 : 0);
		}

		foreach(var peer in LobbyScript.Instance.lobbies[lobbyID].players.Values)
		{
			RpcId(peer.peerUID, nameof(UpdatePlayersReadyResp), LobbyScript.Instance.lobbies[lobbyID].playerOrder.ToArray(), readyStatus.ToArray());
		}
	}

	#endregion
	#region Player Methods

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void UpdatePlayersReadyResp(long[] peerUID, int[] readyStatus)
	{
		for (int i = 0; i < peerUID.Length; i++)
		{
			LobbyScript.Instance.properties.players[peerUID[i]].isReady = readyStatus[i] == 1;
		}
		GameController.Instance.showLoadedPlayers();
	}

	#endregion
}
