using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LobbyScript : Node
{
	public static LobbyScript Instance { get; private set; }
	public override void _Ready()
	{
		Instance = this;
	}
	Dictionary<long,LobbyProperties> lobbies = new Dictionary<long, LobbyProperties>(); //for server
	public LobbyProperties properties; //for players

	#region Server Methods
	//Server run, sent by players
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void createLobby(string lobbyName, LobbyVisibility visibility, int maxCards, int maxPlayers, string hostName, Color hostColor, int hostPeerUID)
	{
		LobbyPlayer hostPlayer = new LobbyPlayer(){name=hostName, color=hostColor, peerUID=hostPeerUID};
		LobbyProperties newLobby = new LobbyProperties();
		newLobby.lobbyName = lobbyName;
		newLobby.LobbyID = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		newLobby.visibility = visibility;
		newLobby.maxCards = maxCards;
		newLobby.maxPlayers = maxPlayers;
		newLobby.players = new List<LobbyPlayer>();
		newLobby.players.Add(hostPlayer);
		lobbies.Add(newLobby.LobbyID, newLobby);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void closeLobby(long lobbyID)
	{
		foreach(var peer in lobbies[lobbyID].players)
		{
			RpcId(peer.peerUID, "lobbyClosedNotif");
		}
		lobbies.Remove(lobbyID);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void viewLobbiesReq(int callerPeerUID)
	{
		List<long> lobbyIDs = new List<long>();
		List<String> lobbyNames = new List<String>();
		List<String> playerCount = new List<String>();
		foreach(var lobby in lobbies)
		{
			if(lobby.Value.visibility == LobbyVisibility.Public)
			{
				lobbyIDs.Add(lobby.Key);
				lobbyNames.Add(lobby.Value.lobbyName);
				playerCount.Add(lobby.Value.players.Count + "/" + lobby.Value.maxPlayers);
			}
		}
		RpcId(callerPeerUID, "viewLobbiesResp", lobbyIDs.ToArray(), lobbyNames.ToArray(), playerCount.ToArray());
	}
	
	#endregion
	#region Player Methods
	//Player run, sent by server
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyClosedResp()
	{
		
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void viewLobbiesResp(long[] lobbyIDs, String[] lobbyNames, String[] playerCounts)
	{
		GD.Print("Lobbies Available:");
		for(int i = 0; i < lobbyIDs.Length; i++)
		{
			GD.Print("Lobby ID: " + lobbyIDs[i] + " | Name: " + lobbyNames[i] + " | Players: " + playerCounts[i]);
		}
	}
	#endregion

}
