// TerrainSystem/Layers/RoadGenerationLayer.cs

using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class RoadGenerationLayer : TerrainLayer
{
	[ExportGroup("Road Settings")]
	[Export(PropertyHint.Range, "1.0, 50.0")] public float RoadWidth = 8.0f;
	[Export(PropertyHint.Range, "0.0, 1.0")] public float FlattenStrength = 0.9f;
	[Export(PropertyHint.Range, "0.0, 100.0")] public float RoadHeightOffset = 0.5f;

	[ExportGroup("Texture Blending")]
	[Export] public bool UseForTexturing = true;
	[Export(PropertyHint.Range, "0, 3")] public int TextureIndex = 1;
	[Export(PropertyHint.Range, "0.0, 1.0")] public float TextureStrength = 1.0f;

	private CityDataManager _cityDataManager;
	// This dictionary will now be populated with heights for ALL cities.
	private Dictionary<Vector2, float> _allCityHeights = new Dictionary<Vector2, float>();

	public override void Apply(TerrainData data, int resolution, Vector2 position, int lod, float step)
	{
		if (_cityDataManager == null)
		{
			_cityDataManager = CityDataManager.Instance;
		}

		var cityRings = _cityDataManager.CityRings;
		if (cityRings == null || cityRings.Count == 0)
		{
			return;
		}

		// --- New Logic ---
		// Pre-calculate the height of every city on the map before we start drawing.
		// We only need to do this once.
		if (_allCityHeights.Count == 0 && Terrain != null)
		{
			foreach (var cityCenter in _cityDataManager.CityCenters)
			{
				float height = Terrain.GetHeightUntilLayer(new Vector3(cityCenter.X, 0, cityCenter.Y), this);
				_allCityHeights[cityCenter] = height;
			}
		}

		// Draw roads connecting cities using the pre-calculated heights.
		DrawRoads(cityRings, data, resolution, position, step);
	}

	// Method no longer needed, we calculate all heights in Apply.
	// private void CacheCityHeights(...) { ... }

	private void DrawRoads(List<List<Vector2>> cityRings, TerrainData data, int resolution, Vector2 position, float step)
	{
		// This logic remains the same, but it will now call the updated DrawRoadSegment.
		for (int i = 0; i < cityRings.Count; i++)
		{
			var ring = cityRings[i];
			if (ring.Count < 2) continue;
			
			for (int j = 0; j < ring.Count; j++)
			{
				Vector2 cityA = ring[j];
				Vector2 cityB = ring[(j + 1) % ring.Count];
				DrawRoadSegment(cityA, cityB, data, resolution, position, step);
			}
		}

		for (int i = 0; i < cityRings.Count - 1; i++)
		{
			var currentRing = cityRings[i];
			var nextRing = cityRings[i + 1];

			foreach (var city in currentRing)
			{
				Vector2 closestCity = FindClosestCity(city, nextRing);
				DrawRoadSegment(city, closestCity, data, resolution, position, step);
			}
		}
	}

	private Vector2 FindClosestCity(Vector2 city, List<Vector2> ring)
	{
		// This logic remains the same.
		Vector2 closest = Vector2.Zero;
		float minDistanceSq = float.MaxValue;

		foreach (var otherCity in ring)
		{
			float distSq = city.DistanceSquaredTo(otherCity);
			if (distSq < minDistanceSq)
			{
				minDistanceSq = distSq;
				closest = otherCity;
			}
		}
		return closest;
	}

	private void DrawRoadSegment(Vector2 start, Vector2 end, TerrainData data, int resolution, Vector2 position, float step)
	{
		// --- Modified Logic ---
		// Use the globally calculated heights instead of a local cache.
		if (!_allCityHeights.ContainsKey(start) || !_allCityHeights.ContainsKey(end)) return;

		float startHeight = _allCityHeights[start];
		float endHeight = _allCityHeights[end];

		// The rest of this method remains the same.
		for (int z = 0; z < resolution; z++)
		{
			for (int x = 0; x < resolution; x++)
			{
				float worldX = position.X + x * step;
				float worldZ = position.Y + z * step;
				var worldPos = new Vector2(worldX, worldZ);

				float distance = DistanceToLineSegment(worldPos, start, end);

				if (distance < RoadWidth / 2.0f)
				{
					Vector2 roadVector = end - start;
					Vector2 pointVector = worldPos - start;
					float t = Mathf.Clamp(pointVector.Dot(roadVector) / roadVector.LengthSquared(), 0.0f, 1.0f);

					float roadHeight = Mathf.Lerp(startHeight, endHeight, t) + RoadHeightOffset;

					data.Heights[x, z] = Mathf.Lerp(data.Heights[x, z], roadHeight, FlattenStrength);

					if (UseForTexturing)
					{
						data.Splatmap[x, z, TextureIndex] += TextureStrength;
					}
				}
			}
		}
	}

	private float DistanceToLineSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		// This logic remains the same.
		float l2 = a.DistanceSquaredTo(b);
		if (l2 == 0.0) return p.DistanceTo(a);
		float t = Mathf.Max(0, Mathf.Min(1, (p - a).Dot(b - a) / l2));
		Vector2 projection = a + t * (b - a);
		return p.DistanceTo(projection);
	}
}
