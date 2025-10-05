using Godot;
using System.Collections.Generic;

public class Quadtree
{
	public QuadtreeNode Root { get; private set; }

	public Quadtree(Terrain terrain, int maxLODs)
	{
		Root = new QuadtreeNode(terrain, Vector2.Zero, terrain.TerrainWidth, 0, maxLODs);
	}

	public void Update(Vector3 viewerPosition)
	{
		Root.Update(viewerPosition);
	}

	public void PreGenerate()
	{
		Root.PreGenerate();
	}
}

public class QuadtreeNode
{
	public Vector2 Position { get; private set; }
	public float Size { get; private set; }
	public int Lod { get; private set; }
	public QuadtreeNode[] Children { get; private set; }
	public TerrainChunk Chunk { get; private set; }

	private readonly Terrain _terrain;
	private readonly int _maxLODs;

	public QuadtreeNode(Terrain terrain, Vector2 position, float size, int lod, int maxLODs)
	{
		_terrain = terrain;
		Position = position;
		Size = size;
		Lod = lod;
		_maxLODs = maxLODs;
	}

	public void Update(Vector3 viewerPosition)
	{
		float distance = new Vector2(viewerPosition.X, viewerPosition.Z).DistanceTo(Position + new Vector2(Size / 2, Size / 2));

		if (distance < Size && Lod < _maxLODs)
		{
			if (Children == null)
			{
				Subdivide();
			}
			foreach (var child in Children)
			{
				child.Update(viewerPosition);
			}

			if (Chunk != null)
			{
				bool allChildrenReady = true;
				if (Children != null)
				{
					foreach (var child in Children)
					{
						if (child.Chunk == null || !child.Chunk.IsGenerationComplete)
						{
							allChildrenReady = false;
							break;
						}
					}
				}
				else
				{
					allChildrenReady = false;
				}

				if (allChildrenReady)
				{
					Chunk.Hide();
				}
			}
		}
		else
		{
			if (Children != null)
			{
				Merge();
			}
			if (Chunk == null)
			{
				Chunk = new TerrainChunk();
				_terrain.AddChild(Chunk);
				Chunk.QueueGeneration(_terrain, Position, Size, Lod);
			}
			else
			{
				Chunk.Show();
			}
		}
	}

	public void PreGenerate()
	{
		// Create a chunk for the current node.
		if (Chunk == null)
		{
			Chunk = new TerrainChunk();
			_terrain.AddChild(Chunk);
			Chunk.QueueGeneration(_terrain, Position, Size, Lod);
		}

		// If not at the max LOD, subdivide and recurse.
		if (Lod < _maxLODs)
		{
			if (Children == null)
			{
				Subdivide();
			}
			foreach (var child in Children)
			{
				child.PreGenerate();
			}
		}
	}

	private void Subdivide()
	{
		Children = new QuadtreeNode[4];
		float halfSize = Size / 2;
		int nextLod = Lod + 1;

		Children[0] = new QuadtreeNode(_terrain, Position, halfSize, nextLod, _maxLODs);
		Children[1] = new QuadtreeNode(_terrain, new Vector2(Position.X + halfSize, Position.Y), halfSize, nextLod, _maxLODs);
		Children[2] = new QuadtreeNode(_terrain, new Vector2(Position.X, Position.Y + halfSize), halfSize, nextLod, _maxLODs);
		Children[3] = new QuadtreeNode(_terrain, Position + new Vector2(halfSize, halfSize), halfSize, nextLod, _maxLODs);
	}

	private void Merge()
	{
		if (Children == null) return;

		foreach (var child in Children)
		{
			child.Merge();
			child.Chunk?.QueueFree();
		}
		Children = null;
	}
}
