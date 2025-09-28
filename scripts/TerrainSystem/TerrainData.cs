using Godot;

/// <summary>
/// It holds the heightmap and the splatmap texture weights.
/// </summary>
public class TerrainData
{
	public float[,] Heights;
	public float[,,] Splatmap; // Dimensions: [x, z, texture_index]

	public TerrainData(int resolution, int splatmapLayers)
	{
		Heights = new float[resolution, resolution];
		Splatmap = new float[resolution, resolution, splatmapLayers];
	}
}
