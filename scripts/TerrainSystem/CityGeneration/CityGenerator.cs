using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class CityGenerator : Node3D
{
	[Export] private PackedScene _buildingScene;
	[Export] private Terrain _terrain; // Reference to your terrain node

	[ExportGroup("Building Generation")]
	[Export(PropertyHint.Range, "5.0, 50.0")] public float MinBuildingHeight = 10.0f;
	[Export(PropertyHint.Range, "10.0, 100.0")] public float MaxBuildingHeight = 50.0f;
	[Export(PropertyHint.Range, "10.0, 50.0")] public float BuildingWidth = 20.0f;
	[Export(PropertyHint.Range, "10.0, 50.0")] public float BuildingDepth = 20.0f;
	[Export(PropertyHint.Range, "0.1, 1.0")] public float BuildingDensity = 0.5f;
	[Export] public int BuildingSeed = 42;
	[Export] public float GenerationRadius = 1000.0f; // The radius around the player to generate cities

	private CityDataManager _cityDataManager;
	private HashSet<Vector2> _generatedCities = new HashSet<Vector2>();
	private bool _terrainReady = false;

	public override void _Ready()
	{
		_cityDataManager = GetNode<CityDataManager>("/root/CityDataManager");

		if (_terrain != null)
		{
			_terrain.GenerationFinished += () => { _terrainReady = true; };
		}
		else
		{
			GD.PrintErr("Terrain node not assigned in CityGenerator.");
		}
	}

	public override void _Process(double delta)
	{
		if (!_terrainReady || _terrain.Viewer == null)
		{
			return;
		}

		var viewerPosition = _terrain.Viewer.GlobalTransform.Origin;
		var cityCenters = _cityDataManager.CityCenters;

		foreach (var center in cityCenters)
		{
			if (!_generatedCities.Contains(center))
			{
				float distance = new Vector2(viewerPosition.X, viewerPosition.Z).DistanceTo(center);
				if (distance < GenerationRadius)
				{
					GenerateCity(center);
					_generatedCities.Add(center);
				}
			}
		}
	}

	private void GenerateCity(Vector2 center)
	{
		if (_buildingScene == null)
		{
			GD.PrintErr("Building scene not assigned in CityGenerator.");
			return;
		}

		var buildingRandom = new RandomNumberGenerator();
		buildingRandom.Seed = (ulong)BuildingSeed;

		// Assuming the same SquareSize as in your CitySquareLayer
		float halfSquareSize = 200f / 2.0f;
		float startX = center.X - halfSquareSize;
		float endX = center.X + halfSquareSize;
		float startZ = center.Y - halfSquareSize;
		float endZ = center.Y + halfSquareSize;

		// Use a step that's larger than the building size to create gaps
		float stepX = BuildingWidth * 1.5f;
		float stepZ = BuildingDepth * 1.5f;

		for (float worldX = startX; worldX < endX; worldX += stepX)
		{
			for (float worldZ = startZ; worldZ < endZ; worldZ += stepZ)
			{
				if (buildingRandom.Randf() < BuildingDensity)
				{
					SpawnBuilding(worldX, worldZ, buildingRandom);
				}
			}
		}
	}

	private void SpawnBuilding(float worldX, float worldZ, RandomNumberGenerator rng)
	{
		if(_buildingScene == null) return; 
		var buildingInstance = _buildingScene.Instantiate<RigidBody3D>();

		var meshInstance = buildingInstance.GetNode<MeshInstance3D>("MeshInstance3D");
		var collisionShape = buildingInstance.GetNode<CollisionShape3D>("CollisionShape3D");

		// Get the terrain height at this position
		float terrainHeight = _terrain.GetHeight(new Vector3(worldX, 0, worldZ));

		float buildingHeight = rng.RandfRange(MinBuildingHeight, MaxBuildingHeight);

		// Set the size of the mesh and collision shape
		var buildingSize = new Vector3(BuildingWidth, buildingHeight, BuildingDepth);
		(meshInstance.Mesh as BoxMesh).Size = buildingSize;
		(collisionShape.Shape as BoxShape3D).Size = buildingSize;

		// Position the building
		buildingInstance.Position = new Vector3(
			worldX,
			terrainHeight + buildingHeight / 2.0f, // Position the base on the ground
			worldZ
		);

		AddChild(buildingInstance);
	}
}
