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

        // Initialize the first splatmap layer to 1.0
        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                Splatmap[x, z, 0] = 1.0f;
            }
        }
    }
}