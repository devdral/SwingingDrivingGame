// TerrainSystem/Layers/TerrainLayer.cs

using Godot;

/// <summary>
/// Each layer takes the TerrainData, modifies it, and passes it on.
/// </summary>
[GlobalClass]
public abstract partial class TerrainLayer : Resource
{
	// Add a protected property to hold a reference to the main terrain.
	protected Terrain Terrain { get; private set; }

	// Add a virtual method to initialize the layer with the terrain reference.
	public virtual void Init(Terrain terrain)
	{
		Terrain = terrain;
	}

	public abstract void Apply(TerrainData data, int resolution, Vector2 position, int lod, float step);
}
