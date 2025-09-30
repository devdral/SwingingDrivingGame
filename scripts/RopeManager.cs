using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Array = Godot.Collections.Array;

public partial class RopeManager : Node
{
    public const int RopeBoundryLayer = 3;
    
    public float MaxDist { get; set; }
    public float RopeRadius { get; set; }
    
    private Node3D _ropeSegmentAttachPoint;
    private CharacterBody3D _characterBody;
    
    private List<RopeSegment> _ropeSegments = [];
    private bool _isUsingRope;
    private StaticBody3D _lastSegmentBoundryCollider;
    private float _totalLength;

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
        _characterBody = GetParent<CharacterBody3D>();
    }

    public void EnableRope(Vector3 startPos)
    {
        if (_isUsingRope)
            return;
        var distance = startPos.DistanceTo(_characterBody.Position);
        if (distance > MaxDist)
            return;
        var @object = new Node3D();
        var mesh = new CylinderMesh();
        mesh.Height = distance;
        mesh.TopRadius = RopeRadius;
        mesh.BottomRadius = RopeRadius;
        var mi = new MeshInstance3D();
        mi.Mesh = mesh;
        mi.RotationDegrees = new Vector3(90, 0, 0);
        @object.AddChild(mi);
        @object.Position = startPos;
        _ropeSegmentAttachPoint.AddChild(@object);
        if (startPos != _characterBody.Position)
        {
            @object.LookAt(_characterBody.Position, Vector3.Forward);
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
        // _lastSegmentBoundryCollider.QueueFree();
        // _lastSegmentBoundryCollider = null;
    }

    public void UpdateRope()
    {
        if (!_isUsingRope) return;

        var prevPoint = _ropeSegments.Last().StartPoint;
        var query = PhysicsRayQueryParameters3D.Create(_characterBody.Position, prevPoint);
        var newExclude = query.Exclude;
        newExclude.Add(_characterBody.GetRid());
        query.Exclude = newExclude;
        var result = _characterBody.GetWorld3D().DirectSpaceState.IntersectRay(query);
        
        var distance = prevPoint.DistanceTo(_characterBody.Position);
        if (!(result.Count <= 0))
        {
            GD.Print($"Rope collided with {((Node)result["collider"]).Name}. Creating new segment.");
            
            var @object = new Node3D();
            var mesh = new CylinderMesh();
            mesh.Height = distance;
            mesh.TopRadius = RopeRadius;
            mesh.BottomRadius = RopeRadius;
            var mi = new MeshInstance3D();
            mi.Mesh = mesh;
            mi.RotationDegrees = new Vector3(90, 0, 0);
            @object.AddChild(mi);
            @object.Position = prevPoint;
            // Godot's raycast API for SOME reason uses a Dictionary--yes a DICTIONARY--for the result
            // of a raycast. Not only that, it has a value type of Godot.Variant, which must be CAST to
            // the desired type. So weird.
            var newPos = (Vector3)result["position"];
            _ropeSegments.Add(new RopeSegment(@object, newPos, distance));
            _ropeSegmentAttachPoint.AddChild(@object);
            
            UpdateSeg(_ropeSegments.Last(), newPos);
            
            if (prevPoint != _characterBody.Position)
            {
                @object.LookAt(_characterBody.Position, Vector3.Forward);
            }

            // if (_lastSegmentBoundryCollider is not null)
            // {
            //     _lastSegmentBoundryCollider.QueueFree();
            //     _lastSegmentBoundryCollider = null;
            // }
        }
        else
        {
            var lastSeg = _ropeSegments.Last();
            UpdateSeg(lastSeg, _characterBody.Position);
            // var collisionObject = new StaticBody3D();
            // var collider = new CollisionShape3D();
            // var shape = new SphereShape3D();
            // // All the remaining space, plus space occupied by the current segment
            // shape.Radius = MaxDist - totalLength + lastSeg.Length;
            // collider.Shape = shape;
            // collisionObject.AddChild(collider);
            // collisionObject.SetCollisionLayerValue(1, false);
            // collisionObject.SetCollisionLayerValue(RopeBoundryLayer, true);
            // collisionObject.Position = lastSeg.Object.Position;
            // _lastSegmentBoundryCollider = collisionObject;
            // _ropeSegmentAttachPoint.AddChild(collisionObject);
        }
    }

    private void UpdateSeg(RopeSegment segment, Vector3 posAt)
    {
        var distance = segment.StartPoint.DistanceTo(posAt);
        var @object = segment.Object;
        if (segment.StartPoint != posAt)
        {
            @object.LookAt(posAt, Vector3.Forward);
        }

        var mi = @object.GetChild<MeshInstance3D>(0);
        CylinderMesh mesh = (CylinderMesh)mi.Mesh;
        mesh.Height = distance;
        mi.Position = mi.Position with { Z = -distance / 2 };
        segment.Length = distance;
        _totalLength = 0f;
        foreach (var seg in _ropeSegments)
        {
            _totalLength += seg.Length;
        }
    }

    public override void _Process(double delta)
    {
        if (_isUsingRope)
        {
            var prevPoint = _ropeSegments.Last().StartPoint;
            var distance = prevPoint.DistanceTo(_characterBody.Position);
            var lastSeg = _ropeSegments.Last();
            if (distance > MaxDist - _totalLength + lastSeg.Length)
            {
                // GD.Print("Get back inside!!");
                var vector = _characterBody.Position.DirectionTo(lastSeg.StartPoint);
                vector = new Vector3(
                    vector.X*vector.X,
                    vector.Y*vector.Y,
                    vector.Z*vector.Z)
                    / distance;
                _characterBody.Velocity = vector;
                _characterBody.MoveAndSlide();
            }   
        }
    }
}

public record RopeSegment(Node3D Object, Vector3 StartPoint, float Length)
{
    public float Length { get; set; } = Length;
}