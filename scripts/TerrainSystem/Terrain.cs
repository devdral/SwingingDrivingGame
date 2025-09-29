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