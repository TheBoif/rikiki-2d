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
    Public,
    Protected,
    Private
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
    public ENetMultiplayerPeer peer;
}

public class LobbyProperties
{
    public string lobbyName;
    public long LobbyID;
    public LobbyVisibility visibility;
    public List<LobbyPlayer> players;
    public int maxCards;
    public int maxPlayers;
}

public static class Functions
{
    public static Card[] Shuffle(Card[] deck)
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
        return deck;
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

                    GD.Print("Red back: " + deck[red * 52 + suit * 13 + val].isred + "Created card: " + deck[red * 52 + suit * 13 + val].cardSuit + " " + deck[red * 52 + suit * 13 + val].value);
                }
            }
        }
        return deck;
    }
}
#endregion