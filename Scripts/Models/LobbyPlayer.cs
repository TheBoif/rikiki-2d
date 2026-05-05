using System.Collections.Generic;
public class LobbyPlayer
{
	public string name;
	public int colorIndex;
	public long peerUID;
	public bool isReady = false;
	public int currentGuess = 0;
	public int currentHits = 0;
	public int score = 0;
	List<Card> hand = new List<Card>();

	public LobbyPlayer(string name, int colorIndex, int peerUID)
	{
		this.name = name;
		this.colorIndex = colorIndex;
		this.peerUID = peerUID;
	}
}