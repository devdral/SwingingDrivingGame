using Godot;
using System;

namespace SwingingDrivingGame;

public partial class Car : CharacterBody3D
{
    [ExportGroup("Movement")]
    [Export] public float Gravity { get; set; } = 750;
    [Export] public float Speed { get; set; } = 1000;
    [Export(PropertyHint.Range, "0,1")] public float TurnSpeed { get; set; } = 0.75f;
    [Export] public float MaxYVelocity { get; set; } = 1250;
    [Export] public float MaxSpeed { get; set; } = 1500;
    [Export] public float Deceleration { get; set; } = 250;
    
    [ExportGroup("Rope Settings")]
    [Export] public float MaxRopeDistance { get; set; } = 50;
    [Export] public float RopeRadius { get; set; } = 0.125f;
    // [Export] public float RopeVerticalImpulse { get; set; } = 10;

    private float _wheelRotation;
    private float _currentSpeed;
    private bool _shouldOverrideNextMovement;
    private Vector3 _movementOverride;
    
    private RopeManager _ropeManager;
    private ShapeCast3D _nearestSurfaceFinder;
    
    public float CurrentSpeed => _currentSpeed;

    public override void _Ready()
    {
        _ropeManager = GetNode<RopeManager>("RopeManager");
        _ropeManager.MaxDist = MaxRopeDistance;
        _ropeManager.RopeRadius = RopeRadius;
        
        _nearestSurfaceFinder = GetNode<ShapeCast3D>("NearestSurfaceFinder");
        ((SphereShape3D)_nearestSurfaceFinder.Shape).Radius = MaxRopeDistance;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("toggle_rope"))
        {
            if (!_ropeManager.IsUsingRope)
            {
                _nearestSurfaceFinder.ForceShapecastUpdate();
                if (_nearestSurfaceFinder.GetCollisionCount() > 0)
                {
                    var point = _nearestSurfaceFinder.GetCollisionPoint(0);
                    _ropeManager.EnableRope(point);
                }
            }
            else
            {
                _ropeManager.DisableRope();
            }
        }

        Vector3 rot = Rotation;

        var newVel = new Vector3
        {
            Y = Velocity.Y
        };
        if (Math.Abs(_wheelRotation) > 0)
        {
            rot = Rotation with { Y = Rotation.Y + _wheelRotation };
            Rotation = rot;
            _wheelRotation = 0;
        }

        if (!_ropeManager.IsUsingRope)
        {
            newVel.Y -= Gravity * (float)delta;
            newVel.Y = float.Clamp(newVel.Y, -MaxYVelocity, MaxYVelocity);
        }   

        if (Input.IsActionPressed("forward"))
        {
            _currentSpeed += Speed * (float)delta;
        }
        else if (Input.IsActionPressed("back"))
        {
            _currentSpeed -= Speed * (float)delta;
        }

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

        Vector2 baseVector = Vector2.Up.Rotated(-rot.Y);
        var twoDVelocity = baseVector * _currentSpeed;

        newVel.X = twoDVelocity.X;
        newVel.Z = twoDVelocity.Y;

        if (Input.IsActionPressed("left"))
        {
            _wheelRotation += TurnSpeed * (float)delta;
        }
        else if (Input.IsActionPressed("right"))
        {
            _wheelRotation -= TurnSpeed * (float)delta;
        }

        if (Math.Abs(_currentSpeed) <= 0)
        {
            _wheelRotation = 0;
        }

        if (_shouldOverrideNextMovement)
        {
            Velocity = _movementOverride;
            _currentSpeed = 0;
            _shouldOverrideNextMovement = false;
        }
        else
        {
            Velocity = newVel;
        }
        
        var prevPos = Position;
        MoveAndSlide();
        Vector3 correctedPoint;
        if (_ropeManager.IsUsingRope && _ropeManager.IsPointTooFar(Position, out correctedPoint))
        {
            Position = prevPos;
            // DebugDraw3D.DrawSphere(correctedPoint, 5, Colors.BlueViolet);
            if (correctedPoint != Position)
            {
                LookAt(correctedPoint);
                // Kind of inefficient and janky
                // Maybe consider consolidating this down to one MoveAndSlide call
                var rotQuat = Quaternion.FromEuler(Rotation);
                Velocity = Velocity.Rotated(rotQuat.GetAxis().Normalized(), rotQuat.GetAngle());
                MoveAndSlide();
                // if (GetSlideCollisionCount() > 0)
                // {
                //     _ropeManager.DisableRope();
                // }
            }
        }
        
        if (Position != prevPos)
        {
            _ropeManager.UpdateRope();
        }
        _currentSpeed = _currentSpeed > 0 ? new Vector2(Velocity.X, Velocity.Z).Length() : -new Vector2(Velocity.X, Velocity.Z).Length();
        if (IsOnFloor())
        {
            Rotation = Rotation with { X = 0, Z = 0 };
        }
    }

    public void SetCurrentSpeed(float newSpeed)
    {
        _currentSpeed = newSpeed;
    }

    public void OverrideVelocity(Vector3 newVelocity)
    {
        _movementOverride = newVelocity;
        _shouldOverrideNextMovement = true;
    }
}
