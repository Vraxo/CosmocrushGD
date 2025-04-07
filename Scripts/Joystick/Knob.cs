using Godot;

public partial class Knob : Sprite2D
{
	[Export] public float MaxLength = 50f;
	[Export] private Button button;

	private Joystick parent;
	private bool pressing = false;
	private int touchIndex = -1; // -1 means no touch associated

	public override void _Ready()
	{
		parent = GetParent<Joystick>();
		// Remove Button signal connections as we handle touch directly
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventScreenTouch eventScreenTouch)
		{
			if (eventScreenTouch.Pressed)
			{
				// Check if touch started within the button bounds and if this knob isn't already controlled
				if (touchIndex == -1 && button.GetGlobalRect().HasPoint(eventScreenTouch.Position))
				{
					touchIndex = eventScreenTouch.Index;
					pressing = true;
					// Optional: Immediately move knob to touch position within limits
					UpdateKnobPosition(eventScreenTouch.Position);
				}
			}
			else
			{
				// Check if the touch that ended is the one controlling this knob
				if (eventScreenTouch.Index == touchIndex)
				{
					touchIndex = -1;
					pressing = false;
				}
			}
		}
		else if (@event is InputEventScreenDrag eventScreenDrag)
		{
			// Check if the drag event is from the touch controlling this knob
			if (eventScreenDrag.Index == touchIndex)
			{
				UpdateKnobPosition(eventScreenDrag.Position);
			}
		}
	}


	public override void _Process(double delta)
	{
		// Lerp back to center if not being pressed
		if (!pressing)
		{
			GlobalPosition = GlobalPosition.Lerp(parent.GlobalPosition, (float)(delta * 10.0f)); // Adjusted lerp speed
			parent.PosVector = Vector2.Zero; // Ensure vector is zero when released
		}
		else
		{
			// Ensure vector is calculated even if no drag event happened this frame
			CalculateVector();
		}


		// Input simulation remains the same
		if (parent.SimulateInput)
		{
			UpdateInputActions();
		}
	}

	private void UpdateKnobPosition(Vector2 screenPosition)
	{
		float distance = screenPosition.DistanceTo(parent.GlobalPosition);

		GlobalPosition = distance <= MaxLength
			? screenPosition
			: parent.GlobalPosition + parent.GlobalPosition.DirectionTo(screenPosition) * MaxLength;

		CalculateVector();
	}


	private void CalculateVector()
	{
		parent.PosVector = (GlobalPosition - parent.GlobalPosition) / MaxLength;
	}

	private void UpdateInputActions()
	{
		Vector2 dir = parent.Direction;
		Input.ActionPress("left", Mathf.Max(-dir.X, 0f));
		Input.ActionPress("right", Mathf.Max(dir.X, 0f));
		Input.ActionPress("up", Mathf.Max(-dir.Y, 0f));
		Input.ActionPress("down", Mathf.Max(dir.Y, 0f));
	}

	// Remove OnButtonPressed and OnButtonReleased as they are replaced by _Input handling
	// private void OnButtonPressed() { ... }
	// private void OnButtonReleased() { ... }
}
