using Godot;
using Godot.Collections; // Important: Add this using statement

[GlobalClass]
public partial class Terrain : Node3D
{
	[ExportGroup("Dimensions")]
	[Export] public int TerrainWidth = 100; // World units
	[Export] public int TerrainDepth = 100; // World units
	[Export(PropertyHint.Range, "2, 512")] public int Resolution = 128; // Number of vertices on one side

	[ExportGroup("Generation")]
	// This is the line that has been changed back
	[Export] public TerrainLayer[] Layers { get; set; } = [];

	// This bool acts as a button in the inspector to regenerate the terrain
	private bool _generate;
	[Export]
	public bool Generate
	{
		get => _generate;
		set
		{
			_generate = value;
			if (_generate)
			{
				GenerateTerrain();
			}
		}
	}

	private MeshInstance3D _meshInstance;
	private const int MAX_TEXTURES = 4; // Corresponds to RGBA channels in vertex color

	public void GenerateTerrain()
	{
		// Changed Layers.Count to Layers.Length
		if (Layers == null || Layers.Length == 0)
		{
			GD.PrintErr("No terrain layers assigned. Cannot generate terrain.");
			return;
		}

		// ... (rest of the file is unchanged)
		// Ensure MeshInstance3D child exists
		_meshInstance = GetNodeOrNull<MeshInstance3D>("TerrainMesh");
		if (_meshInstance == null)
		{
			_meshInstance = new MeshInstance3D();
			_meshInstance.Name = "TerrainMesh";
			AddChild(_meshInstance);
		}

		// 1. Process Layers
		var terrainData = new TerrainData(Resolution, MAX_TEXTURES);
		foreach (var layer in Layers)
		{
			if (layer != null)
			{
				layer.Apply(terrainData, Resolution);
			}
		}

		// 2. Normalize Splatmap Data
		NormalizeSplatmap(terrainData);

		// 3. Build Mesh from final data
		BuildMesh(terrainData);

		GD.Print("Terrain generated successfully.");
	}

	private void NormalizeSplatmap(TerrainData data)
	{
		for (int z = 0; z < Resolution; z++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				float totalStrength = 0;
				for (int i = 0; i < MAX_TEXTURES; i++)
				{
					totalStrength += data.Splatmap[x, z, i];
				}

				if (totalStrength > 0)
				{
					for (int i = 0; i < MAX_TEXTURES; i++)
					{
						data.Splatmap[x, z, i] /= totalStrength;
					}
				}
				else
				{
					// If no texture is applied, default to the first one
					data.Splatmap[x, z, 0] = 1.0f;
				}
			}
		}
	}

	private void BuildMesh(TerrainData data)
	{
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		// Define vertex attributes for the splatmap shader
		st.SetColor(new Color(1, 1, 1));
		st.SetUV(new Vector2(0, 0));

		// Create Vertices
		for (int z = 0; z < Resolution; z++)
		{
			for (int x = 0; x < Resolution; x++)
			{
				float xPos = (float)x / (Resolution - 1) * TerrainWidth;
				float zPos = (float)z / (Resolution - 1) * TerrainDepth;
				float yPos = data.Heights[x, z];

				Vector2 uv = new Vector2((float)x / (Resolution - 1), (float)z / (Resolution - 1));
				Color splatColor = new Color(
					data.Splatmap[x, z, 0],
					data.Splatmap[x, z, 1],
					data.Splatmap[x, z, 2],
					data.Splatmap[x, z, 3]
				);

				st.SetUV(uv);
				st.SetColor(splatColor); // Pack splat weights into vertex color
				st.AddVertex(new Vector3(xPos, yPos, zPos));
			}
		}

		// Create Indices
		for (int z = 0; z < Resolution - 1; z++)
		{
			for (int x = 0; x < Resolution - 1; x++)
			{
				int topLeft = z * Resolution + x;
				int topRight = topLeft + 1;
				int bottomLeft = (z + 1) * Resolution + x;
				int bottomRight = bottomLeft + 1;

				// First triangle
				st.AddIndex(topLeft);
				st.AddIndex(bottomLeft);
				st.AddIndex(topRight);

				// Second triangle
				st.AddIndex(topRight);
				st.AddIndex(bottomLeft);
				st.AddIndex(bottomRight);
			}
		}

		st.GenerateNormals();
		st.GenerateTangents();

		var arrayMesh = st.Commit();
		_meshInstance.Mesh = arrayMesh;

		// Add a simple material for visualization until you have a splat shader
		var defaultMaterial = new StandardMaterial3D
		{
			VertexColorUseAsAlbedo = true
		};
		_meshInstance.MaterialOverride = defaultMaterial;
	}
}
