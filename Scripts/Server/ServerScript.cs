using Godot;
using System;
using System.Diagnostics;
using System.Collections.Generic;
//84.3.73.101 - my public ip
public partial class ServerScript : Node
{
    public static ServerScript Instance { get; private set; }
    bool isServer = false;

    public ENetMultiplayerPeer peer;

    // Networking defaults
    public const int DefaultPort = 25565;
    public const int MaxClients = 4095;
    public string ServerIp = "127.0.0.1";
    bool setupDone = false;

    public override void _Ready()
    {
        Instance = this;

        // Determine role correctly: if the dedicated_server feature is present, this instance should run as server
        isServer = OS.HasFeature("dedicated_server");

        peer = new ENetMultiplayerPeer();

        if (isServer)
        {
            var err = peer.CreateServer(DefaultPort, MaxClients);
            if (err == Error.Ok)
            {
                Multiplayer.MultiplayerPeer = peer;
                GD.Print($"Server started and listening on port {DefaultPort}");
            }
            else
            {
                GD.PrintErr($"Failed to create server: {err}");
            }
        }
        else
        {
            var err = peer.CreateClient(ServerIp, DefaultPort);
            if (err == Error.Ok)
            {
                Multiplayer.MultiplayerPeer = peer;
                GD.Print($"Client created and attempting connect to {ServerIp}:{DefaultPort}");
            }
            else
            {
                GD.PrintErr($"Failed to create client to {ServerIp}:{DefaultPort} - {err}");
            }
        }
    }

    public override void _Process(double delta)
    {
        if(!setupDone)
        {
            GlobalScript.Instance.peer = peer;
            setupDone = true;
        }
    }
}