using Godot;
using System;
using System.Diagnostics;
using System.Collections.Generic;
//84.3.73.101 - my public ip
public partial class ServerScript : Node
{
    bool isServer = false;
    public override void _Ready()
    {
        if (!OS.HasFeature("dedicated_server"))
        {
            isServer = true;
            GD.Print("Server started");
        }
    }
    /*
    Dictionary<long,LobbyScript> lobbies = new Dictionary<long, LobbyScript>();

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void createLobby(LobbyProperties lobbyProperties, ENetMultiplayerPeer callerPeer)
    {
        if(isServer)
        {
            LobbyScript newLobby = new LobbyScript();
            newLobby.startLobby(lobbyProperties);
            AddChild(newLobby);
            lobbies.Add(newLobby.properties.LobbyID, newLobby);
            newLobby.Name = "Lobby - " + newLobby.properties.lobbyName;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void joinLobbyPeer(long lobbyID, LobbyPlayer newPlayer)
    {
        if(isServer)
        {
            if (lobbies.ContainsKey(lobbyID))
            {
                lobbies[lobbyID].joinLobby(newPlayer);
            }
            else
            {
                GD.Print("Lobby with ID " + lobbyID + " does not exist.");
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]

    public void LobbyUpdate(LobbyScript lobby)
    {
        
    }
    */
}