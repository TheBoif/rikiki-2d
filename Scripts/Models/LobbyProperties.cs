using System.Collections.Generic;
public class LobbyProperties
{
	public string lobbyName;
	public LobbyVisibility visibility;
	public string password; //hashed password for protected lobbies
	public int maxPlayers;
	public int maxCards;
	public int timeLimit;
	public RoundOrder roundOrder;
	public bool revealCards;
    public bool gameStarted = false;
	public int[] pointValues;

	public string LobbyID;
	public Dictionary<long, LobbyPlayer> players;

	public LobbyProperties(string lobbyName, LobbyVisibility visibility, string password, int maxPlayers, int maxCards, int timeLimit, RoundOrder roundOrder, bool revealCards, int[] pointValues)
	{
		this.lobbyName = lobbyName;
		this.visibility = visibility;
		this.password = password;
		this.maxPlayers = maxPlayers;
		this.maxCards = maxCards;
		this.timeLimit = timeLimit;
		this.roundOrder = roundOrder;
		this.revealCards = revealCards;
		this.pointValues = pointValues;
		players = new Dictionary<long, LobbyPlayer>();
	}
}