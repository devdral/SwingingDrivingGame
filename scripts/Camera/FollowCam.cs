using System;
using Godot;
using SwingingDrivingGame.Utility;

namespace SwingingDrivingGame;

[GlobalClass]
public partial class FollowCam : Camera3D
{
    [Export] public Node3D? Following { get; set; }
    [Export] public float MovementTime { get; set; } = 0.125f;

    [ExportGroup("Settings")]
    [Export] public float FollowHeight { get; set; } = 30;
    [Export(PropertyHint.Range, "0,360")] public float FollowAngle { get; set; }
    [Export] public float FollowRadius { get; set; }
    [Export] public bool RemainLevel { get; set; } = false;

    [ExportSubgroup("Look Up")]
    [Export] public float LookUpTime { get; set; } = 0.25f;
    [Export] public double LookUpEaseCurve { get; set; } = -4;

    [ExportSubgroup("User-Rotate Camera")]
    [Export] public float MouseSensitivity { get; set; } = 30f;
    [Export] public float JoystickRotateSensitivity { get; set; } = 80f;

    private float _lookUpInterpTime;
    private Vector2 _lastMousePos = Vector2.Zero;
    private bool _isRotatingCamera = false;

    private bool _isMoving = false;
    private float _moveInterpTime;
    private Vector3 _oldPos;
    private Vector3 _newPos;
    private GodotObject? _lastCollided = null;

    public override void _Process(double delta)
    {
        if (Following is null)
            GD.PushError("Please set the Node3D to be followed.");
        float angle;
        if (Following.Rotation.Y > Mathf.DegToRad(180))
            angle = -FollowAngle;
        else
            angle = FollowAngle;
        var twoDOffset = Vector2.FromAngle(Mathf.DegToRad(angle)) * FollowRadius;
        var newPos = new Vector3(
            Following.Position.X + twoDOffset.X,
            Following.Position.Y + FollowHeight,
            Following.Position.Z + twoDOffset.Y
        );
        var query = PhysicsRayQueryParameters3D.Create(Following.Position, newPos);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count > 0)
        {;
            var collider = (GodotObject)result["collider"];
            var obstructedPos = (Vector3)result["position"];
            if (_lastCollided is null || _lastCollided != collider)
            {
                GD.Print("Smooth-transitioning to obstructed state.");
                MoveTo(obstructedPos);
            }
            else
            {
                Position = obstructedPos;
            }

            _lastCollided = collider;
        }
        else
        {
            if (_lastCollided is not null)
            {
                GD.Print("Smooth-transitioning to unobstructed state.");
                MoveTo(newPos);
                _lastCollided = null;
            }
            else if (!_isMoving)
            {
                Position = newPos;
                CancelMove();
            }
        }

        LookAt(Following.Position);
        var rotation = Rotation;
        if (RemainLevel)
        {
            rotation.X = 0;
        }
        else if (Input.IsActionPressed("look_up"))
        {
            if (_lookUpInterpTime < 1)
                _lookUpInterpTime += 1 / LookUpTime * (float)delta;
        }
        else
        {
            if (_lookUpInterpTime > 0)
                _lookUpInterpTime -= 1 / LookUpTime * (float)delta;
        }

        rotation.X = Mathf.Lerp(rotation.X, -rotation.X, (float)Mathf.Ease(_lookUpInterpTime, LookUpEaseCurve));
        Rotation = rotation;

        if (_isMoving)
        {
            Position = _oldPos.Lerp(_newPos, _moveInterpTime);
            if (_moveInterpTime < 1)
                _moveInterpTime += 1 / MovementTime * (float)delta;
            else
            {
                _isMoving = false;
                _moveInterpTime = 0;
            }
        }
        
        FollowAngle += Input.GetJoyAxis(0, JoyAxis.RightX) * JoystickRotateSensitivity * (float)GetProcessDeltaTime();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right } buttonEvent)
        {
            _isRotatingCamera = buttonEvent.Pressed;
        }
        else if (@event is InputEventMouseMotion motionEvent)
        {
            if (_isRotatingCamera)
            {
                var mouseDeltaPos = motionEvent.Relative;
                FollowAngle += mouseDeltaPos.X * MouseSensitivity * (float)GetProcessDeltaTime();
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }
    }

    private void MoveTo(Vector3 pos)
    {
        _newPos = pos;
        _oldPos = Position;
        _isMoving = true;
    }

    private void CancelMove()
    {
        _isMoving = false;
        _moveInterpTime = 0f;
    }

}
