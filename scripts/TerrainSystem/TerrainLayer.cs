using Godot;

/// <summary>
/// Each layer takes the TerrainData, modifies it, and passes it on.
/// </summary>
[GlobalClass]
public abstract partial class TerrainLayer : Resource
{
    public abstract void Apply(TerrainData data, int resolution, Vector2 position, int lod, float step);
}