using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SwingingDrivingGame;
using Array = Godot.Collections.Array;
using Range = System.Range;

public partial class RopeManager : Node
{
    public const int RopeBoundryLayer = 3;
    
    public float MaxDist { get; set; }
    public float RopeRadius { get; set; }
    
    private Node3D _ropeSegmentAttachPoint;
    private Car _car;
    
    private List<RopeSegment> _ropeSegments = [];
    private bool _isUsingRope;
    private StaticBody3D _lastSegmentBoundryCollider;
    private float _totalLength;
    private float _targetLength;

    public bool IsUsingRope => _isUsingRope;

    public override void _Ready()
    {
        try
        {
            _ropeSegmentAttachPoint = (Node3D)GetTree().CurrentScene;
        }
        catch (InvalidCastException)
        {
            GD.PushError("A Car may only be placed in a 3D scene (scene root must be of type Node3D).");
        }
        _car = GetParent<Car>();
    }

    public void EnableRope(Vector3 startPos)
    {
        if (_isUsingRope)
            return;
        var distance = startPos.DistanceTo(_car.Position);
        if (distance > MaxDist)
            return;
        _targetLength = distance;
        var @object = new Node3D();
        var mesh = new CylinderMesh();
        mesh.Height = distance;
        mesh.TopRadius = RopeRadius;
        mesh.BottomRadius = RopeRadius;
        var mi = new MeshInstance3D();
        mi.Mesh = mesh;
        mi.RotationDegrees = new Vector3(90, 0, 0);
        mi.Position = mi.Position with { Z = -distance / 2 };
        @object.AddChild(mi);
        @object.Position = startPos;
        _ropeSegmentAttachPoint.AddChild(@object);
        if (startPos != _car.Position)
        {
            @object.LookAt(_car.Position, Vector3.Forward);
        }
        _ropeSegments.Add(new RopeSegment(@object, startPos, distance));
        _isUsingRope = true;
    }

    public void DisableRope()
    {
        if (!_isUsingRope)
            return;
        _isUsingRope = false;
        foreach (var seg in _ropeSegments)
        {
            seg.Object.QueueFree();
        }
        _ropeSegments.Clear();
    }

    public void UpdateRope()
    {
        if (!_isUsingRope) return;

        var lastSeg = _ropeSegments.Last();
        var prevPoint = lastSeg.StartPoint;
        var query = PhysicsRayQueryParameters3D.Create(_car.Position, prevPoint);
        var newExclude = query.Exclude;
        newExclude.Add(_car.GetRid());
        query.Exclude = newExclude;
        var result = _car.GetWorld3D().DirectSpaceState.IntersectRay(query);
        
        var distance = prevPoint.DistanceTo(_car.Position);
        if (result.Count > 0)
        {
            GD.Print($"Rope collided with {((Node)result["collider"]).Name}. Creating new segment.");
            
            var @object = new Node3D();
            var mesh = new CylinderMesh();
            mesh.Height = distance;
            mesh.TopRadius = RopeRadius;
            mesh.BottomRadius = RopeRadius;
            var mi = new MeshInstance3D();
            mi.Mesh = mesh;
            mi.Position = mi.Position with { Z = -distance / 2 };
            mi.RotationDegrees = new Vector3(90, 0, 0);
            @object.AddChild(mi);
            @object.Position = prevPoint;
            // Godot's raycast API for SOME reason uses a Dictionary--yes a DICTIONARY--for the result
            // of a raycast. Not only that, it has a value type of Godot.Variant, which must be CAST to
            // the desired type. So weird.
            var newPos = (Vector3)result["position"];
            _ropeSegments.Add(new RopeSegment(@object, newPos, distance));
            _ropeSegmentAttachPoint.AddChild(@object);
            
            UpdateSeg(lastSeg, newPos);
            
            if (prevPoint != _car.Position)
            {
                @object.LookAt(_car.Position, Vector3.Forward);
            }

            // if (_lastSegmentBoundryCollider is not null)
            // {
            //     _lastSegmentBoundryCollider.QueueFree();
            //     _lastSegmentBoundryCollider = null;
            // }
        }
        else
        {
            UpdateSeg(lastSeg, _car.Position);
        }
    }

    private void UpdateSeg(RopeSegment segment, Vector3 posAt)
    {
        var distance = segment.StartPoint.DistanceTo(posAt);
        var @object = segment.Object;
        if (@object.Position != posAt)
        {
            @object.LookAt(posAt, Vector3.Forward);
        }

        var mi = @object.GetChild<MeshInstance3D>(0);
        CylinderMesh mesh = (CylinderMesh)mi.Mesh;
        mesh.Height = distance;
        mi.Position = mi.Position with { Z = -distance / 2 };
        segment.Length = distance;
        _totalLength = 0f;
        var i = 0;
        var mustUpdateAll = false;
        foreach (var seg in _ropeSegments)
        {
            // var query = PhysicsRayQueryParameters3D.Create(_car.Position, segment.StartPoint);
            // var result = _car.GetWorld3D().DirectSpaceState.IntersectRay(query);
            // if (result.Count <= 0)
            // {
            //     mustUpdateAll = true;
            //     break;
            // }
            _totalLength += seg.Length;
            i++;
        }

        // if (mustUpdateAll)
        // {
        //     foreach (var seg in _ropeSegments[new Range(i, _ropeSegments.Count - 1)])
        //     {
        //         // Make sure to remove it from the SceneTree.
        //         seg.Object.QueueFree();
        //     }
        //     // _ropeSegments = _ropeSegments[new Range(0, i)];
        //     for (var index = 0; index < _ropeSegments.Count; index++)
        //     {
        //         var seg = _ropeSegments[index];
        //         Vector3 target;
        //         if (index > 0)
        //         {
        //             target = _ropeSegments[index + 1].StartPoint;
        //         }
        //         else
        //         {
        //             target = _car.Position;
        //         }
        //         UpdateSeg(seg, target);
        //     }
        // }
    }

    public bool IsPointTooFar(Vector3 point, out Vector3 corrected)
    {
        if (!_isUsingRope)
            throw new InvalidOperationException("There is no rope yet.");
        var prevPoint = _ropeSegments.Last().StartPoint;
        var distance = prevPoint.DistanceTo(point);
        var lastSeg = _ropeSegments.Last();
        corrected = point;
        var targetDist = _targetLength - _totalLength + lastSeg.Length;
        if (distance > targetDist)
        {
            var directionFromCenter = prevPoint.DirectionTo(point);
            corrected = prevPoint + directionFromCenter * targetDist;
            return true;
        }
        return false;
    }
}

public record RopeSegment(Node3D Object, Vector3 StartPoint, float Length)
{
    public float Length { get; set; } = Length;
}