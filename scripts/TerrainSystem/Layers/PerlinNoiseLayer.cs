using Godot;

[GlobalClass]
public partial class PerlinNoiseLayer : TerrainLayer
{
    [ExportGroup("Noise Shape")]
    [Export] public int NoiseSeed = 1337;
    [Export(PropertyHint.Range, "0.1, 512.0")] public float NoiseScale = 50.0f;
    [Export(PropertyHint.Range, "0.0, 100.0")] public float Strength = 10.0f;

    [ExportGroup("Fractal Settings")]
    [Export(PropertyHint.Range, "1, 8")] public int Octaves = 4;
    [Export(PropertyHint.Range, "0.1, 1.0")] public float Persistence = 0.5f;
    [Export(PropertyHint.Range, "1.0, 4.0")] public float Lacunarity = 2.0f;

    [ExportGroup("Texture Blending")]
    [Export] public bool UseForTexturing = false;
    [Export(PropertyHint.Range, "0, 3")] public int TextureIndex = 0;
    [Export(PropertyHint.Range, "0.0, 1.0")] public float TextureStrength = 1.0f;

    public override void Apply(TerrainData data, int resolution, Vector2 position, int lod, float step)
    {
        var noise = new FastNoiseLite
        {
            Seed = NoiseSeed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = Octaves,
            FractalGain = Persistence,
            FractalLacunarity = Lacunarity,
            Frequency = 1.0f / NoiseScale
        };

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = position.X + x * step;
                float worldZ = position.Y + z * step;

                // Get noise value between -1 and 1
                float noiseValue = noise.GetNoise2D(worldX, worldZ);

                // Add to the existing height
                data.Heights[x, z] += noiseValue * Strength;

                // If configured, add to the splatmap
                if (UseForTexturing)
                {
                    // TODO: normalize
                    data.Splatmap[x, z, TextureIndex] += TextureStrength;
                }
            }
        }
    }
}