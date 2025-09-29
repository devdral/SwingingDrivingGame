using Godot;

[GlobalClass]
public partial class Terrain : Node3D
{
    [ExportGroup("Dimensions")]
    [Export] public int TerrainWidth = 1024;
    [Export] public int TerrainDepth = 1024;
    [Export] public int ChunkSize = 128;

    [ExportGroup("Generation")]
    [Export] public TerrainLayer[] Layers { get; set; } = [];
    [Export(PropertyHint.Range, "0, 5")] public int MaxLODs = 3;

    [ExportGroup("Materials")]
    [Export] public ShaderMaterial SplatmapMaterial { get; set; }
    [Export] public TerrainMaterial[] Materials { get; set; } = new TerrainMaterial[MAX_TEXTURES];

    [Export] public Node3D Viewer { get; set; }

    public const int MAX_TEXTURES = 4;
    private Quadtree _quadtree;

    public override void _Ready()
    {
        if (Viewer == null)
        {
            GD.PrintErr("Viewer not set for terrain LOD system.");
            return;
        }

        // Pass the texture arrays to the splatmap material's shader
        if (SplatmapMaterial != null)
        {
            var albedos = new Godot.Collections.Array<Texture2D>();
            var normals = new Godot.Collections.Array<Texture2D>();
            var roughnesses = new Godot.Collections.Array<Texture2D>();

            foreach (var material in Materials)
            {
                if (material != null)
                {
                    albedos.Add(material.AlbedoTexture);
                    normals.Add(material.NormalTexture);
                    roughnesses.Add(material.RoughnessTexture);
                }
            }

            SplatmapMaterial.SetShaderParameter("albedo_textures", albedos);
            SplatmapMaterial.SetShaderParameter("normal_textures", normals);
            SplatmapMaterial.SetShaderParameter("roughness_textures", roughnesses);
        }

        _quadtree = new Quadtree(this, MaxLODs);
        UpdateTerrain();
    }

    public override void _Process(double delta)
    {
        if (_quadtree != null && Viewer != null)
        {
            UpdateTerrain();
        }
    }

    private void UpdateTerrain()
    {
        _quadtree.Update(Viewer.GlobalTransform.Origin);
    }
}