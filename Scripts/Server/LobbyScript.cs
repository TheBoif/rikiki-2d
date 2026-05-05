using Godot;
using System;
using System.Collections.Generic;

public partial class LobbyScript : Node
{
	public VBoxContainer LobbyListContainer;
	public VBoxContainer PlayerListContainer;
	public static LobbyScript Instance { get; private set; }
	public PackedScene LobbyListItem = GD.Load<PackedScene>("res://Scenes/LobbyListItem.tscn");
	public PackedScene PlayerListItem = GD.Load<PackedScene>("res://Scenes/PlayerListItem.tscn");
	MenuScript menuScript;
	int i = 0;
	public override void _Ready()
	{
		menuScript = GetTree().Root.GetNode("Menu") as MenuScript;
		Instance = this;

		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
	}

	// lobby list for server to keep track, for players to be listed in the browser
	public Dictionary<string ,LobbyProperties> lobbies = new Dictionary<string, LobbyProperties>();
	public LobbyProperties properties; //current lobby for players

	#region Server Methods

	//Server run, sent by players

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyCreateReq(string lobbyName, LobbyVisibility visibility, string password, int maxPlayers, int maxCards, int timeLimit, RoundOrder roundOrder, bool reveal, int[] pointValues, string hostName, int hostPeerUID)
	{
		LobbyPlayer hostPlayer = new LobbyPlayer(hostName, -1, hostPeerUID);
		LobbyProperties newLobby = new LobbyProperties(lobbyName, visibility, password, maxPlayers, maxCards, timeLimit, roundOrder, reveal, pointValues);

		string id = Functions.GenerateLobbyID(5);
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
		newLobby.players.Add(hostPlayer.peerUID,hostPlayer);
		lobbies.Add(newLobby.LobbyID, newLobby);

		RpcId(hostPeerUID, nameof(lobbyCreateResp), newLobby.LobbyID, lobbyName, (int)visibility, password, maxPlayers, maxCards, timeLimit, (int)roundOrder, reveal, pointValues);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyJoinReq(int joiningUID, string lobbyID, string password, string playerName)
	{
		if (!lobbies.ContainsKey(lobbyID))
		{
			RpcId(joiningUID, nameof(lobbyJoinError), "badId");
			return;
		}

		LobbyProperties lobby = lobbies[lobbyID];

		if (lobby.gameStarted)
		{
			RpcId(joiningUID, nameof(lobbyJoinError), "started");
			return;
		}


		if (lobby.maxPlayers <= lobby.players.Count)
		{
			RpcId(joiningUID, nameof(lobbyJoinError), "full");
			return;
		}

		if (!string.IsNullOrEmpty(lobby.password) && Functions.HashString(password) != lobby.password)
		{
			RpcId(joiningUID, nameof(lobbyJoinError), "badPassword");
			return;
		}

		if (lobby.players.ContainsKey(joiningUID))
		{
			RpcId(joiningUID, nameof(lobbyJoinError), "alreadyInLobby");
			return;
		}

		GD.Print($"Player {playerName} with UID {joiningUID} joined lobby {lobbyID}");

		lobby.players.Add(joiningUID, new LobbyPlayer(playerName, -1, joiningUID));
		RpcId(joiningUID, nameof(lobbyJoinResp), lobbyID, lobby.lobbyName, (int)lobby.visibility, lobby.password, lobby.maxPlayers, lobby.maxCards, lobby.timeLimit, (int)lobby.roundOrder, lobby.revealCards, lobby.pointValues);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyLeaveReq(long peerUID, string lobbyID)
	{
		if (lobbies.ContainsKey(lobbyID))
		{
			var lobby = lobbies[lobbyID];
			var player = lobby.players.GetValueOrDefault(peerUID);
			if (player != null)
			{
				lobby.players.Remove(peerUID);
				if(lobby.players.Count == 0)
				{
					lobbies.Remove(lobbyID);
					GD.Print($"Lobby {lobbyID} removed due to all players leaving.");
				}
				RpcId(peerUID, nameof(lobbyLeaveResp));
				broadcastPlayerListUpdate(lobbyID);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyCloseReq(string lobbyID)
	{
		foreach(var peer in lobbies[lobbyID].players.Values)
		{
			RpcId(peer.peerUID, nameof(lobbyClosedResp));
		}
		lobbies.Remove(lobbyID);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void viewLobbiesReq(int callerPeerUID)
	{
		List<string> lobbyIDs = new List<string>();
		List<String> lobbyNames = new List<String>();
		List<String> playerCount = new List<String>();
		List<int> passwordProtected = new List<int>();
		foreach(var lobby in lobbies.Values)
		{
			if(lobby.visibility != LobbyVisibility.Private)
			{
				lobbyIDs.Add(lobby.LobbyID);
				lobbyNames.Add(lobby.lobbyName);
				playerCount.Add(lobby.players.Count + "/" + lobby.maxPlayers);
				passwordProtected.Add(LobbyVisibility.Protected == lobby.visibility? 1 : 0);
			}
		}
		RpcId(callerPeerUID, nameof(viewLobbiesResp), lobbyIDs.ToArray(), lobbyNames.ToArray(), playerCount.ToArray(), passwordProtected.ToArray());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void pickColorReq(long peerUID, string lobbyID, int colorIndex)
	{
		foreach(var player in lobbies[lobbyID].players.Values)
		{
			if(player.colorIndex == colorIndex)
			{
				broadcastPlayerListUpdate(lobbyID);
				return;
			}
		}

		lobbies[lobbyID].players[peerUID].colorIndex = colorIndex;
		broadcastPlayerListUpdate(lobbyID);
		RpcId(peerUID, nameof(pickColorResp));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void readyStateChangedReq(long peerUID, string lobbyID, bool isReady)
	{
		lobbies[lobbyID].players[peerUID].isReady = isReady;
		broadcastPlayerListUpdate(lobbyID);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void startGameReq(long peerUID, string lobbyID)
	{
		foreach(var peer in lobbies[lobbyID].players.Values)
		{
			lobbies[lobbyID].players[peerUID].isReady = false;
			RpcId(peer.peerUID, nameof(startGameResp));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void broadcastPlayerListUpdate(string lobbyID)
	{
		List<String> playerNames = new List<String>();
		List<int> playerColorIndeces = new List<int>();
		List<long> playerUIDs = new List<long>();
		List<int> readyStatus = new List<int>();
		GD.Print("Broadcasting player list update for lobby " + lobbyID);
		foreach(var peer in lobbies[lobbyID].players.Values)
		{
			GD.Print($"Player in lobby: {peer.name}, color index: {peer.colorIndex}, UID: {peer.peerUID}, ready: {peer.isReady}");
			playerNames.Add(peer.name);
			playerColorIndeces.Add(peer.colorIndex);
			playerUIDs.Add(peer.peerUID);
			readyStatus.Add(peer.isReady ? 1 : 0);
		}
		foreach(var peer in lobbies[lobbyID].players.Values)
		{
			RpcId(peer.peerUID, nameof(updatePlayerList), playerNames.ToArray(), playerColorIndeces.ToArray(), playerUIDs.ToArray(), readyStatus.ToArray());
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void playerLoadedToGameReq(string lobbyID, long peerUID)
	{
		lobbies[lobbyID].players[peerUID].isReady = true;
		foreach(var peer in lobbies[lobbyID].players.Values)
		{
			RpcId(peer.peerUID, nameof(playerLoadedToGameResp), lobbyID, peerUID);
		}
	}

	#endregion

	#region Network Event Handlers

	private void OnPeerDisconnected(long peerUID)
	{
		if (!Multiplayer.IsServer()) return;

		foreach (var lobbyPair in lobbies.Values)
		{
			var lobby = lobbyPair;
			if (lobby.players.ContainsKey(peerUID))
			{
				lobby.players.Remove(peerUID);
				if(lobby.players.Count == 0)
				{
					lobbies.Remove(lobbyPair.LobbyID);
					GD.Print($"Lobby {lobbyPair.LobbyID} removed due to all players leaving.");
				}
				broadcastPlayerListUpdate(lobby.LobbyID);
				break;
			}
		}
	}

	private void OnServerDisconnected()
	{
		properties = null;
		menuScript.PopupMessage("Disconnected", "Connection to the server was lost.");
	}

	private void OnConnectionFailed()
	{
		menuScript.PopupMessage("Connection Failed", "Unable to connect to the server.");
	}

	#endregion

	#region Player Methods
	//Player run, sent by server

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyCreateResp(string lobbyID, string lobbyName, LobbyVisibility visibility, string password, int maxPlayers, int maxCards, int timeLimit, RoundOrder roundOrder, bool reveal, int[] pointValues)
	{
        properties = new LobbyProperties(lobbyName, visibility, password, maxPlayers, maxCards, timeLimit, roundOrder, reveal, pointValues)
        {
            LobbyID = lobbyID
        };
        menuScript.lobbyCreatedResp();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyJoinResp(string lobbyID, string lobbyName, LobbyVisibility visibility, string password, int maxPlayer, int maxCards, int timeLimit, RoundOrder roundOrder, bool reveal, int[] pointValues)
	{
		properties = new LobbyProperties(lobbyName, visibility, password, maxPlayer, maxCards, timeLimit, roundOrder, reveal, pointValues)
		{
			LobbyID = lobbyID
		};
		menuScript.lobbyJoinedResp();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyLeaveResp()
	{
		menuScript.lobbyLeftResp();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyJoinError(string result)
	{
		switch(result)
		{
			case "badId":
				menuScript.PopupMessage("Failed to Join Lobby", "This lobby no longer exists.");
				break;
			case "started":
				menuScript.PopupMessage("Failed to Join Lobby", "This game has already started.");
				break;
			case "full":
				menuScript.PopupMessage("Failed to Join Lobby", "Lobby is full.");
				break;
			case "badPassword":
				menuScript.PopupMessage("Failed to Join Lobby", "Incorrect password.");
				break;
			case "alreadyInLobby":
				menuScript.PopupMessage("Failed to Join Lobby", "A player with your UID is already in this lobby.");
				break;
			default:
				menuScript.PopupMessage("Failed to Join Lobby", "An unknown error occurred.");
				break;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyClosedResp()
	{
		menuScript.lobbyLeftResp("closed");
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void viewLobbiesResp(string[] lobbyIDs, String[] lobbyNames, String[] playerCounts, int[] passwordProtected)
	{
		foreach(Node child in LobbyListContainer.GetChildren())
		{
			child.QueueFree();
		}
		if(lobbyIDs.Length == 0)
		{
            Label noLobbiesLabel = new Label
            {
                Text = "No lobbies available.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both
            };
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
			string lobbyID = lobbyIDs[i];
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

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void updatePlayerList(String[] playerNames, int[] playerColorIndeces, int[] peerUIDs, int[] readyStatuses)
	{
		if(properties == null)
		{
			GD.PrintErr("Tried to update player list while not in a lobby!");
			return;
		}

		properties.players.Clear();
		foreach(Node child in PlayerListContainer.GetChildren())
		{
			child.QueueFree();
		}

		foreach(AspectRatioContainer arc in menuScript.ColorGrid.GetChildren())
		{
			Button button = arc.GetChild(0) as Button;
			button.Disabled = false;
			button.Icon = null;
		}

		for(int i = 0; i < playerNames.Length; i++)
		{
			properties.players.Add(peerUIDs[i], new LobbyPlayer(playerNames[i], playerColorIndeces[i], peerUIDs[i]));
			properties.players[peerUIDs[i]].isReady = readyStatuses[i] == 1;
			Node playerListItem = PlayerListItem.Instantiate();
			playerListItem.GetNode<Label>("PlayerHbox/Name").Text = playerNames[i];
			if(playerColorIndeces[i] != -1)
			{
				playerListItem.GetNode<Control>("Border").Visible = false;
				StyleBoxFlat playerBG = new StyleBoxFlat();
				playerBG.BgColor = Functions.PlayerColors[playerColorIndeces[i]].Darkened(0.2f);
				playerBG.SetCornerRadiusAll(5);
				((Panel)playerListItem).AddThemeStyleboxOverride("panel", playerBG);

				Button colorbtn =  menuScript.ColorGrid.GetChild(playerColorIndeces[i]).GetChild(0) as Button;
				colorbtn.Disabled = true;
				colorbtn.Icon = GD.Load<Texture2D>("res://Textures/exitbutton.png");
			}

			if(i == 0)
			{
				properties.players[peerUIDs[i]].isReady = true;
				Label readyIndicator = playerListItem.GetNode<Label>("PlayerHbox/ReadyIndicator");
				readyIndicator.Text = "Host";
				readyIndicator.AddThemeColorOverride("font_color", Colors.White);
				readyIndicator.Visible = true;
				if(GlobalScript.Instance.peer.GetUniqueId() == peerUIDs[i])
				{
					menuScript.GetNode<Button>("LobbyView/MainVbox/ReadyButton").Visible = false;
					menuScript.GetNode<Button>("LobbyView/MainVbox/StartButton").Visible = true;
				}
			}
			else
			{
				if(GlobalScript.Instance.peer.GetUniqueId() == peerUIDs[i])
				{
					menuScript.GetNode<Button>("LobbyView/MainVbox/ReadyButton").Visible = true;
					menuScript.GetNode<Button>("LobbyView/MainVbox/StartButton").Visible = false;
				}
				playerListItem.GetNode<Label>("PlayerHbox/ReadyIndicator").Visible = readyStatuses[i] != 0;
			}
			PlayerListContainer.AddChild(playerListItem);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void pickColorResp()
	{
		menuScript.GetNode<PanelContainer>("ColorSelectPanel").Visible = false;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void startGameResp()
	{
		GlobalScript.Instance.
		GetTree().ChangeSceneToFile("res://Scenes/GameScene.tscn");
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void playerLoadedToGameResp(string lobbyID, long peerUID)
	{
		lobbies[lobbyID].players[peerUID].isReady = true;
	}

	#endregion
}