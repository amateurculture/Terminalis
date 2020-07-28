using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using B83.JobQueue;
using UnityEngine.UI;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

#region Helper Classes

public struct Int2
{
	public int x;
	public int y;
	public Int2(int x, int y) { this.x = x; this.y = y; }
	public int sqrMagnitude { get { return x * x + y * y; } }
	public long sqrMagnitudeLong { get { return x * (long)x + y * (long)y; } }
	public static Int2 operator +(Int2 a, Int2 b) { return new Int2(a.x + b.x, a.y + b.y); }
	public static Int2 operator -(Int2 a, Int2 b) { return new Int2(a.x - b.x, a.y - b.y); }
	public static bool operator ==(Int2 a, Int2 b) { return a.x == b.x && a.y == b.y; }
	public static bool operator !=(Int2 a, Int2 b) { return a.x != b.x || a.y != b.y; }
	public static int Dot(Int2 a, Int2 b) { return a.x * b.x + a.y * b.y; }
	public static long DotLong(Int2 a, Int2 b) { return a.x * (long)b.x + a.y * (long)b.y; }
	public override bool Equals(object o)
	{
		if (o == null) return false;
		Int2 rhs = (Int2)o;
		return x == rhs.x && y == rhs.y;
	}
	public override int GetHashCode() { return x * 49157 + y * 98317; }
}

public class TileSide
{
	public bool noCheck;
	public List<bool> tilePattern;

	public TileSide(bool dontCare, List<bool> pattern)
	{
		noCheck = dontCare;
		tilePattern = pattern;
	}

	public string patternString()
	{
		if (noCheck) return "X";

		string pattern = "";
		foreach (bool connection in tilePattern)
		{
			if (connection)
				pattern += "1";
			else
				pattern += "0";
		}
		return pattern;
	}
}

public class TileData
{
	public int type;
	public List<TileSide> border;
	public Transform prefab;

	public TileData(int tileType, List<bool> left, List<bool> top, List<bool> right, List<bool> down)
	{
		type = tileType;
		border = new List<TileSide>(4);
		border.Add(new TileSide(false, left));
		border.Add(new TileSide(false, top));
		border.Add(new TileSide(false, right));
		border.Add(new TileSide(false, down));
	}

	public TileData(int tileType, List<bool> left, List<bool> top, List<bool> right, List<bool> down, Transform tilePrefab)
	{
		type = tileType;
		border = new List<TileSide>(4);
		border.Add(new TileSide(false, left));
		border.Add(new TileSide(false, top));
		border.Add(new TileSide(false, right));
		border.Add(new TileSide(false, down));
		prefab = tilePrefab;
	}
}

public class TilePrefab
{
	public GameObject prefab;
	public TileData tileData;
	public bool used;
}

[Serializable] public class Movement
{
	public float moveV;
	public float moveH;
	public bool isRunning;
	public bool isLooking;
}

public class VisibleTile
{
	public TilePrefab tilePrefab;
	public int rotation;

	public VisibleTile()
	{
		tilePrefab = new TilePrefab();
	}

	public void Instantiate(Int2 index, TilePrefab newprefab, float tileSize, Transform cityObject)
	{
		try
		{
			tilePrefab = newprefab;
			tilePrefab.prefab = UnityEngine.Object.Instantiate(newprefab.prefab, new Vector3(index.y * tileSize, 0, index.x * tileSize), Quaternion.Euler(0, 90 * rotation, 0)) as GameObject;
			tilePrefab.prefab.transform.parent = cityObject;
		}
		catch (Exception e)
		{
			Debug.Log(e.Message);
		}
	}
}

#endregion

public class Maze : MonoBehaviour
{
	public TextMeshProUGUI seedText;
	[Tooltip("Set to -1 if you want a random seed")] public int randomSeed = -1;
	[Range(0, 16)] public int radius = 8;
	public float tileSize = 64;
	public int tileConnections = 1;
	[HideInInspector] public bool isBuilding;

	public Dictionary<Int2, VisibleTile> visibleTiles;
	[HideInInspector] public GameObject cityObject;

	[Range(0, 100)] public int roundaboutDensity = 50;

	public TextMeshProUGUI loadingLabel;

	[Header("Default")]
	public Transform tileList;
	[HideInInspector] public List<TileData> baseContainer;

	public bool buildImmediate;
	Vector3 moveToPos;

	protected void InstallPack(Transform pack, ref List<TileData> tileDataList)
	{
		tileDataList = new List<TileData>();
		int type = 0;

		foreach (Transform obj in pack)
		{
			Border other = obj.GetComponent<Border>();
			TileData tileData = new TileData(type, other.left.values, other.top.values, other.right.values, other.down.values, obj);
			tileDataList.Add(tileData);
			++type;
		}
	}

