using Godot;
using System;
using System.Security.Cryptography;

public partial class MenuScript : Node
{
	//Menu refrences
	Control MouseBlocker;
	Control LobbyBrowser;
	Control LobbyCreator;
	Control PasswordField;

	public override void _Ready()
	{
		//Creating refrences
		MouseBlocker = GetNode<Control>("MouseBlocker");
		LobbyBrowser = GetNode<Control>("LobbyBrowser");
		LobbyCreator = GetNode<Control>("LobbyCreator");
		LobbyScript.Instance.LobbyListContainer = GetNode<VBoxContainer>("LobbyBrowser/MainVbox/LobbyScrollContainer/LobbyListVbox");
	}

	public override void _Process(double delta)
	{
		if (OS.HasFeature("dedicated_server"))
		{
			GetTree().ChangeSceneToFile("res://Scenes/ServerScene.tscn");
		}
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

	#region Lobby Creator
	public void onVisibilityChanged(int index)
	{
		PasswordField = GetNode<Control>("LobbyCreator/MainVbox/ScrollContainer/VBoxContainer/Visibility/PasswordField");
		if(index == 1) //Protected
		{
			PasswordField.Visible = true;
		}
		else
		{
			PasswordField.Visible = false;
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
        LobbyScript.Instance.RpcId(1, "lobbyCreateReq", lobbyName, visibility, password, maxPlayers, maxCards, timeLimit, roundOrder, reveal, pointValues, GlobalScript.Instance.playerName, GlobalScript.Instance.playerColor, GlobalScript.Instance.peer.GetUniqueId());
		closeLobbyCreator();
	}
	#endregion

	
	#endregion
}