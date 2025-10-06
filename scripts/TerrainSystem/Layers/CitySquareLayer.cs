using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class CitySquareLayer : TerrainLayer
{
	[ExportGroup("City Generation")]
	[Export(PropertyHint.Range, "50.0, 500.0")] public float SquareSize = 200.0f;
	[Export(PropertyHint.Range, "0.0, 1.0")] public float FlattenStrength = 0.8f;

	private CityDataManager _cityDataManager;
	
	// This dictionary will cache the calculated average height for each city center.
	// This is the key to solving the seam issue.
	private Dictionary<Vector2, float> _cityAverageHeights = new Dictionary<Vector2, float>();

	// Add a lock object for thread safety
	private readonly object _heightCacheLock = new();

	// This method is hypothetical, but if your terrain generator has a "start" signal,
	// it's good practice to connect it to clear the cache for new generations.
	public void OnGenerationStart()
	{
		lock (_heightCacheLock)
		{
			_cityAverageHeights.Clear();
		}
	}

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
						float averageHeight;
						bool heightExists;

						// Lock the dictionary for reading
						lock (_heightCacheLock)
						{
							heightExists = _cityAverageHeights.TryGetValue(center, out averageHeight);
						}

						// If it's not in the cache, calculate it now.
						if (!heightExists)
						{
							averageHeight = GetAverageHeight(center, SquareSize, resolution, position, step, data);
							
							// Lock the dictionary for writing.
							// All other chunks that touch this city will now use this exact value.
							lock (_heightCacheLock)
							{
								_cityAverageHeights[center] = averageHeight;
							}
						}
						
						// All vertices for this city square will now lerp to the same height.
						data.Heights[x, z] = Mathf.Lerp(data.Heights[x, z], averageHeight, FlattenStrength);
					}
				}
			}
		}
	}

	private float GetAverageHeight(Vector2 center, float size, int resolution, Vector2 position, float step, TerrainData data)
	{
		// This function remains unchanged. Its flaw (being chunk-local) is now
		// mitigated by the caching logic in the Apply method.
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
