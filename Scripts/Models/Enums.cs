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

public enum GameState
{
	Lobby = 0,
	Waiting = 1,
	Betting = 2,
	CardPlaying = 3,
	GameOver = 4
}