using System;
using Godot;
using SwingingDrivingGame.Utility;

namespace SwingingDrivingGame;

public partial class FollowCam : Camera3D
{
    [Export] public Node3D? Following { get; set; }
    
    [ExportGroup("Settings")]
    [Export] public float FollowHeight { get; set; } = 30;
    [Export(PropertyHint.Range, "0,360")] public float FollowAngle { get; set; }
    [Export] public float FollowRadius { get; set; }

    public override void _Process(double delta)
    {
        if (Following is null)
            throw new NullReferenceException("Please set the Node3D to be followed.");
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
        {
            Position = (Vector3)result["position"];
        }
        else
        {
            Position = newPos;
        }
        LookAt(Following.Position);
        var rotation = Rotation;
        rotation.X = 0;
        Rotation = rotation;
    }
}
