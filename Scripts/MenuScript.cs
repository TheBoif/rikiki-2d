using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;

public partial class MenuScript : Node
{
	#region Menu refrences
	[Export] Control LobbyView;
	[Export] Control MouseBlocker;
	[Export] Control LobbyBrowser;
	[Export] Control LobbyCreator;
	[Export] Control ColorSelectPanel;
	[Export] Control PasswordPrompt;
	[Export] Control StartGamePrompt;
	[Export] Control PopupPanel;
	[Export] Control CreatePasswordField;
	[Export] VBoxContainer LobbyListContainer;
	[Export] VBoxContainer PlayerListContainer;
	[Export] public GridContainer ColorGrid;

	private long _pendingJoinLobbyID;

	public override void _Ready()
	{
		LobbyScript.Instance.LobbyListContainer = LobbyListContainer;
		LobbyScript.Instance.PlayerListContainer = PlayerListContainer;

		PopupPanel.GetNode<Button>("Panel/MainVbox/OkButton").Pressed += () => PopupPanel.Visible = false;

		// Connect persistent UI signals once to prevent signal stacking
		PasswordPrompt.GetNode<Button>("Panel/MainVbox/Confirm").Pressed += OnConfirmPasswordPressed;

		int colorIdx = 0;
		foreach (var node in ColorGrid.GetChildren())
		{
			Button button = node.GetChild(0) as Button;
			int temp = colorIdx;
			button.Pressed += () =>
			{
				if (LobbyScript.Instance.properties != null)
				{
					LobbyScript.Instance.RpcId(1, nameof(LobbyScript.pickColorReq), GlobalScript.Instance.peer.GetUniqueId(), LobbyScript.Instance.properties.LobbyID, temp);
					MouseBlocker.Visible = false;
				}
			};
			colorIdx++;
		}
	}
	#endregion

	public override void _Process(double delta)
	{
		if (OS.HasFeature("dedicated_server") || OS.GetCmdlineArgs().ToList().Contains("--server"))
		{
			GetTree().ChangeSceneToFile("res://Scenes/ServerScene.tscn");
		}
	}

	public void joinLobby(long lobbyID, string password = "")
	{
		closeLobbyBrowser();
		PopupMessage("Joining Lobby", "Attempting to join lobby...");
		LobbyScript.Instance.RpcId(1, nameof(LobbyScript.lobbyJoinReq), GlobalScript.Instance.peer.GetUniqueId(), lobbyID, password, GlobalScript.Instance.playerName);
		closePasswordPrompt();
	}

	public void lobbyJoinedResp()
	{
		PopupPanel.Visible = false;
		LobbyView.Visible = true;
		LobbyScript.Instance.RpcId(1, nameof(LobbyScript.broadcastPlayerListUpdate), LobbyScript.Instance.properties.LobbyID);
		openColorSelectPanel();
	}

	public void lobbyCreatedResp()
	{
		LobbyView.Visible = true;
		LobbyScript.Instance.RpcId(1, nameof(LobbyScript.broadcastPlayerListUpdate), LobbyScript.Instance.properties.LobbyID);
		openColorSelectPanel();
	}

	public void openColorSelectPanel()
	{
		MouseBlocker.Visible = true;
		int colorIdx = 0;
		foreach (var node in ColorGrid.GetChildren())
		{
			Button button = node.GetChild(0) as Button;
			// Duplicating styleboxes ensures we don't modify shared resources
			StyleBoxFlat normal = (StyleBoxFlat)button.GetThemeStylebox("normal").Duplicate();
			StyleBoxFlat pressed = (StyleBoxFlat)button.GetThemeStylebox("pressed").Duplicate();
			StyleBoxFlat hover = (StyleBoxFlat)button.GetThemeStylebox("hover").Duplicate();
			StyleBoxFlat disabled = (StyleBoxFlat)button.GetThemeStylebox("disabled").Duplicate();

			normal.SetCornerRadiusAll(20);
			pressed.SetCornerRadiusAll(20);
			hover.SetCornerRadiusAll(20);
			disabled.SetCornerRadiusAll(20);

			normal.BgColor = Functions.PlayerColors[colorIdx];
			pressed.BgColor = Functions.PlayerColors[colorIdx].Darkened(0.4f);
			hover.BgColor = Functions.PlayerColors[colorIdx].Darkened(0.2f);
			disabled.BgColor = Functions.PlayerColors[colorIdx].Darkened(0.6f);

			button.AddThemeStyleboxOverride("normal", normal);
			button.AddThemeStyleboxOverride("pressed", pressed);
			button.AddThemeStyleboxOverride("hover", hover);
			button.AddThemeStyleboxOverride("disabled", disabled);
			colorIdx++;
		}
		ColorSelectPanel.Visible = true;
	}

	public void lobbyLeftResp(string reason = "Left")
	{
		LobbyView.Visible = false;
		switch (reason)
		{
			case "Kicked":
				PopupMessage("Lobby Left", "You were kicked from the lobby by the host.");
				break;
			case "Closed":
				PopupMessage("Lobby Left", "The lobby was closed by the host.");
				break;
			case "Left":
				PopupMessage("Lobby Left", "You have left the lobby.");
				break;
			default:
				PopupMessage("Lobby Left", "An unknown error occurred.");
				break;
		}
	}

	public void PopupMessage(string header, string message)
	{
		PopupPanel.GetNode<Label>("Panel/MainVbox/Header").Text = header;
		PopupPanel.GetNode<Label>("Panel/MainVbox/Message").Text = message;
		PopupPanel.Visible = true;
	}

