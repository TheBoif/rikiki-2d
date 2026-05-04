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
	}

	Dictionary<long,LobbyProperties> lobbies = new Dictionary<long, LobbyProperties>(); //for server
	public LobbyProperties properties; //for players

	#region Server Methods

	//Server run, sent by players

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyCreateReq(string lobbyName, LobbyVisibility visibility, string password, int maxPlayers, int maxCards, int timeLimit, RoundOrder roundOrder, bool reveal, int[] pointValues, string hostName, int hostPeerUID)
	{
		LobbyPlayer hostPlayer = new LobbyPlayer(hostName, -1, hostPeerUID);
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

		RpcId(hostPeerUID, "lobbyCreateResp",newLobby.LobbyID, lobbyName, (int)visibility, password, maxPlayers, maxCards, timeLimit, (int)roundOrder, reveal, pointValues);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void lobbyJoinReq(int joiningUID, long lobbyID, string password, string playerName)
	{
		if(!lobbies.ContainsKey(lobbyID))
		{
			RpcId(joiningUID, "lobbyJoinError", "badId");
			return;
		}

		if(lobbies[lobbyID].maxPlayers <= lobbies[lobbyID].players.Count)
		{
			RpcId(joiningUID, "lobbyJoinError", "full");
			return;
		}

		if(lobbies[lobbyID].password != "" && Functions.HashString(password) != lobbies[lobbyID].password)
		{
			RpcId(joiningUID, "lobbyJoinError", "badPassword");
			return;
		}

		RpcId(joiningUID, "lobbyJoinResp", lobbyID, lobbies[lobbyID].lobbyName, (int)lobbies[lobbyID].visibility, lobbies[lobbyID].password, lobbies[lobbyID].maxPlayers, lobbies[lobbyID].maxCards, lobbies[lobbyID].timeLimit, (int)lobbies[lobbyID].roundOrder, lobbies[lobbyID].revealCards, lobbies[lobbyID].pointValues);
		lobbies[lobbyID].players.Add(new LobbyPlayer(playerName, -1, joiningUID));
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void pickColorReq(int peerUID, long lobbyID, int colorIndex)
	{
		foreach(var player in lobbies[lobbyID].players)
		{
			if(player.colorIndex == colorIndex)
			{
				broadcastPlayerListUpdate(lobbyID);
				return;
			}
		}

		lobbies[lobbyID].players.Find(p => p.peerUID == peerUID).colorIndex = colorIndex;
		broadcastPlayerListUpdate(lobbyID);
		RpcId(peerUID, "pickColorResp");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void readyStateChangedReq(int peerUID, long lobbyID, bool isReady)
	{
		lobbies[lobbyID].players.Find(p => p.peerUID == peerUID).isReady = isReady;
		broadcastPlayerListUpdate(lobbyID);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void startGameReq(int peerUID, long lobbyID)
	{
		foreach(var peer in lobbies[lobbyID].players)
		{
			RpcId(peer.peerUID, "startGameResp");
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void broadcastPlayerListUpdate(long lobbyID)
	{
		List<String> playerNames = new List<String>();
		List<int> playerColorIndeces = new List<int>();
		List<int> playerUIDs = new List<int>();
		List<int> readyStatus = new List<int>();
		foreach(var peer in lobbies[lobbyID].players)
		{
			playerNames.Add(peer.name);
			playerColorIndeces.Add(peer.colorIndex);
			playerUIDs.Add(peer.peerUID);
			readyStatus.Add(peer.isReady ? 1 : 0);
		}
		foreach(var peer in lobbies[lobbyID].players)
		{
			RpcId(peer.peerUID, "updatePlayerList", playerNames.ToArray(), playerColorIndeces.ToArray(), playerUIDs.ToArray(), readyStatus.ToArray());
		}
	}

	#endregion
	#region Player Methods
	//Player run, sent by server

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyCreateResp(long lobbyID, string lobbyName, LobbyVisibility visibility, string password, int maxPlayers, int maxCards, int timeLimit, RoundOrder roundOrder, bool reveal, int[] pointValues)
	{
        properties = new LobbyProperties(lobbyName, visibility, password, maxPlayers, maxCards, timeLimit, roundOrder, reveal, pointValues)
        {
            LobbyID = lobbyID
        };
        menuScript.lobbyCreatedResp();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyJoinResp(long lobbyID = 0, string lobbyName = "", LobbyVisibility visibility = 0, string password = "", int maxPlayer = 0, int maxCards = 0, int timeLimit = 0, RoundOrder roundOrder = 0, bool reveal = false, int[] pointValues = null)
	{
		properties = new LobbyProperties(lobbyName, visibility, password, maxPlayer, maxCards, timeLimit, roundOrder, reveal, pointValues)
		{
			LobbyID = lobbyID
		};
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	public void lobbyJoinError(string result)
	{
		switch(result)
		{
			case "badId":
				menuScript.PopupMessage("Failed to Join Lobby", "Lobby does not exist.");
				break;
			case "full":
				menuScript.PopupMessage("Failed to Join Lobby", "Lobby is full.");
				break;
			case "badPassword":
				menuScript.PopupMessage("Failed to Join Lobby", "Incorrect password.");
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
	public void viewLobbiesResp(long[] lobbyIDs, String[] lobbyNames, String[] playerCounts, int[] passwordProtected)
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

		for(int i = 0; i < playerNames.Length; i++)
		{
			properties.players.Add(new LobbyPlayer(playerNames[i], playerColorIndeces[i], peerUIDs[i]));
			properties.players[i].isReady = readyStatuses[i] == 1;
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
				properties.players[i].isReady = true;
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
		GetTree().ChangeSceneToFile("res://Scenes/GameScene.tscn");
	}

	#endregion
}