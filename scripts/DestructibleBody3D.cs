using Godot;
using System;
using Godot.Collections;

namespace SwingingDrivingGame;

/// <summary>
/// A class that allows you to display mesh in the world, with automatic collision. If the Node is ever collided with by a Car,
/// the Car's DestroyVelocity threshold will be checked; if it's high enough, the mesh will shatter procedurally into many pieces.
/// </summary>
[Tool]
[GlobalClass]
public partial class DestructibleBody3D : Node3D
{
    [Export]
    public Mesh Mesh
    {
        get => _mesh;
        set
        {
            _mesh = value;
            if (Engine.IsEditorHint())
            {
                GD.Print(_meshInstance);
                if (_meshInstance is not null)
                {
                    _meshInstance.Mesh = value;   
                }
            }
            else
                SwapMesh();
        }
    }

    private Mesh _mesh;

    [Export] public int ShatterStrength { get; set; } = 4;
    [Export] public float ExplosionStrength { get; set; } = 10;
    
    private StaticBody3D _staticBody;
    private RigidBody3D _rigidBody;
    private CollisionShape3D _collider;
    private MeshInstance3D _meshInstance;

    private GDScript _scriptForDestronoi;
    private bool _alreadyDestroyed;

    public DestructibleBody3D() {}

    public DestructibleBody3D(Mesh mesh)
    {
        Mesh = mesh;
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            var editorMi = new MeshInstance3D();
            editorMi.Mesh = Mesh;
            _meshInstance = editorMi;
            AddChild(editorMi);
            return;
        }
        _scriptForDestronoi = GD.Load<GDScript>("res://addons/destronoi/DestronoiNode.gd");
    }

    public void SwapMesh()
    {
        _alreadyDestroyed = false;
        _rigidBody?.QueueFree();
        _rigidBody = null;
        _staticBody = new StaticBody3D();
        var mi = new MeshInstance3D();
        mi.Mesh = _mesh;
        _meshInstance = mi;
        _staticBody.AddChild(mi);
        var collider = new CollisionShape3D();
        collider.Shape = _mesh.CreateConvexShape();
        _collider = collider;
        _staticBody.AddChild(collider);
        AddChild(_staticBody);
        _staticBody.SetCollisionLayerValue(3,true);
    }

    public void ProcessCollisionWithCar(Car car, Vector3 collisionVelocity)
    {
        if (car.CurrentSpeed >= car.DestructionVelocity && !_alreadyDestroyed)
        {
            _rigidBody = new RigidBody3D();
            _meshInstance.Reparent(_rigidBody);
            _collider.Reparent(_rigidBody);
            _staticBody.QueueFree();
            // Remember that this is of type Destronoi,
            // although that class doesn't exist in C# land
            Node destronoi = (Node)_scriptForDestronoi.New();
            AddChild(_rigidBody);
            destronoi.Set("tree_height", ShatterStrength); // in GDScript: destronoi.tree_height = 6
            _rigidBody.AddChild(destronoi);
            destronoi.Call("destroy", ShatterStrength, ShatterStrength, 0); // in GDScript: destronoi.destroy(4,4, 10)
            foreach (var node in GetChildren())
            {
                if (node is RigidBody3D body)
                {
                    body.ApplyForce(collisionVelocity * .5f);
                }
            }
            _alreadyDestroyed = true;
        }
    }
}
