using Godot;
using System.Threading.Tasks;

public partial class TerrainChunk : Node3D
{
	private MeshInstance3D _meshInstance;
	private CollisionShape3D _collisionShape;
	private Terrain _terrain;
	private bool _isGenerationComplete = false;
	private int _lod;

	public void QueueGeneration(Terrain terrain, Vector2 position, float size, int lod)
	{
		_terrain = terrain;
		_lod = lod;
		Name = $"Chunk_{position.X}_{position.Y}";
		Position = new Vector3(position.X, 0, position.Y);

		Task.Run(() =>
		{
			var terrainData = GenerateTerrainData(position, size, lod);
			int resolution = terrainData.Heights.GetLength(0);
			int splatmapLayers = terrainData.Splatmap.GetLength(2);
			
			var heightsArray = new float[resolution * resolution];
			var splatmapArray = new float[resolution * resolution * splatmapLayers];

			for (int z = 0; z < resolution; z++)
			{
				for (int x = 0; x < resolution; x++)
				{
					heightsArray[z * resolution + x] = terrainData.Heights[x, z];
					for (int i = 0; i < splatmapLayers; i++)
					{
						splatmapArray[(z * resolution + x) * splatmapLayers + i] = terrainData.Splatmap[x, z, i];
					}
				}
			}
			
			CallDeferred(nameof(OnGenerationComplete), heightsArray, splatmapArray, resolution, splatmapLayers, size, lod);
		});
	}

	private void OnGenerationComplete(float[] heightsArray, float[] splatmapArray, int resolution, int splatmapLayers, float size, int lod)
	{
		var terrainData = new TerrainData(resolution, splatmapLayers);
		for (int z = 0; z < resolution; z++)
		{
			for (int x = 0; x < resolution; x++)
			{
				terrainData.Heights[x, z] = heightsArray[z * resolution + x];
				for (int i = 0; i < splatmapLayers; i++)
				{
					terrainData.Splatmap[x, z, i] = splatmapArray[(z * resolution + x) * splatmapLayers + i];
				}
			}
		}

		BuildMeshAsync(terrainData, size, lod);
		_isGenerationComplete = true;
	}


	private void BuildMeshAsync(TerrainData data, float size, int lod)
	{
		var chunkWorldPosition = Position;

		Task.Run(() =>
		{
			var st = new SurfaceTool();
			st.Begin(Mesh.PrimitiveType.Triangles);

			int resolution = _terrain.ChunkSize;
			float step = size / resolution;

			for (int z = 0; z < resolution + 1; z++)
			{
				for (int x = 0; x < resolution + 1; x++)
				{
					float xPos = x * step;
					float zPos = z * step;
					float yPos = data.Heights[x, z];
					
					var uv = new Vector2(chunkWorldPosition.X + xPos, chunkWorldPosition.Z + zPos);

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
			
			CallDeferred(nameof(OnMeshGenerated), mesh);
		});
	}

	private void OnMeshGenerated(Mesh mesh)
	{
		if (_meshInstance == null)
		{
			_meshInstance = new MeshInstance3D();
			AddChild(_meshInstance);
		}

		_meshInstance.Mesh = mesh;
		
		if (_terrain.UseTestMaterial)
		{
			_meshInstance.MaterialOverride = _terrain.TestMaterial;
		}
		else
		{
			_meshInstance.MaterialOverride = _terrain.SplatmapMaterial;
		}

		if (_lod == _terrain.MaxLODs)
		{
			if (_collisionShape == null)
			{
				_collisionShape = new CollisionShape3D();
				AddChild(_collisionShape);
			}
			_collisionShape.Shape = mesh.CreateTrimeshShape();
		}
	}

	private TerrainData GenerateTerrainData(Vector2 position, float size, int lod)
	{
		int resolution = _terrain.ChunkSize;
		if (resolution < 1) resolution = 1;

		var terrainData = new TerrainData(resolution + 1, Terrain.MAX_TEXTURES);
		float step = size / resolution;

		foreach (var layer in _terrain.Layers)
		{
			if (layer != null)
			{
				layer.Apply(terrainData, resolution + 1, position, lod, step);
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
}
