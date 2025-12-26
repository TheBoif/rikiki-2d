using Godot;
using System;
using System.Collections.Generic;
#region Enumerators
public enum CardSuit
{
	Hearts,
	Diamonds,
	Clubs,
	Spades
}

public enum LobbyVisibility
{
	Public = 0,
	Protected = 1,
	Private = 2
}

public enum RoundOrder
{
	oneMaxOne = 0,
	maxOneMax = 1,
	oneMax = 2,
	maxOne = 3
}

#endregion
#region classes

public class Card
{
	public bool isred; //if the back of the card is red (true) or blue (false)
	public CardSuit cardSuit;
	public int value; //2,3,4,5,6,7,8,9,10,11(J),12(Q),13(K),14(A)
	public Texture2D texture; //front texture tied to the specific card
}

public class LobbyPlayer
{
	public string name;
	public Color color;
	public int peerUID;
	public int currentGuess = 0;
	public int currentHits = 0;
	public int score = 0;
	List<Card> hand = new List<Card>();
}

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
	public int[] pointValues;

	public long LobbyID;
	public List<LobbyPlayer> players;

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
		players = new List<LobbyPlayer>();
	}
}

public static class Functions
{
	public static void Shuffle(this Card[] deck)
	{
		for (int j = 0; j < 5; j++)
		{
			for (int i = 0; i < deck.Length; i++)
			{
				Random random = new Random();
				int randomIndex = random.Next(deck.Length);
				Card temp = deck[i];
				deck[i] = deck[randomIndex];
				deck[randomIndex] = temp;
			}
		}
	}

	public static Card[] CreateDeck()
	{
		Card[] deck = new Card[104];
		for (int red = 0; red < 2; red++)
		{
			for (int suit = 0; suit < 4; suit++)
			{
				for (int val = 0; val < 13; val++)
				{
					deck[red * 52 + suit * 13 + val] = new Card
					{
						isred = red == 1,
						cardSuit = (CardSuit)suit,
						value = val + 2
					};
				}
			}
		}
		return deck;
	}

	public static string HashString(string input)
	{
		using (var sha256 = System.Security.Cryptography.SHA256.Create())
		{
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
			byte[] hash = sha256.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}
	}

	public static Color[] PlayerColors = new Color[]
	{
		new Color("dd0900",1), //Red
		new Color("0042ff",1), //Blue
		new Color("006816",1), //Green
		new Color("ff4edc",1), //Pink
		new Color("ff6a00",1), //Orange
		new Color("ffd800",1), //Yellow
		new Color("333333",1), //Black
		new Color("d3d3d3",1), //White
		new Color("8200bf",1), //Purple
		new Color("772f00",1), //Brown
		new Color("00e8ff",1), //Cyan
		new Color("00ff3a",1), //Lime
	};
}
#endregion