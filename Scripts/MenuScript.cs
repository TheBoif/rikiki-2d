using Godot;
using System;

public partial class MenuScript : Node
{
    public override void _Process(double delta)
    {
        LobbyScript.Instance.LobbyListContainer = GetNode<VBoxContainer>("LobbyBrowser/LobbyBrowserVbox/LobbyScrollContainer/LobbyListVbox");
        if (OS.HasFeature("dedicated_server"))
        {
            GetTree().ChangeSceneToFile("res://Scenes/ServerScene.tscn");
        }
    }    
    public void RefreshLobbies()
    {
        LobbyScript.Instance.RpcId(1, "viewLobbiesReq", GlobalScript.Instance.peer.GetUniqueId());
    }
    public void createLobby()
    {
        Random random = new Random();
        LobbyScript.Instance.RpcId(1, "createLobby", "Lobby " + random.NextInt64(), 0, 10, 6, "HostPlayer " + random.NextInt64(), Colors.Red, GlobalScript.Instance.peer.GetUniqueId());
    }
}