	#region UI Button Methods
	public void exitGame()
	{
		GetTree().Quit();
	}
	public void RefreshLobbies()
	{
		LobbyScript.Instance.RpcId(1, nameof(LobbyScript.viewLobbiesReq), GlobalScript.Instance.peer.GetUniqueId());
	}
	public void openLobbyBrowser()
	{
		MouseBlocker.Visible = true;
		LobbyBrowser.Visible = true;
		RefreshLobbies();
	}
	public void closeLobbyBrowser()
	{
		MouseBlocker.Visible = false;
		LobbyBrowser.Visible = false;
	}

	public void openLobbyCreator()
	{
		MouseBlocker.Visible = true;
		LobbyCreator.Visible = true;
	}
	public void closeLobbyCreator()
	{
		MouseBlocker.Visible = false;
		LobbyCreator.Visible = false;
	}

	public void openPasswordPrompt(long lobbyID)
	{
		_pendingJoinLobbyID = lobbyID;
		PasswordPrompt.GetNode<LineEdit>("Panel/MainVbox/Field").Text = "";
		PasswordPrompt.Visible = true;
	}

	private void OnConfirmPasswordPressed()
	{
		string password = PasswordPrompt.GetNode<LineEdit>("Panel/MainVbox/Field").Text;
		joinLobby(_pendingJoinLobbyID, password);
	}

	public void closePasswordPrompt()
	{
		PasswordPrompt.Visible = false;
	}

	public void readyButtonToggled(bool pressed)
	{
		LobbyScript.Instance.RpcId(1, nameof(LobbyScript.readyStateChangedReq), GlobalScript.Instance.peer.GetUniqueId(), LobbyScript.Instance.properties.LobbyID, pressed);
	}

	public void startGameButton()
	{
		StartGamePrompt.GetNode<Button>("Panel/MainVbox/HBoxContainer/Confirm").Disabled = true;
		bool allReady = true;
		bool allHaveColors = true;
		foreach(var peer in LobbyScript.Instance.properties.players)
		{
			if(!peer.isReady) allReady = false;
			if(peer.colorIndex == -1)
			{
				allHaveColors = false;
				break;
			}
		}
		if(!allHaveColors)
		{
			StartGamePrompt.GetNode<Label>("Panel/MainVbox/Window Label").Text = "You cannot start the game until all players have selected a color!";
		}
		else
		{
			StartGamePrompt.GetNode<Button>("Panel/MainVbox/HBoxContainer/Confirm").Disabled = false;
			if(allReady) StartGamePrompt.GetNode<Label>("Panel/MainVbox/Window Label").Text = "Are you sure you want to start the game?";
			else StartGamePrompt.GetNode<Label>("Panel/MainVbox/Window Label").Text = "Are you sure you want to start the game?\nNot all players are ready!";
		}
		StartGamePrompt.Visible = true;
	}

	public void leaveLobbyButton()
	{
		LobbyScript.Instance.RpcId(1, nameof(LobbyScript.lobbyLeaveReq), GlobalScript.Instance.peer.GetUniqueId(), LobbyScript.Instance.properties.LobbyID);
	}

	public void closeStartPrompt()
	{
		StartGamePrompt.Visible = false;
	}

	public void ConfirmStart()
	{
		LobbyScript.Instance.RpcId(1, nameof(LobbyScript.startGameReq), GlobalScript.Instance.peer.GetUniqueId(), LobbyScript.Instance.properties.LobbyID);
	}

	#region Lobby Creator
	public void onVisibilityChanged(int index)
	{
		if(index == 1) //Protected
		{
			CreatePasswordField.Visible = true;
		}
		else
		{
			CreatePasswordField.Visible = false;
		}
	}

	public void onTurnTimeLimitChanged(float value)
	{
		Label timeLabel = GetNode<Label>("LobbyCreator/MainVbox/ScrollContainer/VBoxContainer/TimeLimit/DisabledLabel");
		timeLabel.Visible = value == 0;
	}

	public void createLobby()
	{
		VBoxContainer optionsContainer = GetNode<VBoxContainer>("LobbyCreator/MainVbox/ScrollContainer/VBoxContainer");
		String lobbyName = optionsContainer.GetNode<LineEdit>("Name/Field").Text;
		int visibility = optionsContainer.GetNode<OptionButton>("Visibility/Field").Selected;
		string password = "";
		if(visibility == 1)
		{
			password = Functions.HashString(optionsContainer.GetNode<LineEdit>("Visibility/PasswordField").Text);
		}
		int maxPlayers = Convert.ToInt32(optionsContainer.GetNode<SpinBox>("MaxPlayers/Field").Value);
		int maxCards = Convert.ToInt32(optionsContainer.GetNode<SpinBox>("MaxCards/Field").Value);
		int timeLimit = Convert.ToInt32(optionsContainer.GetNode<SpinBox>("TimeLimit/Field").Value);
		int roundOrder = optionsContainer.GetNode<OptionButton>("RoundOrder/Field").Selected;
		bool reveal = optionsContainer.GetNode<CheckButton>("RevealFirstCard/Field").ButtonPressed;
		int[] pointValues =
        [
            Convert.ToInt32(optionsContainer.GetNode<SpinBox>("CorrectPoints/Fix").Value),
            Convert.ToInt32(optionsContainer.GetNode<SpinBox>("CorrectPoints/Multi").Value),
            Convert.ToInt32(optionsContainer.GetNode<SpinBox>("MistakePoints/Fix").Value),
            Convert.ToInt32(optionsContainer.GetNode<SpinBox>("MistakePoints/Multi").Value),
        ];
        LobbyScript.Instance.RpcId(1, nameof(LobbyScript.lobbyCreateReq), lobbyName, visibility, password, maxPlayers, maxCards, timeLimit, roundOrder, reveal, pointValues, GlobalScript.Instance.playerName, GlobalScript.Instance.peer.GetUniqueId());
		closeLobbyCreator();
	}
	#endregion
	#endregion
}