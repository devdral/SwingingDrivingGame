using Godot;
using System.Collections.Generic;

public partial class CityDataManager : Node
{
	public static CityDataManager Instance { get; private set; }

	[Export] public int Seed = 1337;
	[Export(PropertyHint.Range, "100.0, 1000.0")] public float RingDistance = 500.0f;
	[Export(PropertyHint.Range, "1, 20")] public int CitiesPerRing = 8;

	public List<Vector2> CityCenters { get; private set; } = new List<Vector2>();

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

		// Always include the central city
		CityCenters.Add(Vector2.Zero);

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

				CityCenters.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentRadius);
			}
		}
	}
}