	public virtual void PrepareTilePrefabs()
	{
		moveToPos = transform.position;
		cityObject = new GameObject();
		cityObject.transform.position = Vector3.zero;
		cityObject.name = transform.name;

		InstallPack(tileList, ref baseContainer);
	}

	List<Coroutine> coroutineList;

	public void generateInitialWorld()
	{
		//diagonal
		Int2 index = new Int2(0, 0);
		for (int i = -radius; i <= radius; ++i)
		{
			index.x = i;
			index.y = i;
			AddWorldTile(index, GenerateTile(null, null, null, null));
		}

		//top triangle
		Int2 indexR = new Int2(0, 0);
		Int2 indexD = new Int2(0, 0);
		for (int col = radius - 1; col >= -radius; --col)
		{
			for (int row = col + 1; row <= radius; ++row)
			{
				index.y = col;
				index.x = row;
				indexR.y = col + 1;
				indexR.x = row;
				indexD.y = col;
				indexD.x = row - 1;
				AddWorldTile(index, GenerateTile(null, visibleTiles[indexD], null, visibleTiles[indexR]));
			}
		}

		//down triangle
		Int2 indexT = new Int2(0, 0);
		Int2 indexL = new Int2(0, 0);
		for (int col = -radius + 1; col <= radius; ++col)
		{
			for (int row = col - 1; row >= -radius; --row)
			{
				index.y = col;
				index.x = row;
				indexL.y = col - 1;
				indexL.x = row;
				indexT.y = col;
				indexT.x = row + 1;
				AddWorldTile(index, GenerateTile(visibleTiles[indexT], null, visibleTiles[indexL], null));
			}
		}
	}

	private void AddWorldTile(Int2 gridPosition, VisibleTile tile)
	{
		visibleTiles.Add(gridPosition, tile);
		tile.Instantiate(gridPosition, tile.tilePrefab, tileSize, cityObject.transform);
	}

	private void OnGUI()
	{
		if (Time.frameCount % 30 == 0)
		{
			loadingLabel.text += ".";
			LayoutRebuilder.ForceRebuildLayoutImmediate(loadingLabel.rectTransform);
			Canvas.ForceUpdateCanvases();
		}
	}

	private TileSide ExtractSide(VisibleTile tile, int offset)
	{
		int index = (4 - (tile.rotation - offset)) % 4;
		List<bool> searchPattern = new List<bool>(tile.tilePrefab.tileData.border[index].tilePattern);
		searchPattern.Reverse();
		TileSide side = new TileSide(false, searchPattern);
		return side;
	}

	private VisibleTile GenerateTile(VisibleTile topTile, VisibleTile downTile, VisibleTile leftTile, VisibleTile rightTile)
	{
		TileSide valuesTop = new TileSide(true, null);
		TileSide valuesDown = new TileSide(true, null);
		TileSide valuesRight = new TileSide(true, null);
		TileSide valuesLeft = new TileSide(true, null);

		if (topTile != null) valuesTop = ExtractSide(topTile, 3);
		if (downTile != null) valuesDown = ExtractSide(downTile, 1);
		if (leftTile != null) valuesLeft = ExtractSide(leftTile, 2);
		if (rightTile != null) valuesRight = ExtractSide(rightTile, 0);

		return MatchTile(valuesLeft, valuesTop, valuesRight, valuesDown);
	}

	private bool EqualSide(List<bool> bits1, List<bool> bits2)
	{
		for (int i = 0; i < tileConnections; ++i)
		{
			if (bits1[i] != bits2[i]) return false;
		}
		return true;
	}

	public bool Match(TileData tileData, List<TileSide> matchSide, ref int rotation)
	{
		rotation = 0;
		int startIndexA = 0;
		TileSide side = matchSide[startIndexA];

		while (matchSide.Count > startIndexA)
		{
			side = matchSide[startIndexA];
			if (!side.noCheck) break;
			++startIndexA;
		}

		if (matchSide.Count > startIndexA)
		{
			int startIndexB;
			for (startIndexB = 0; startIndexB < 4; ++startIndexB)
			{
				if (EqualSide(tileData.border[startIndexB].tilePattern, side.tilePattern))
				{
					// first side match, check the rest of the border
					int rest;
					for (rest = 1; rest < 4; ++rest)
					{
						TileSide mathTileSide = matchSide[(startIndexA + rest) % 4];
						TileSide tileSide = tileData.border[(startIndexB + rest) % 4];
						if (!mathTileSide.noCheck && !EqualSide(tileSide.tilePattern, mathTileSide.tilePattern)) break;
					}

					if (rest == 4)
					{
						//valid pattern, calculates applied rotation
						rotation = (4 - (startIndexB - startIndexA)) % 4;
						return true;
					}
				}
			}
			return false;
		}
		else
		{
			//all rotation is valid, generates one randomly
			rotation = UnityEngine.Random.Range(0, 4);
			return true;
		}
	}

