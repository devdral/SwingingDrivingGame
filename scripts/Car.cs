using Godot;
using System;

namespace SwingingDrivingGame;

public partial class Car : CharacterBody3D
{
    [Export] public float Gravity { get; set; } = 750;
    [Export] public float Speed { get; set; } = 1000;
    [Export(PropertyHint.Range, "0,1")] public float TurnSpeed { get; set; } = 0.75f;
    [Export] public float MaxYVelocity { get; set; } = 1250;
    [Export] public float MaxSpeed { get; set; } = 1500;
    [Export] public float Deceleration { get; set; } = 250;

    private float _wheelRotation;
    private float _currentSpeed;
    
    public float CurrentSpeed => _currentSpeed;

    public override void _PhysicsProcess(double delta)
    {
        var newVel = new Vector3
        {
            Y = Velocity.Y
        };
        if (Math.Abs(_wheelRotation) > 0)
        {
            Rotation = Rotation with { Y = Rotation.Y + _wheelRotation };
            _wheelRotation = 0;
        }

        newVel.Y -= Gravity * (float)delta;
        newVel.Y = float.Clamp(newVel.Y, -MaxYVelocity, MaxYVelocity);
        
        if (Input.IsActionPressed("forward"))
        {
            _currentSpeed += Speed * (float)delta;
            GD.Print("FD");
        }
        else if (Input.IsActionPressed("back"))
        {
            _currentSpeed -= Speed * (float)delta;
            GD.Print("BK");
        }
        
        var twoDVelocity = new Vector2(newVel.X, newVel.Y);
        
        // Limit vector length on the XZ plane to limit "speed" on that plane
        if (MathF.Abs(_currentSpeed) > MaxSpeed)
        {
            _currentSpeed = MaxSpeed;
        }

        if (_currentSpeed > 0)
        {
            if (_currentSpeed < Deceleration * (float)delta)
            {
                _currentSpeed = 0;
            }
            _currentSpeed -= Deceleration * (float)delta;
        }
        else if (_currentSpeed < 0)
        {
            if (_currentSpeed > -(Deceleration * (float)delta))
            {
                _currentSpeed = 0;
            }
            _currentSpeed += Deceleration * (float)delta;
        }
        
        Vector2 baseVector = Vector2.Up.Rotated(-Rotation.Y);
        twoDVelocity = baseVector * _currentSpeed;
        
        newVel.X = twoDVelocity.X;
        newVel.Z = twoDVelocity.Y;
        
        if (Input.IsActionPressed("left"))
        {
            _wheelRotation += TurnSpeed * (float)delta;
            GD.Print("RL");
        }
        else if (Input.IsActionPressed("right"))
        {
            _wheelRotation -= TurnSpeed * (float)delta;
            GD.Print("RR");
        }

        if (Math.Abs(_currentSpeed) <= 0)
        {
            _wheelRotation = 0;
        }
        
        Velocity = newVel;
        MoveAndSlide();
    }
}
