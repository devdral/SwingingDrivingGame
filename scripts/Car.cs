using Godot;
using System;
using SwingingDrivingGame.Utility;

namespace SwingingDrivingGame;

public partial class Car : CharacterBody3D
{
    [Signal] public delegate void RopeAvailableEventHandler();
    [Signal] public delegate void RopeUnavailableEventHandler();
    
    [ExportGroup("Movement")]
    [Export] public float Gravity { get; set; } = 750;
    [Export] public float Speed { get; set; } = 1000;
    [Export] public float TurnSpeed { get; set; } = 0.75f;
    [Export] public float MaxYVelocity { get; set; } = 1250;
    [Export] public float MaxSpeed { get; set; } = 1500;
    [Export] public float Deceleration { get; set; } = 250;
    
    [ExportGroup("Rope Settings")]
    [Export] public float MaxRopeDistance { get; set; } = 50;
    [Export] public float RopeRadius { get; set; } = 0.125f;
    [Export] public PackedScene RopePlacementIndicatorScene { get; set; }
    
    [ExportGroup("Destruction Settings")]
    [Export] public float DestructionVelocity { get; set; } = 60;

    private float _wheelRotation;
    private float _currentSpeed;
    private bool _shouldOverrideNextMovement;
    private Vector3 _movementOverride;
    private Vector3 _spawnPoint;
    private bool _ropeAvailable;
    
    private RopeManager _ropeManager;
    private ShapeCast3D _nearestSurfaceFinder;
    private Node3D _ropePlacementIndicator;

    public float CurrentSpeed => _currentSpeed;

    public override void _Ready()
    {
        _spawnPoint = Position;
        _ropeManager = GetNode<RopeManager>("RopeManager");
        _ropeManager.MaxDist = MaxRopeDistance;
        _ropeManager.RopeRadius = RopeRadius;
        _ropePlacementIndicator = (Node3D)RopePlacementIndicatorScene.Instantiate();
        _ropePlacementIndicator.Hide();
        GetTree().CurrentScene.CallDeferred("add_child", _ropePlacementIndicator);
        
        _nearestSurfaceFinder = GetNode<ShapeCast3D>("NearestSurfaceFinder");
        ((SphereShape3D)_nearestSurfaceFinder.Shape).Radius = MaxRopeDistance;
    }

    public override void _PhysicsProcess(double delta)
    {

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

        if (IsOnFloor() || _ropeManager.IsUsingRope)
        {
            if (Input.IsActionPressed("forward"))
            {
                _currentSpeed += Speed * (float)delta;
            }
            else if (Input.IsActionPressed("back"))
            {
                _currentSpeed -= Speed * (float)delta;
            }
            else
            {
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
            }
        }
        else
        {
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
        }

        // Limit vector length on the XZ plane to limit "speed" on that plane
        if (MathF.Abs(_currentSpeed) > MaxSpeed)
        {
            _currentSpeed = MaxSpeed;
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
        var prevVel = Velocity;
        MoveAndSlide();
        Vector3 correctedPoint;
        if (GetSlideCollisionCount() > 0)
        {
            for (var i = 0; i < GetSlideCollisionCount(); i++)
            {
                var collision = GetSlideCollision(i);
                var @object = (Node)collision.GetCollider();
                if (@object.GetParent() is DestructibleBody3D building && !@object.Name.ToString().StartsWith("VFragment"))
                {
                    building.ProcessCollisionWithCar(this, prevVel);
                    Velocity = prevVel;
                }
                else if (@object is RigidBody3D body)
                {
                    body.ApplyForce(prevVel);
                }
            }
        }
        if (_ropeManager.IsUsingRope)
        {
            if (GetSlideCollisionCount() > 0)
            {
                _ropeManager.DisableRope();
            }
            else if (_ropeManager.IsPointTooFar(Position, out correctedPoint))
            {
                var force = correctedPoint - Position;
                Position = prevPos;
                DebugDraw3D.DrawPoints([correctedPoint], color: Colors.BlueViolet);
                Velocity += force;
                LookAt(Position + Velocity.Normalized());
                MoveAndSlide();
            }
        }
        
        if (Position != prevPos)
        {
            _ropeManager.UpdateRope();
        }
        _currentSpeed = _currentSpeed > 0 ? new Vector2(Velocity.X, Velocity.Z).Length() : -(new Vector2(Velocity.X, Velocity.Z).Length());
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

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("respawn"))
        {
            Position = _spawnPoint;
        }
        _nearestSurfaceFinder.ForceShapecastUpdate();
        if (_nearestSurfaceFinder.GetCollisionCount() > 0)
        {
            EmitSignalRopeAvailable();
            _ropeAvailable = true;
            if (!_ropeManager.IsUsingRope)
            {
                _ropePlacementIndicator.Position = _nearestSurfaceFinder.GetCollisionPoint(0);
                _ropePlacementIndicator.Show();
            }
        }
        else
        {
            EmitSignalRopeUnavailable();
            _ropeAvailable = false;
            _ropePlacementIndicator.Hide();
        }

        // Basis.Z is the direction the Transform3D is facing.
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_rope"))
        {
            if (!_ropeManager.IsUsingRope)
            {
                if (_ropeAvailable)
                {
                    var point = _nearestSurfaceFinder.GetCollisionPoint(0);
                    _ropeManager.EnableRope(point);
                    _ropePlacementIndicator.Hide();
                }
            }
            else
            {
                _ropeManager.DisableRope();
            }
        }
    }
}
