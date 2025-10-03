using Godot;

public partial class TerrainChunk : Node3D
{
	private MeshInstance3D _meshInstance;
	private CollisionShape3D _collisionShape;
	private Terrain _terrain;

	public void Generate(Terrain terrain, Vector2 position, float size, int lod)
	{
		_terrain = terrain;
		Name = $"Chunk_{position.X}_{position.Y}";
		Position = new Vector3(position.X, 0, position.Y);

		var terrainData = GenerateTerrainData(position, size, lod);
		BuildMesh(terrainData, size, lod);
	}

	private TerrainData GenerateTerrainData(Vector2 position, float size, int lod)
	{
		int resolution = _terrain.ChunkSize; // Use a fixed resolution for all chunks
		if (resolution < 1) resolution = 1;

		var terrainData = new TerrainData(resolution + 1, Terrain.MAX_TEXTURES);
		float step = size / resolution; // Calculate the world space step between vertices

		foreach (var layer in _terrain.Layers)
		{
			if (layer != null)
			{
				layer.Apply(terrainData, resolution + 1, position, lod, step); // Pass the step to the layer
			}
		}
		NormalizeSplatmap(terrainData);
		return terrainData;
	}

	private void NormalizeSplatmap(TerrainData data)
	{
		int resolution = data.Heights.GetLength(0);
		for (int z = 0; z < resolution; z++)
		{
			for (int x = 0; x < resolution; x++)
			{
				float totalStrength = 0;
				for (int i = 0; i < Terrain.MAX_TEXTURES; i++)
				{
					totalStrength += data.Splatmap[x, z, i];
				}

				if (totalStrength > 0)
				{
					for (int i = 0; i < Terrain.MAX_TEXTURES; i++)
					{
						data.Splatmap[x, z, i] /= totalStrength;
					}
				}
			}
		}
	}

	private void BuildMesh(TerrainData data, float size, int lod)
	{
		if (_meshInstance == null)
		{
			_meshInstance = new MeshInstance3D();
			AddChild(_meshInstance);
		}

		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		int resolution = _terrain.ChunkSize; // Use a fixed resolution
		float step = size / resolution;

		for (int z = 0; z < resolution + 1; z++)
		{
			for (int x = 0; x < resolution + 1; x++)
			{
				float xPos = x * step;
				float zPos = z * step;
				float yPos = data.Heights[x, z];

				var uv = new Vector2(Position.X + xPos, Position.Z + zPos);

				var color = new Color(
					data.Splatmap[x, z, 0],
					data.Splatmap[x, z, 1],
					data.Splatmap[x, z, 2],
					data.Splatmap[x, z, 3]
				);

				st.SetUV(uv);
				st.SetColor(color);
				st.AddVertex(new Vector3(xPos, yPos, zPos));
			}
		}

		for (int z = 0; z < resolution; z++)
		{
			for (int x = 0; x < resolution; x++)
			{
				int topLeft = z * (resolution + 1) + x;
				int topRight = topLeft + 1;
				int bottomLeft = (z + 1) * (resolution + 1) + x;
				int bottomRight = bottomLeft + 1;

				st.AddIndex(topLeft);
				st.AddIndex(topRight);
				st.AddIndex(bottomLeft);

				st.AddIndex(bottomLeft);
				st.AddIndex(topRight);
				st.AddIndex(bottomRight);
			}
		}

		st.GenerateNormals();
		st.GenerateTangents();

		var mesh = st.Commit();
		_meshInstance.Mesh = mesh;
		
		if (_terrain.UseTestMaterial)
		{
			_meshInstance.MaterialOverride = _terrain.TestMaterial;
		}
		else
		{
			_meshInstance.MaterialOverride = _terrain.SplatmapMaterial;
		}

		if (_collisionShape == null)
		{
			_collisionShape = new CollisionShape3D();
			AddChild(_collisionShape);
		}
		_collisionShape.Shape = mesh.CreateTrimeshShape();
	}
}
