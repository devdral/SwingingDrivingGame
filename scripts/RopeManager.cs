using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RopeManager : Node
{
    public const int RopeBoundryLayer = 3;
    public const float RopeSegmentLength = 0.125f; // 125mm
    
    public float MaxDist { get; set; }
    public float RopeRadius { get; set; }
    
    private Node3D _ropeSegmentAttachPoint;
    private CharacterBody3D _characterBody;
    
    private bool _isUsingRope;
    private StaticBody3D _lastSegmentBoundryCollider;
    private float _totalLength;
    private List<RigidBody3D> _ropeSegments = [];
    private List<PinJoint3D> _ropeJoints = [];
    private RigidBody3D _ropeEndpoint;

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
        var targetDistance = startPos.DistanceTo(_characterBody.Position);
        if (targetDistance > MaxDist)
            return;
        _isUsingRope = true;
        var distance = 0f;
        var position = startPos;
        var endPos = _characterBody.GlobalPosition;
        var direction = startPos.DirectionTo(endPos);
        RigidBody3D lastObj = null;
        
        while (distance < targetDistance)
        {
            var @object = new RigidBody3D();
            
            var collider = new CollisionShape3D();
            var shape = new SphereShape3D();
            shape.Radius = RopeSegmentLength/2;
            collider.Shape = shape;
            @object.AddChild(collider);
            
            var mi = new MeshInstance3D();
            var mesh = new SphereMesh();
            mesh.Radius = RopeSegmentLength/2;
            mi.Mesh = mesh;
            mi.Scale = Vector3.One;
            
            @object.RotationDegrees = new Vector3(90, 0, 0);
            @object.AddChild(mi);
            @object.Position = position;
            @object.SetCollisionLayerValue(1,true);
            _ropeSegmentAttachPoint.AddChild(@object);
            _ropeSegments.Add(@object);
            
            if (lastObj is not null)
            {
                var joint = new PinJoint3D();
                // joint.SetParam(PinJoint3D.Param.Damping, 1000);
                // joint.SetParam(PinJoint3D.Param.ImpulseClamp, 1);
                joint.Position = @object.Position;
                joint.NodeA = lastObj.GetPath();
                joint.NodeB = @object.GetPath();
                _ropeJoints.Add(joint);
                _ropeSegmentAttachPoint.AddChild(joint);
            }
            else
            {
                var joint = new PinJoint3D();
                joint.Position = startPos;
                joint.NodeB = @object.GetPath();
                _ropeSegmentAttachPoint.AddChild(joint);
            }
            lastObj = @object;
            
            position += direction * RopeSegmentLength;
            distance += RopeSegmentLength;
        }

        if (lastObj is not null)
        {
            var finalJoint = new PinJoint3D();
            finalJoint.Position = _characterBody.Position;
            finalJoint.NodeA = lastObj.GetPath();
            
            var body = new RigidBody3D();
            body.Position = position;
            
            var collider = new CollisionShape3D();
            var shape = _characterBody.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
            collider.Shape = shape;
            body.AddChild(collider);
            body.Mass = 10;
            
            _ropeSegmentAttachPoint.AddChild(body);
            finalJoint.NodeB = body.GetPath();
            _ropeSegmentAttachPoint.AddChild(finalJoint);
            _ropeJoints.Add(finalJoint);
            _ropeEndpoint = body;
            body.LinearVelocity = _characterBody.Velocity;
        }

    }

    public Vector3 GetRopeEndpoint()
    {
        if (!_isUsingRope)
            throw new InvalidOperationException("Cannot get rope endpoint when there is no rope.");
        return _ropeEndpoint.Position;
    }
    
    public void DisableRope()
    {
        if (!_isUsingRope)
            return;
        _isUsingRope = false;
        foreach (var ropeSegment in _ropeSegments)
        {
            ropeSegment.QueueFree();
        }
        _ropeSegments.Clear();
        foreach (var joint in _ropeJoints)
        {
            joint.QueueFree();
        }
        _ropeJoints.Clear();
    }
}