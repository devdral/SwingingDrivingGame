using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SwingingDrivingGame;

public partial class RopeManager : Node
{
    public const int RopeBoundryLayer = 3;
    public const float RopeSegmentLength = 0.5f; // 500mm
    
    public float MaxDist { get; set; }
    public float RopeRadius { get; set; }
    
    private Node3D _ropeSegmentAttachPoint;
    private Car _car;
    
    private bool _isUsingRope;
    private StaticBody3D _lastSegmentBoundryCollider;
    private float _totalLength;
    private List<RigidBody3D> _ropeSegments = [];
    private List<PinJoint3D> _ropeJoints = [];
    private RigidBody3D _ropeEndpoint;
    private Vector3 _lastVel = Vector3.Zero;

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
        var targetDistance = startPos.DistanceTo(_car.Position);
        if (targetDistance > MaxDist)
            return;
        _isUsingRope = true;
        var distance = 0f;
        var position = startPos;
        var endPos = _car.GlobalPosition;
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
            mesh.Height = RopeSegmentLength;
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
                joint.SetParam(PinJoint3D.Param.Damping, 10000f);
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
            
            position += direction * RopeSegmentLength/2;
            distance += RopeSegmentLength/2;
        }

        if (lastObj is not null)
        {
            var finalJoint = new PinJoint3D();
            finalJoint.Position = _car.Position;
            finalJoint.NodeA = lastObj.GetPath();
            
            var body = new RigidBody3D();
            body.Position = position;
            
            var collider = new CollisionShape3D();
            var shape = _car.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
            collider.Shape = shape;
            body.AddChild(collider);
            body.Mass = 100;
            
            _ropeSegmentAttachPoint.AddChild(body);
            finalJoint.NodeB = body.GetPath();
            _ropeSegmentAttachPoint.AddChild(finalJoint);
            _ropeJoints.Add(finalJoint);
            _ropeEndpoint = body;
            // var rotQuat = Quaternion.FromEuler(_characterBody.Rotation);
            // body.LinearVelocity = Vector3.Forward.Rotated(rotQuat.GetAxis(), rotQuat.GetAngle()) * 100;
            body.LinearVelocity = _car.Velocity with { Y = 0 };
        }
        _lastVel = _ropeEndpoint.LinearVelocity;
    }

    public Vector3 GetRopeEndpoint()
    {
        if (!_isUsingRope)
            throw new InvalidOperationException("Cannot get rope endpoint when there is no rope.");
        return _ropeEndpoint.Position;
    }

    // public void AddRopeEndpointVel(Vector3 vel)
    // {
    //     _ropeEndpoint.ManualVelocity = vel;
    // }
    
    public void DisableRope()
    {
        if (!_isUsingRope)
            return;
        _car.SetCurrentSpeed(_ropeEndpoint.LinearVelocity.Length());
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