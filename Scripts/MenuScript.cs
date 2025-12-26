using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;

public partial class MenuScript : Node
{
	//Menu refrences
	Control MouseBlocker;
	Control LobbyBrowser;
	Control LobbyCreator;
	Control PasswordPrompt;
	Control CreatePasswordField;

	public override void _Ready()
	{
		//Creating refrences
		MouseBlocker = GetNode<Control>("MouseBlocker");
		LobbyBrowser = GetNode<Control>("LobbyBrowser");
		LobbyCreator = GetNode<Control>("LobbyCreator");
		PasswordPrompt = GetNode<Control>("PasswordPrompt");
		CreatePasswordField = GetNode<Control>("LobbyCreator/MainVbox/ScrollContainer/VBoxContainer/Visibility/PasswordField");
		LobbyScript.Instance.LobbyListContainer = GetNode<VBoxContainer>("LobbyBrowser/MainVbox/LobbyScrollContainer/LobbyListVbox");
	}

	public override void _Process(double delta)
	{
		if (OS.HasFeature("dedicated_server"))
		{
			GetTree().ChangeSceneToFile("res://Scenes/ServerScene.tscn");
		}
	}

	public void joinLobby(long lobbyID, string password = "")
	{
		GD.Print("Joining Lobby: " + lobbyID + " with password: " + password);
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

		HBoxContainer ColorRow1 = GetNode<HBoxContainer>("ColorSelectPanel/ColorSelectMargin/MainVbox/ColorRow1");
		HBoxContainer ColorRow2 = GetNode<HBoxContainer>("ColorSelectPanel/ColorSelectMargin/MainVbox/ColorRow2");
		int i = 0;
		foreach(var node in ColorRow1.GetChildren().Concat(ColorRow2.GetChildren()))
		{
			Button button = node.GetChild(0) as Button;
			StyleBoxFlat normal = (StyleBoxFlat)button.GetThemeStylebox("normal");
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
			if(i%2 == 1)
			{
				button.Disabled = true;
				button.Icon = GD.Load<Texture2D>("res://Textures/exitbutton.png");
			}
			i++;
		}
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