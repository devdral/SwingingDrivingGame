using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class CitySquareLayer : TerrainLayer
{
	[ExportGroup("City Generation")]
	[Export(PropertyHint.Range, "50.0, 500.0")] public float SquareSize = 200.0f;
	[Export(PropertyHint.Range, "0.0, 1.0")] public float FlattenStrength = 0.8f;

	private CityDataManager _cityDataManager;

	public override void Apply(TerrainData data, int resolution, Vector2 position, int lod, float step)
	{
		if (_cityDataManager == null)
		{
			_cityDataManager = CityDataManager.Instance;
		}

		var cityCenters = _cityDataManager.CityCenters;

		for (int z = 0; z < resolution; z++)
		{
			for (int x = 0; x < resolution; x++)
			{
				float worldX = position.X + x * step;
				float worldZ = position.Y + z * step;
				var worldPos = new Vector2(worldX, worldZ);

				foreach (var center in cityCenters)
				{
					if (Mathf.Abs(worldPos.X - center.X) < SquareSize / 2.0f &&
						Mathf.Abs(worldPos.Y - center.Y) < SquareSize / 2.0f)
					{
						float averageHeight = GetAverageHeight(center, SquareSize, resolution, position, step, data);
						data.Heights[x, z] = Mathf.Lerp(data.Heights[x, z], averageHeight, FlattenStrength);
					}
				}
			}
		}
	}

	private float GetAverageHeight(Vector2 center, float size, int resolution, Vector2 position, float step, TerrainData data)
	{
		float totalHeight = 0;
		int count = 0;

		int startX = Mathf.Max(0, (int)((center.X - size / 2.0f - position.X) / step));
		int endX = Mathf.Min(resolution, (int)((center.X + size / 2.0f - position.X) / step));
		int startZ = Mathf.Max(0, (int)((center.Y - size / 2.0f - position.Y) / step));
		int endZ = Mathf.Min(resolution, (int)((center.Y + size / 2.0f - position.Y) / step));

		for (int z = startZ; z < endZ; z++)
		{
			for (int x = startX; x < endX; x++)
			{
				totalHeight += data.Heights[x, z];
				count++;
			}
		}
		return count > 0 ? totalHeight / count : 0;
	}
}
