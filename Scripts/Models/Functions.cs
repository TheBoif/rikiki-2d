using Godot;
using System;
using System.Text;
public static class Functions
{
    static Random random = new Random();
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
						value = val + 2,
                        texture = GD.Load<Texture2D>($"res://Textures/cards/{((CardSuit)suit).ToString().Substring(0,1)}{val + 2}.png")
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

    public static string GenerateLobbyID(int length)
    {
        
        const string pool = "abcdefghijklmnopqrstuvwxyz";

        var builder = new StringBuilder();

        while(LobbyScript.Instance.lobbies.ContainsKey(builder.ToString()) || builder.Length != length)
        {
            builder.Clear();
            for (var i = 0; i < length; i++)
            {
                char c = pool[random.Next(0, pool.Length)];
                builder.Append(c);
            }
        }



        return builder.ToString();
    }
}