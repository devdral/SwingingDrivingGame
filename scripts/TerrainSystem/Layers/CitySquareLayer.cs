using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class CitySquareLayer : TerrainLayer
{
	[ExportGroup("City Generation")]
	[Export] public int Seed = 1337;
	[Export(PropertyHint.Range, "100.0, 1000.0")] public float RingDistance = 500.0f;
	[Export(PropertyHint.Range, "1, 20")] public int CitiesPerRing = 8;
	[Export(PropertyHint.Range, "50.0, 500.0")] public float SquareSize = 200.0f;
	[Export(PropertyHint.Range, "0.0, 1.0")] public float FlattenStrength = 0.8f;

	public override void Apply(TerrainData data, int resolution, Vector2 position, int lod, float step)
	{
		var cityCenters = new List<Vector2>();

		// The maximum distance from the center of the chunk to its corner
		float maxChunkDist = new Vector2(resolution * step, resolution * step).Length();
		// The maximum ring radius to check is the distance from the origin to the chunk center, plus the chunk's corner distance
		float maxRingRadius = position.Length() + maxChunkDist;
		int maxRingIndex = (int)(maxRingRadius / RingDistance);

		// Always include the central city
		cityCenters.Add(Vector2.Zero);

		for (int i = 1; i <= maxRingIndex; i++)
		{
			float ringRadius = i * RingDistance;
			int citiesInThisRing = i * CitiesPerRing;
			var ringRandom = new RandomNumberGenerator();
			// A unique seed for each ring to ensure consistent city placement
			ringRandom.Seed = (ulong)(Seed + i);

			for (int j = 0; j < citiesInThisRing; j++)
			{
				float angle = (float)j / citiesInThisRing * Mathf.Pi * 2.0f;
				// Add some randomness to the angle and radius to make the rings look less perfect
				angle += ringRandom.RandfRange(-0.1f, 0.1f);
				float currentRadius = ringRadius + ringRandom.RandfRange(-RingDistance / 4f, RingDistance / 4f);

				cityCenters.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentRadius);
			}
		}

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

		// Determine the bounds of the square in the heightmap's coordinates
		int startX = Mathf.Max(0, (int)((center.X - size / 2.0f - position.X) / step));
		int endX = Mathf.Min(resolution, (int)((center.X + size / 2.0f - position.X) / step));
		int startZ = Mathf.Max(0, (int)((center.Y - size / 2.0f - position.Y) / step));
		int endZ = Mathf.Min(resolution, (int)((center.Y + size / 2.0f - position.Y) / step));

		// This is a simplified approach. For better performance, you could pre-calculate the average heights.
		for (int z = startZ; z < endZ; z++)
		{
			for (int x = startX; x < endX; x++)
			{
				// This is a placeholder. A more robust solution would be to sample the noise function
				// at these coordinates to get the pre-flattened height.
				// For now, this will have to do.
				totalHeight += data.Heights[x, z];
				count++;
			}
		}
		return count > 0 ? totalHeight / count : 0;
	}
}