	public virtual VisibleTile MatchTile(TileSide valuesLeft, TileSide valuesTop, TileSide valuesRight, TileSide valuesDown)
	{
		VisibleTile tile = null;
		List<VisibleTile> tilesFound = new List<VisibleTile>();
		List<TileSide> matchSide = new List<TileSide>
		{
			valuesLeft,
			valuesTop,
			valuesRight,
			valuesDown
		};
		int rotation = 0;
		bool foundRoundabout = false;
		bool foundCrossroad = false;
		int index;

		for (int i = 0; i < baseContainer.Count; i++)
		{
			TileData tileData = baseContainer[i];
			if (Match(tileData, matchSide, ref rotation))
			{
				var t = new VisibleTile();
				t.tilePrefab.prefab = tileData.prefab.gameObject;
				t.tilePrefab.tileData = tileData;
				t.rotation = rotation;
				tilesFound.Add(t);
			}
		}

		// Remove roundabouts by percentage change
		int dieRoll = UnityEngine.Random.Range(0, 101);
		if (dieRoll >= roundaboutDensity)
		{
			index = 0;
			foreach (var tl in tilesFound)
			{
				if (tl.tilePrefab.tileData.prefab.name.Contains("Roundabout"))
				{
					foundRoundabout = true; break;
				}
				index++;
			}
			if (foundRoundabout) tilesFound.RemoveAt(index);
		}
		else
		{
			index = 0;
			foreach (var tl in tilesFound)
			{
				if (tl.tilePrefab.tileData.prefab.name.Contains("Crossroad"))
				{
					foundCrossroad = true; break;
				}
				index++;
			}
			if (foundCrossroad) tilesFound.RemoveAt(index);
		}

		if (tilesFound.Count > 0)
		{
			dieRoll = UnityEngine.Random.Range(0, tilesFound.Count);
			VisibleTile tle = null;
			tle = tilesFound[dieRoll];
			tile = tle;
			tile.tilePrefab.prefab = tle.tilePrefab.prefab;
			tile.tilePrefab.tileData = tle.tilePrefab.tileData;
			tile.rotation = tle.rotation;
		}

		if (tile == null) Debug.Log("<color=red>Fatal error:</color> Missing tile with pattern: " + valuesLeft.patternString() + " " + valuesTop.patternString() + " " + valuesRight.patternString() + " " + valuesDown.patternString());

		return tile;
	}

	void UpdateSeed()
	{
		if (randomSeed < 0) randomSeed = UnityEngine.Random.Range(0, int.MaxValue);
		if (seedText != null) seedText.text = "DIMENSION SEED: " + randomSeed;
		UnityEngine.Random.InitState(randomSeed);
	}

	public class MazeJob : JobItem
	{
		protected override void DoWork()
		{
			;
		}
		public override void OnFinished()
		{
			;
		}
	}

	JobQueue<MazeJob> queue;

	public void InitMap()
	{
		/*
		queue = new JobQueue<MazeJob>(1); // create new queue with 2 threads
		queue.AddJob(new MazeJob()); // queue.AddJob(new MazeJob { count = 200, CustomName = "200 iterations" });
		*/
		loadingLabel.text = "";
		LayoutRebuilder.ForceRebuildLayoutImmediate(loadingLabel.rectTransform);
		Canvas.ForceUpdateCanvases();

		isBuilding = true;
		visibleTiles = new Dictionary<Int2, VisibleTile>();

		UpdateSeed();
		PrepareTilePrefabs();
		generateInitialWorld();

		cityObject.transform.position = moveToPos;
	}

	private void Start()
	{
		if (buildImmediate) InitMap();
	}

	void Update()
	{
		//queue.Update();

		if (Input.GetKeyDown(KeyCode.Home))
		{
			Time.timeScale = 0;
			UnityEngine.Object.Destroy(cityObject);
			InitMap();
			Time.timeScale = 1;
		}
		else if (Input.GetKeyDown(KeyCode.End))
		{
			Time.timeScale = 1;
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
	}

	/*
	void OnDisable()
	{
		queue.ShutdownQueue();
	}
	*/
}
