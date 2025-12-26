using Godot;
using System;
using System.Collections.Generic;

public partial class LobbyScript : Node
{
	public VBoxContainer LobbyListContainer;
	public static LobbyScript Instance { get; private set; }
	public PackedScene LobbyListItem = GD.Load<PackedScene>("res://Scenes/LobbyListItem.tscn");
	MenuScript menuScript;
	int i = 0;
	public override void _Ready()
	{
		menuScript = GetTree().Root.GetNode("Menu") as MenuScript;
		Instance = this;
	}

	Dictionary<long,LobbyProperties> lobbies = new Dictionary<long, LobbyProperties>(); //for server
	public LobbyProperties properties; //for players

	#region Server Methods
	//Server run, sent by players
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyCreateReq(string lobbyName, LobbyVisibility visibility, string password, int maxPlayers, int maxCards, int timeLimit, RoundOrder roundOrder, bool reveal, int[] pointValues, string hostName, int hostPeerUID)
	{
		LobbyPlayer hostPlayer = new LobbyPlayer(){name=hostName, peerUID=hostPeerUID};
		LobbyProperties newLobby = new LobbyProperties(lobbyName, visibility, password, maxPlayers, maxCards, timeLimit, roundOrder, reveal, pointValues);

		long id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		newLobby.LobbyID = id;
		if(lobbies.ContainsKey(id))
		{
			int i = 1;
			while(lobbies.ContainsKey(id + i))
			{
				i++;
			}
			newLobby.LobbyID = id + i;
		}
		newLobby.players.Add(hostPlayer);
		lobbies.Add(newLobby.LobbyID, newLobby);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyCloseReq(long lobbyID)
	{
		foreach(var peer in lobbies[lobbyID].players)
		{
			RpcId(peer.peerUID, "lobbyClosedResp");
		}
		lobbies.Remove(lobbyID);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void viewLobbiesReq(int callerPeerUID)
	{
		List<long> lobbyIDs = new List<long>();
		List<String> lobbyNames = new List<String>();
		List<String> playerCount = new List<String>();
		List<int> passwordProtected = new List<int>();
		foreach(var lobby in lobbies)
		{
			if(lobby.Value.visibility != LobbyVisibility.Private)
			{
				lobbyIDs.Add(lobby.Key);
				lobbyNames.Add(lobby.Value.lobbyName);
				playerCount.Add(lobby.Value.players.Count + "/" + lobby.Value.maxPlayers);
				passwordProtected.Add(LobbyVisibility.Protected == lobby.Value.visibility? 1 : 0);
			}
		}
		RpcId(callerPeerUID, "viewLobbiesResp", lobbyIDs.ToArray(), lobbyNames.ToArray(), playerCount.ToArray(), passwordProtected.ToArray());
	}
	
	#endregion
	#region Player Methods
	//Player run, sent by server

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyCreateResp(int closerUID)
	{
		if(GlobalScript.Instance.peer.GetUniqueId() == closerUID)
		{
			//closer
		}
		else
		{
			//everyone else
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyJoinReq(int joiningUID, long lobbyID, string password)
	{
		if(!lobbies.ContainsKey(lobbyID))
		{
			RpcId(joiningUID, "lobbyJoinResp", "badId");
			return;
		}
		if(lobbies[lobbyID].maxPlayers >= lobbies[lobbyID].players.Count)
		{
			RpcId(joiningUID, "lobbyJoinResp", "full");
			return;
		}
		if(lobbies[lobbyID].password != "")
		{
			if(Functions.HashString(password) == lobbies[lobbyID].password)
			{
				RpcId(joiningUID, "lobbyJoinResp", "success");
			}
			else
			{
				RpcId(joiningUID, "lobbyJoinResp", "badPassword");
				return;
			}
		}
		else
		{
			RpcId(joiningUID, "lobbyJoinResp", "success");
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyJoinResp(String result)
	{
		switch(result)
		{
			case "success":
				GD.Print("Joined lobby successfully.");
				break;
			case "badId":
				GD.Print("Failed to join lobby: Lobby does not exist.");
				break;
			case "full":
				GD.Print("Failed to join lobby: Lobby is full.");
				break;
			case "badPassword":
				GD.Print("Failed to join lobby: Incorrect password.");
				break;
			default:
				GD.Print("Failed to join lobby: Unknown error.");
				break;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyClosedResp()
	{
		
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void viewLobbiesResp(long[] lobbyIDs, String[] lobbyNames, String[] playerCounts, int[] passwordProtected)
	{
		foreach(Node child in LobbyListContainer.GetChildren())
		{
			child.QueueFree();
		}
		if(lobbyIDs.Length == 0)
		{
			Label noLobbiesLabel = new Label();
			noLobbiesLabel.Text = "No lobbies available.";
			noLobbiesLabel.HorizontalAlignment = HorizontalAlignment.Center;
			noLobbiesLabel.VerticalAlignment = VerticalAlignment.Center;
			noLobbiesLabel.GrowHorizontal = Control.GrowDirection.Both;
			noLobbiesLabel.GrowVertical = Control.GrowDirection.Both;
			noLobbiesLabel.AddThemeFontSizeOverride("font_size", 50);
			LobbyListContainer.AddChild(noLobbiesLabel);
			return;
		}
		for(int i = 0; i < lobbyIDs.Length; i++)
		{
			Node newLobbyListItem = LobbyListItem.Instantiate();
			newLobbyListItem.GetNode<Label>("LobbyHbox/Name").Text = lobbyNames[i];
			newLobbyListItem.GetNode<Label>("LobbyHbox/PlayerCount").Text = playerCounts[i];
			newLobbyListItem.GetNode<TextureRect>("LobbyHbox/PasswordProtected").Visible = passwordProtected[i] == 1;
			long lobbyID = lobbyIDs[i];
			if(passwordProtected[i] == 1)
			{
				newLobbyListItem.GetNode<Button>("LobbyHbox/JoinButton").Pressed += () => menuScript.openPasswordPrompt(lobbyID);
			}
			else
			{
				newLobbyListItem.GetNode<Button>("LobbyHbox/JoinButton").Pressed += () => menuScript.joinLobby(lobbyID);
			}
			LobbyListContainer.AddChild(newLobbyListItem);
		}
	}
	#endregion
}