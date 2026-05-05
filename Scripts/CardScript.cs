using Godot;
using System;

public partial class CardScript : SubViewportContainer
{
	private Tween scaleTween;
	private Tween posTween;
	ShaderMaterial shaderMaterial;
	Vector2 trueScale;
	Vector2 truePos;
	int originalIndex;
	private float targetXRot = 0f;
	private float targetYRot = 0f;

	Card cardData;
	[Export] Sprite2D CardTexture;
	[Export] Sprite2D outlineSprite;
	[Export] AudioStreamPlayer2D HoverSound;

	public override void _Ready()
	{
		shaderMaterial = Material as ShaderMaterial;
		trueScale = Scale;
		truePos = Position;
		PivotOffset = Size / 2;

		Card[] deck = Functions.CreateDeck();
		setAsCard(deck[GD.Randi() % deck.Length]);
	}

	public void setAsCard(Card card)
	{
		cardData = card;
		CardTexture.Texture = card.texture;
		if(card.isred)
		{
			outlineSprite.Modulate = new Color("dd0900a8");
		}
		else
		{
			outlineSprite.Modulate = new Color("0042ffa8");
		}
	}

	public override void _Process(double delta)
	{
		if (shaderMaterial == null) return;

		float currentX = (float)shaderMaterial.GetShaderParameter("x_rot");
		float currentY = (float)shaderMaterial.GetShaderParameter("y_rot");

		float newX = Mathf.Lerp(currentX, targetXRot, (float)delta * 20.0f);
		float newY = Mathf.Lerp(currentY, targetYRot, (float)delta * 20.0f);

		shaderMaterial.SetShaderParameter("x_rot", newX);
		shaderMaterial.SetShaderParameter("y_rot", newY);
	}

	void on_gui_input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseEvent)
		{
			Vector2 mousePos = mouseEvent.Position;
			Vector2 diff = (Position + Size) - mousePos;
			double lerpValX = Mathf.Remap(mousePos.X, 0.0, Size.X, 0, 1);
			double lerpValY = Mathf.Remap(mousePos.Y, 0.0, Size.Y, 0, 1);
			double rotationX = Mathf.Lerp(10, -10, lerpValY);
			double rotationY = Mathf.Lerp(-10, 10, lerpValX);

			targetXRot = (float)rotationX;
			targetYRot = (float)rotationY;
		}
	}

	void on_mouse_enter()
	{
		scaleTween?.Kill();
		HoverSound.Play();

		// Control nodes determine input priority by tree order. 
		// Moving this node to the end of the parent's children list makes it "on top" for inputs.
		originalIndex = GetParent().GetIndex();
		GetParent().GetParent().MoveChild(GetParent(), -1);

		Vector2 targetScale = trueScale * 1.1f;
		scaleTween = GetTree().CreateTween();
		scaleTween.SetEase(Tween.EaseType.Out);
		scaleTween.SetTrans(Tween.TransitionType.Quad);
		scaleTween.TweenProperty(this, "scale", targetScale, 0.2);

		Vector2 targetPos = truePos + new Vector2(0, -200);
		posTween?.Kill();
		posTween = GetTree().CreateTween();
		posTween.SetEase(Tween.EaseType.Out);
		posTween.SetTrans(Tween.TransitionType.Quad);
		posTween.TweenProperty(this, "position", targetPos, 0.2);
	}

	void on_mouse_exit()
	{

		// Restore the original tree index so the card returns to its correct layout position
		// if you are using a Container (like HBoxContainer), otherwise it will stay at the front.
		GetParent().GetParent().MoveChild(GetParent(), originalIndex);

		// Reset targets to 0; _Process will handle the smooth transition back
		targetXRot = 0f;
		targetYRot = 0f;

		scaleTween?.Kill();

		scaleTween = GetTree().CreateTween();
		scaleTween.SetEase(Tween.EaseType.Out);
		scaleTween.SetTrans(Tween.TransitionType.Quad);
		scaleTween.TweenProperty(this, "scale", trueScale, 0.2);

		posTween?.Kill();
		posTween = GetTree().CreateTween();
		posTween.SetEase(Tween.EaseType.Out);
		posTween.SetTrans(Tween.TransitionType.Quad);
		posTween.TweenProperty(this, "position", truePos, 0.2);
	}
}
