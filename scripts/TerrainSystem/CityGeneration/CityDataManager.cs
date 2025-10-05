using Godot;
using System.Collections.Generic;

public partial class CityDataManager : Node
{
	public static CityDataManager Instance { get; private set; }

	[Export] public int Seed = 1337;
	[Export(PropertyHint.Range, "100.0, 1000.0")] public float RingDistance = 500.0f;
	[Export(PropertyHint.Range, "1, 20")] public int CitiesPerRing = 8;

	// Flat list of all city centers, used by CitySquareLayer
	public List<Vector2> CityCenters { get; private set; } = new List<Vector2>();
	
	// List of city rings, used by RoadGenerationLayer
	public List<List<Vector2>> CityRings { get; private set; } = new List<List<Vector2>>();

	public override void _Ready()
	{
		Instance = this;
		GenerateCityLocations();
	}

	private void GenerateCityLocations()
	{
		// The maximum distance you want to generate cities for.
		// This should be large enough to cover your terrain.
		float maxGenerationRadius = 5000.0f; 
		int maxRingIndex = (int)(maxGenerationRadius / RingDistance);

		// Always include the central city as the first "ring"
		var centralCityRing = new List<Vector2> { Vector2.Zero };
		CityCenters.Add(Vector2.Zero);
		CityRings.Add(centralCityRing);

		for (int i = 1; i <= maxRingIndex; i++)
		{
			var currentRing = new List<Vector2>();
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

				var cityPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentRadius;
				CityCenters.Add(cityPosition);
				currentRing.Add(cityPosition);
			}
			CityRings.Add(currentRing);
		}
	}
}
