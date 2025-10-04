using Godot;

[GlobalClass]
public partial class TerrainMaterial : Resource
{
	[Export] public Texture2D AlbedoTexture { get; set; }
	[Export] public Texture2D NormalTexture { get; set; }
	[Export] public Texture2D RoughnessTexture { get; set; }
	[Export] public Texture2D MetallicTexture { get; set; }
}
