using Godot;

[GlobalClass]
public partial class Terrain : Node3D
{
	[Signal]
	public delegate void GenerationFinishedEventHandler();

	[ExportGroup("Dimensions")]
	[Export] public int TerrainWidth = 1024;
	[Export] public int TerrainDepth = 1024;
	[Export] public int ChunkSize = 128;

	[ExportGroup("Generation")]
	[Export] public TerrainLayer[] Layers { get; set; } = [];
	[Export(PropertyHint.Range, "0, 5")] public int MaxLODs = 3;
	[Export] public bool PreGenerateOnStart { get; set; } = true;

	[ExportGroup("Materials")]
	[Export] public ShaderMaterial SplatmapMaterial { get; set; }
	[Export] public TerrainMaterial[] Materials { get; set; } = new TerrainMaterial[MAX_TEXTURES];
	[Export] public float TextureScale = 32.0f;
	[Export] public Node3D Viewer { get; set; }
	[Export] public bool UseTestMaterial { get; set; } = false;
	[Export] public StandardMaterial3D TestMaterial { get; set; }

	public const int MAX_TEXTURES = 4;
	private Quadtree _quadtree;
	private bool _isInitialGenerationComplete = false;

	public override void _Ready()
	{
		if (Layers != null)
		{
			foreach (var layer in Layers)
			{
				if (layer != null)
				{
					layer.Init(this);
				}
			}
		}
		// Pass the texture arrays to the splatmap material's shader
		if (SplatmapMaterial != null)
		{
			var albedos = new Godot.Collections.Array<Texture2D>();
			var normals = new Godot.Collections.Array<Texture2D>();
			var roughnesses = new Godot.Collections.Array<Texture2D>();
			var metallics = new Godot.Collections.Array<Texture2D>();

			foreach (var material in Materials)
			{
				if (material != null)
				{
					albedos.Add(material.AlbedoTexture);
					normals.Add(material.NormalTexture);
					roughnesses.Add(material.RoughnessTexture);
					metallics.Add(material.MetallicTexture);
				}
			}

			SplatmapMaterial.SetShaderParameter("albedo_textures", albedos);
			SplatmapMaterial.SetShaderParameter("normal_textures", normals);
			SplatmapMaterial.SetShaderParameter("roughness_textures", roughnesses);
			SplatmapMaterial.SetShaderParameter("metallic_textures", metallics);
			SplatmapMaterial.SetShaderParameter("texture_scale", TextureScale);
		}

		_quadtree = new Quadtree(this, MaxLODs);

		if (PreGenerateOnStart)
		{
			GD.Print("Starting terrain pre-generation...");
			_quadtree.PreGenerate();
			GD.Print("Terrain pre-generation queued.");
		}

		UpdateTerrain();

		// The terrain has been generated for the first time.
		_isInitialGenerationComplete = true;
		EmitSignal(SignalName.GenerationFinished);
		GD.Print("Terrain generation finished.");
	}

	public override void _Process(double delta)
	{
		if (_quadtree != null)
		{
			UpdateTerrain();
		}
	}

	private void UpdateTerrain()
	{
		_quadtree.Update(Viewer != null ? Viewer.GlobalTransform.Origin : Vector3.Zero);
	}

	/// <summary>
	/// Calculates the generated terrain height at a specific global position.
	/// This method re-runs the generation layers for a single point to ensure accuracy.
	/// </summary>
	/// <param name="globalPosition">The world-space X and Z coordinates.</param>
	/// <returns>The height of the terrain at that point.</returns>
	public float GetHeight(Vector3 globalPosition)
	{
		// Find the chunk that contains this position.
		var chunkNode = _quadtree.Root.FindNode(new Vector2(globalPosition.X, globalPosition.Z));

		if (chunkNode != null && chunkNode.Chunk != null && chunkNode.Chunk.IsGenerationComplete)
		{
			// If the chunk is ready, get the height from its data.
			return chunkNode.Chunk.GetHeight(globalPosition);
		}
		else
		{
			// Fallback to the original method if the chunk is not ready.
			var tempData = new TerrainData(1, MAX_TEXTURES);
			var position2D = new Vector2(globalPosition.X, globalPosition.Z);

			foreach (var layer in Layers)
			{
				layer.Apply(tempData, 1, position2D, 0, 1.0f);
			}

			return tempData.Heights[0, 0];
		}
	}
	/// <summary>
	/// Calculates the generated terrain height at a specific global position,
	/// stopping before a specified layer is applied. This is crucial for preventing
	/// recursive calculations within a layer.
	/// </summary>
	/// <param name="globalPosition">The world-space X and Z coordinates.</param>
	/// <param name="layerToStopAt">The TerrainLayer to stop the calculation before.</param>
	/// <returns>The height of the terrain at that point.</returns>
	public float GetHeightUntilLayer(Vector3 globalPosition, TerrainLayer layerToStopAt)
	{
		var tempData = new TerrainData(1, MAX_TEXTURES);
		var position2D = new Vector2(globalPosition.X, globalPosition.Z);

		// Apply each generation layer up to the specified stop layer.
		foreach (var layer in Layers)
		{
			if (layer == layerToStopAt)
			{
				break; // Stop before applying the target layer
			}
			layer.Apply(tempData, 1, position2D, 0, 1.0f);
		}

		// Return the calculated height.
		return tempData.Heights[0, 0];
	}
}
