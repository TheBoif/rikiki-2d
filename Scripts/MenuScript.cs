using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;

public partial class MenuScript : Node
{
	#region Menu refrences
	Control MouseBlocker;
	Control LobbyBrowser;
	Control LobbyCreator;
	Control PasswordPrompt;
	Control CreatePasswordField;
	Control PopupPanel;
	Control LobbyView;
	Control LobbyPlayerList;

	public override void _Ready()
	{
		//Creating refrences
		MouseBlocker = GetNode<Control>("MouseBlocker");
		LobbyBrowser = GetNode<Control>("LobbyBrowser");
		LobbyCreator = GetNode<Control>("LobbyCreator");
		PasswordPrompt = GetNode<Control>("PasswordPrompt");
		LobbyView = GetNode<Control>("LobbyView");
		PopupPanel = GetNode<Control>("PopupPanel");
		CreatePasswordField = GetNode<Control>("LobbyCreator/MainVbox/ScrollContainer/VBoxContainer/Visibility/PasswordField");
		LobbyScript.Instance.LobbyListContainer = GetNode<VBoxContainer>("LobbyBrowser/MainVbox/LobbyScrollContainer/LobbyListVbox");
		LobbyScript.Instance.PlayerListContainer = GetNode<VBoxContainer>("LobbyView/MainVbox/LobbyScrollContainer/PlayerListVbox");
		PopupPanel.GetNode<Button>("Panel/MainVbox/OkButton").Pressed += () => PopupPanel.Visible = false;
	}
	#endregion

	public override void _Process(double delta)
	{
		if (OS.HasFeature("dedicated_server"))
		{
			GetTree().ChangeSceneToFile("res://Scenes/ServerScene.tscn");
		}
	}

	public void joinLobby(long lobbyID, string password = "")
	{
		PopupMessage("Joining Lobby", "Attempting to join lobby...");
		LobbyScript.Instance.RpcId(1, "lobbyJoinReq", GlobalScript.Instance.peer.GetUniqueId(), lobbyID, password, GlobalScript.Instance.playerName);
		closePasswordPrompt();
	}

	public void lobbyJoinedResp()
	{
		LobbyView.Visible = true;
		PopupMessage("Lobby Joined", "Successfully joined the lobby.");
		LobbyScript.Instance.RpcId(1, "broadcastPlayerListUpdate", LobbyScript.Instance.properties.LobbyID);
	}

	public void lobbyCreatedResp()
	{
		LobbyView.Visible = true;
		PopupMessage("Lobby Created", "Successfully created lobby.");
		LobbyScript.Instance.RpcId(1, "broadcastPlayerListUpdate", LobbyScript.Instance.properties.LobbyID);
	}

	public void openColorSelectPanel()
	{
		GridContainer colorGrid = GetNode<GridContainer>("ColorSelectPanel/ColorSelectMargin/MainVbox/ColorGrid");
		int i = 0;
		foreach(var node in colorGrid.GetChildren())
		{
			Button button = node.GetChild(0) as Button;
			StyleBoxFlat normal = (StyleBoxFlat)button.GetThemeStylebox("normal");
			StyleBoxFlat a = new StyleBoxFlat();
			StyleBoxFlat pressed = (StyleBoxFlat)button.GetThemeStylebox("pressed");
			StyleBoxFlat hover = (StyleBoxFlat)button.GetThemeStylebox("hover");
			StyleBoxFlat disabled = (StyleBoxFlat)button.GetThemeStylebox("disabled");
			normal.SetCornerRadiusAll(20);
			pressed.SetCornerRadiusAll(20);
			hover.SetCornerRadiusAll(20);
			disabled.SetCornerRadiusAll(20);
			normal.BgColor = Functions.PlayerColors[i];
			pressed.BgColor = Functions.PlayerColors[i].Darkened(0.4f);
			hover.BgColor = Functions.PlayerColors[i].Darkened(0.2f);
			disabled.BgColor = Functions.PlayerColors[i].Darkened(0.6f);
			button.AddThemeStyleboxOverride("normal", normal);
			button.AddThemeStyleboxOverride("pressed", pressed);
			button.AddThemeStyleboxOverride("hover", hover);
			button.AddThemeStyleboxOverride("disabled", disabled);
			i++;
		}
		colorGrid.Visible = true;
	}

	public void lobbyLeftResp(string reason = "Left")
	{
		//called when a lobby is left
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
		LobbyScript.Instance.RpcId(1, "viewLobbiesReq", GlobalScript.Instance.peer.GetUniqueId());
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
		PasswordPrompt.Visible = true;
		PasswordPrompt.GetNode<Button>("Panel/MainVbox/Confirm").Pressed += () => joinLobby(lobbyID, PasswordPrompt.GetNode<LineEdit>("Panel/MainVbox/Field").Text);
	}

	public void closePasswordPrompt()
	{
		PasswordPrompt.Visible = false;
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
        LobbyScript.Instance.RpcId(1, "lobbyCreateReq", lobbyName, visibility, password, maxPlayers, maxCards, timeLimit, roundOrder, reveal, pointValues, GlobalScript.Instance.playerName, GlobalScript.Instance.peer.GetUniqueId());
		closeLobbyCreator();
	}
	#endregion
	#endregion
}