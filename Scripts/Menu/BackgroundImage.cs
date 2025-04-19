using System;
using Godot;

public partial class BackgroundImage : TextureRect
{
    private readonly string pathTemplate = "res://Sprites/MenuBackgrounds/MenuBackground{0}.png";
    private float oscillationSpeed = 1.0f;
    private float amplitude = 20.0f;

    public override void _Ready()
    {
        Random random = new();
        string path = string.Format(pathTemplate, random.Next(1, 7)); // Generates a number in the range [1, 6]
        Texture = (Texture2D)ResourceLoader.Load(path);
    }

    public override void _Process(double delta)
    {
        UpdatePosition();
        UpdateSize();
        Move();
    }

    private void UpdatePosition()
    {
        Position = GetViewportRect().Size / 2 - Size / 2; // Center the texture on the screen
    }

    private void UpdateSize()
    {
        Size = GetViewportRect().Size * 1.5f; // Size to 1.5 times the screen size
    }

    private void Move()
    {
        Vector2 windowCenter = GetViewportRect().Size / 2 - Size / 2;

        float offsetX = amplitude * Mathf.Sin(Time.GetTicksMsec() * oscillationSpeed / 1000.0f);
        float offsetY = amplitude * Mathf.Cos(Time.GetTicksMsec() * oscillationSpeed * 0.8f / 1000.0f); // Slightly slower Y movement

        //// Update the position to stay centered with oscillation
        Position = windowCenter + new Vector2(offsetX, offsetY);
    }
}