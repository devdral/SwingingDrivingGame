using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SwingingDrivingGame;

public partial class RopeManager : Node
{
    public const int RopeBoundryLayer = 3;
    public const float RopeSegmentLength = 1.5f; // 500mm
    
    public float MaxDist { get; set; }
    public float RopeRadius { get; set; }
    public float RopeVImp { get; set; }
    
    private Node3D _ropeSegmentAttachPoint;
    private Car _car;
    
    private bool _isUsingRope;
    private List<RigidBody3D> _ropeSegments = [];
    private List<PinJoint3D> _ropeJoints = [];
    private RigidBody3D _ropeEndpoint;
    private Vector3 _lastVel = Vector3.Zero;
    private Vector3 _startPos;
    private List<Node3D> _ropeMeshes = [];

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
            shape.Radius = 0.5f;
            collider.Shape = shape;
            @object.AddChild(collider);
            @object.Position = position;
            @object.SetCollisionLayerValue(1,false);
            @object.SetCollisionLayerValue(2,true);
            @object.SetCollisionMaskValue(1,true);
            // @object.Mass = 0;
            _ropeSegmentAttachPoint.AddChild(@object);
            _ropeSegments.Add(@object);
            
            if (lastObj is not null)
            {
                var joint = new PinJoint3D();
                joint.SetParam(PinJoint3D.Param.Damping, 100f);
                joint.Position = @object.Position;
                joint.NodeA = lastObj.GetPath();
                joint.NodeB = @object.GetPath();
                _ropeJoints.Add(joint);
                _ropeSegmentAttachPoint.AddChild(joint);
                var shownSegment = new Node3D();
                var mi2 = new MeshInstance3D();
                var localDist = lastObj.Position.DistanceTo(@object.Position);
                mi2.Mesh = new CylinderMesh
                {
                    TopRadius = .125f,
                    BottomRadius = .125f,
                    Height = localDist, 
                };
                // mi2.Position = mi2.Position with { Z = localDist / 2f };
                mi2.RotationDegrees = new Vector3(90, 0, 0);
                _ropeMeshes.Add(shownSegment);
                shownSegment.AddChild(mi2);
                @object.AddChild(shownSegment);
                shownSegment.LookAt(lastObj.Position);
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
            finalJoint.Position = position;
            finalJoint.NodeA = lastObj.GetPath();
            
            var body = new RigidBody3D();
            body.Position = position;
            
            var collider = new CollisionShape3D();
            var shape = _car.GetNode<CollisionShape3D>("CollisionShape3D").Shape;
            body.Transform = _car.Transform;
            collider.Shape = shape;
            body.AddChild(collider);
            // body.Mass = 100000;
            body.SetCollisionMaskValue(1,true);
            
            _ropeSegmentAttachPoint.AddChild(body);
            finalJoint.NodeB = body.GetPath();
            _ropeSegmentAttachPoint.AddChild(finalJoint);
            _ropeJoints.Add(finalJoint);
            _ropeEndpoint = body;
            // var rotQuat = Quaternion.FromEuler(_characterBody.Rotation);
            // body.LinearVelocity = Vector3.Forward.Rotated(rotQuat.GetAxis(), rotQuat.GetAngle()) * 100;
            body.LinearVelocity = _car.Velocity with { Y = RopeVImp };
        }
        _lastVel = _ropeEndpoint.LinearVelocity;
        _car.Position = _ropeEndpoint.Position;
        _startPos = startPos;
    }

    public Vector3 GetRopeEndpoint()
    {
        if (!_isUsingRope)
            throw new InvalidOperationException("Cannot get rope endpoint when there is no rope.");
        return _ropeEndpoint.Position;
    }

    public Vector3 GetRopeEndpointVel()
    {
        if (!_isUsingRope)
            throw new InvalidOperationException("Cannot get rope endpoint when there is no rope.");
        return _ropeEndpoint.LinearVelocity;
    }

    public void AddRopeEndpointVel(Vector3 vel)
    {
        _ropeEndpoint.LinearVelocity += vel;
    }
    
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

    // public override void _PhysicsProcess(double delta)
    // {
    //     if (_isUsingRope)
    //     {
    //         for (var i = 0; i < _ropeSegments.Count; i++)
    //         {
    //             var segment = _ropeJoints[i];
    //             var mesh = _ropeMeshes[i];
    //             var meshMesh = (CylinderMesh)mesh.GetChild<MeshInstance3D>(0).Mesh;
    //             Vector3 position;
    //             if (i < _ropeSegments.Count - 1)
    //                 position = _ropeJoints[i+1].Position;
    //             else
    //                 position = _car.Position;
    //             mesh.LookAt(position);
    //             meshMesh.Height = segment.Position.DistanceTo(position);
    //         }
    //     }
    //     // _car.LookAt(_startPos);
    // }
}