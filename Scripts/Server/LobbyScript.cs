using Godot;
using System;
using System.Collections.Generic;

public partial class LobbyScript : Node
{
    Dictionary<long,LobbyProperties> lobbies = new Dictionary<long, LobbyProperties>(); //for server
	public LobbyProperties properties; //for players
}
