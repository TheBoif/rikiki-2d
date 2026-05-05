using Godot;
public class Card
{
	public bool isred; //if the back of the card is red (true) or blue (false)
	public CardSuit cardSuit;
	public int value; //2,3,4,5,6,7,8,9,10,11(J),12(Q),13(K),14(A)
	public Texture2D texture; //front texture tied to the specific card
}