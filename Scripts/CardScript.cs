using Godot;
using System;

public partial class CardScript : SubViewportContainer
{
	private Tween scaleTween;
	ShaderMaterial shaderMaterial;
	Vector2 trueScale;
	private float targetXRot = 0f;
	private float targetYRot = 0f;

	public override void _Ready()
	{
		shaderMaterial = Material as ShaderMaterial;
		trueScale = Scale;
		PivotOffset = Size / 2;
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
			GD.Print("Mouse event on card: " + mouseEvent.Position);
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

		Vector2 targetScale = trueScale * 1.2f;
		scaleTween = GetTree().CreateTween();
		scaleTween.SetEase(Tween.EaseType.Out);
		scaleTween.SetTrans(Tween.TransitionType.Bounce);
		scaleTween.TweenProperty(this, "scale", targetScale, 0.3);
	}

	void on_mouse_exit()
	{
		// Reset targets to 0; _Process will handle the smooth transition back
		targetXRot = 0f;
		targetYRot = 0f;

		scaleTween?.Kill();

		scaleTween = GetTree().CreateTween();
		scaleTween.SetEase(Tween.EaseType.Out);
		scaleTween.SetTrans(Tween.TransitionType.Bounce);
		scaleTween.TweenProperty(this, "scale", trueScale, 0.3);
	}
}
