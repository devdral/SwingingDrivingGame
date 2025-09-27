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

    public override void _PhysicsProcess(double delta)
    {
        var newVel = Velocity;

        newVel.Y -= Gravity * (float)delta;
        newVel.Y = float.Clamp(newVel.Y, -MaxYVelocity, MaxYVelocity);
        
        if (Input.IsActionPressed("forward"))
        {
            Quaternion rotQuat = Quaternion.FromEuler(Rotation);
            Vector3 baseVector;
            if (Rotation == Vector3.Zero)
            {
                baseVector = Vector3.Forward;
            }
            else
            {
                baseVector = Vector3.Forward.Rotated(rotQuat.GetAxis().Normalized(), rotQuat.GetAngle());
            }
            newVel += baseVector * Speed * (float)delta;
        }
        else if (Input.IsActionPressed("back"))
        {
            Quaternion rotQuat = Quaternion.FromEuler(Rotation);
            Vector3 baseVector;
            if (Rotation == Vector3.Zero)
            {
                baseVector = Vector3.Forward;
            }
            else
            {
                baseVector = Vector3.Forward.Rotated(rotQuat.GetAxis().Normalized(), rotQuat.GetAngle());
            }
            newVel -= baseVector * Speed * (float)delta;
        }
        
        // Limit vector length on the XZ plane to limit "speed" on that plane
        var twoDVelocity = new Vector2(newVel.X, newVel.Z);
        float currentSpeed = twoDVelocity.Length();
        if (MathF.Abs(currentSpeed) > MaxSpeed)
        {
            twoDVelocity = twoDVelocity.Normalized() * MaxSpeed;
            currentSpeed = MaxSpeed;
        }

        if (currentSpeed > 0)
        {
            currentSpeed -= Deceleration * (float)delta;
        }
        else if (currentSpeed < 0)
        {
            currentSpeed += Deceleration * (float)delta;
        }
        
        twoDVelocity = twoDVelocity.Normalized() * currentSpeed;
        newVel.X = twoDVelocity.X;
        newVel.Z = twoDVelocity.Y;
        

        var wheelRotation = 0f;
        if (Input.IsActionPressed("left"))
        {
            wheelRotation -= TurnSpeed * 90;
        }
        else if (Input.IsActionPressed("right"))
        {
            wheelRotation += TurnSpeed * 90;
        }

        if (wheelRotation > 0f && currentSpeed > 0f)
        {
            Rotation = Rotation with { Y = Rotation.Y + wheelRotation };
        }
        Velocity = newVel;
        MoveAndSlide();
    }
}
