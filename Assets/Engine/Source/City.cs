using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using System;

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

[Serializable]
public class Movement
{
	public float moveV;
	public float moveH;
	public bool isRunning;
	public bool isLooking;
}

public class City : MonoBehaviour
{
	public TextMeshProUGUI seedText;
	[Tooltip("Set to -1 if you want a random seed")] public int randomSeed = -1;
	[Range(0,16)] public int radius = 8;
	public float tileSize = 64;
	public int tileConnections = 1;
	public bool allowDeadEnds = false;

	[HideInInspector] [Tooltip("Each person on the screen represents 100 people")] public long population;
	[HideInInspector] [Tooltip("Credits -- generic unit of money")] public float gdp;
	[HideInInspector] [Tooltip("Bushel -- One bushel feeds 100 people")] public float food;
	[HideInInspector] [Tooltip("AQI Index -- 0-50 Good : 51-100 Moderate : 101-150 Sensitive : 151-200 Unhealthy : 201-300 Very Unhealthy : 301+ Hazardous")] [Range(0,500)] public float pollution;

	[Header("Economy")]
	public float taxRate = .05f;
	public float wealth = 1000000f;

	public enum Era
	{
		Wild = 1 << 0,
		Primitive = 1 << 1,
		Medieval = 1 << 2,
		//Baroque = 1 << 3,
		Modern = 1 << 4,
		//Future = 1 << 5
	}

	[Header("Tile Sets")]
	public Era era = Era.Primitive;

	/*
	public enum EconomicClass
	{
		low,
		medium,
		high
	}
	public EconomicClass economicClass = EconomicClass.medium;
	*/

	[Range(0, 100)] public int roundaboutDensity = 50;
	[Range(0, 100)] public int wildDensity = 50;
	[Range(0, 100)] public int farmDensity = 20;
	[Range(0, 100)] public int residentialDensity = 20;
	[Range(0, 100)] public int commercialDensity = 20;
	[Range(0, 100)] public int industrialDensity = 20;

	[Header("Wild")]
	public Transform wild;

	[Header("Primitive")]
	public Transform primitiveFarm;
	public Transform primitiveResidential;

	[Header("Medieval")]
	public Transform medievalResidential;
	public Transform medievalCommercial;

	/*
	[Header("Baroque")]
	public Transform baroqueRoad;
	public Transform baroqueResidential;
	public Transform baroqueCommercial;
	public Transform barqoueIndustrial;
	*/

	[Header("Modern")]
	public Transform road;
	public Transform farm;
	public Transform residential;
	public Transform commercial;
	public Transform industrial;

	private List<TileData> wildContainer;
	private List<TileData> primitiveFarmContainer;
	private List<TileData> primitiveResidentialContainer;
	private List<TileData> medievalResidentialContainer;
	private List<TileData> medievalCommercialContainer;
	private List<TileData> farmContainer;
	private List<TileData> roadContainer;
	private List<TileData> residentialContainer;
	private List<TileData> commercialContainer;
	private List<TileData> industrialContainer;

	private Dictionary<Int2, VisibleTile> visibleTiles;
	static private GameObject cityObject;

	public int AggregateFood()
	{
		if (cityObject == null) return -1;

		int food = 0;
		Tile[] tiles = cityObject.GetComponentsInChildren<Tile>();
		foreach (var tile in tiles) food += tile.foodProduction;
		return food;
	}

	public int AggregateWidgets()
	{
		if (cityObject == null) return -1;

		int widgets = 0;
		Tile[] tiles = cityObject.GetComponentsInChildren<Tile>();
		foreach (var tile in tiles) widgets += tile.widgetProduction;
		return widgets;
	}

	public float AggregateRevenue()
	{
		if (cityObject == null) return -1;

		float wealth = 0;
		Tile[] tiles = cityObject.GetComponentsInChildren<Tile>();
		foreach (var tile in tiles) wealth += (tile.widgetProduction * tile.widgetPrice) + (tile.foodProduction * tile.foodPrice);
		return wealth;
	}

	public int AggregatePopulation()
	{
		if (cityObject == null) return -1;

		int population = 0;
		Tile[] tiles = cityObject.GetComponentsInChildren<Tile>();
		foreach(var tile in tiles) population += tile.population;
		return population * 500;
	}

	public int AggregatePollution()
	{
		if (cityObject == null) return -1;

		int pollution = 0;
		Tile[] tiles = cityObject.GetComponentsInChildren<Tile>();
		foreach (var tile in tiles) pollution += tile.pollution;
		return (tiles.Length > 0) ? pollution / tiles.Length : 0;
	}

	private void Reset()
	{
		
	}

	private void InstallPack(Transform pack, ref List<TileData> tileDataList) {
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

	private void prepareTilePrefabs()
	{
		cityObject = new GameObject();
		cityObject.transform.position = Vector3.zero;
		cityObject.name = transform.name;

		InstallPack(wild, ref wildContainer);
		InstallPack(primitiveFarm, ref primitiveFarmContainer);
		InstallPack(primitiveResidential, ref primitiveResidentialContainer);
		InstallPack(medievalResidential, ref medievalResidentialContainer);
		InstallPack(medievalCommercial, ref medievalCommercialContainer);
		InstallPack(road, ref roadContainer);
		InstallPack(farm, ref farmContainer);
		InstallPack(residential, ref residentialContainer);
		InstallPack(commercial, ref commercialContainer);
		InstallPack(industrial, ref industrialContainer);
	}

	class VisibleTile
	{
		public TilePrefab tilePrefab;
		public int rotation;

		public VisibleTile()
		{
			tilePrefab = new TilePrefab();
		}

		public void Instantiate(Int2 index, TilePrefab newprefab, float tileSize)
		{
			try
			{
				tilePrefab = newprefab;
				tilePrefab.prefab = UnityEngine.Object.Instantiate(newprefab.prefab, new Vector3(index.y * tileSize, 0, index.x * tileSize), Quaternion.Euler(0, 90 * rotation, 0)) as GameObject;
				tilePrefab.prefab.transform.parent = cityObject.transform;
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
			}
		}
	}

	public void generateInitialWorld()
	{
		//diagonal
		Int2 index = new Int2(0, 0);
		for (int i = -radius; i <= radius; ++i)
		{
			index.x = i;
			index.y = i;
			addWorldTile(index, generateTile(null, null, null, null));
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
				addWorldTile(index, generateTile(null, visibleTiles[indexD], null, visibleTiles[indexR]));
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
				addWorldTile(index, generateTile(visibleTiles[indexT], null, visibleTiles[indexL], null));
			}
		}
	}

	private void addWorldTile(Int2 gridPosition, VisibleTile tile)
	{
		visibleTiles.Add(gridPosition, tile);
		tile.Instantiate(gridPosition, tile.tilePrefab, tileSize);
	}

	private TileSide extractSide(VisibleTile tile, int offset)
	{
		int index = (4 - (tile.rotation - offset)) % 4;
		List<bool> searchPattern = new List<bool>(tile.tilePrefab.tileData.border[index].tilePattern);
		searchPattern.Reverse();
		TileSide side = new TileSide(false, searchPattern);
		return side;
	}

	private VisibleTile generateTile(VisibleTile topTile, VisibleTile downTile, VisibleTile leftTile, VisibleTile rightTile)
	{
		TileSide valuesTop = new TileSide(true, null);
		TileSide valuesDown = new TileSide(true, null);
		TileSide valuesRight = new TileSide(true, null);
		TileSide valuesLeft = new TileSide(true, null);

		if (topTile != null) valuesTop = extractSide(topTile, 3);
		if (downTile != null) valuesDown = extractSide(downTile, 1);
		if (leftTile != null) valuesLeft = extractSide(leftTile, 2);
		if (rightTile != null) valuesRight = extractSide(rightTile, 0);

		return matchTile(valuesLeft, valuesTop, valuesRight, valuesDown);
	}

	private bool equalSide(List<bool> bits1, List<bool> bits2)
	{
		for (int i = 0; i < tileConnections; ++i)
		{
			if (bits1[i] != bits2[i]) return false;
		}
		return true;
	}

	private bool match(TileData tileData, List<TileSide> matchSide, ref int rotation)
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
				if (equalSide(tileData.border[startIndexB].tilePattern, side.tilePattern))
				{
					// first side match, check the rest of the border
					int rest;
					for (rest = 1; rest < 4; ++rest)
					{
						TileSide mathTileSide = matchSide[(startIndexA + rest) % 4];
						TileSide tileSide = tileData.border[(startIndexB + rest) % 4];
						if (!mathTileSide.noCheck)
						{
							if (!equalSide(tileSide.tilePattern, mathTileSide.tilePattern)) break;
						}
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

	private VisibleTile matchTile(TileSide valuesLeft, TileSide valuesTop, TileSide valuesRight, TileSide valuesDown)
	{
		VisibleTile tile = null;
		List<VisibleTile> tilesFound = new List<VisibleTile>();
		List<TileData> tileContainer = new List<TileData>();
		List<String> tileContainerNames = new List<String>();

		int dieRoll = UnityEngine.Random.Range(0, 101);
		if (dieRoll < wildDensity)
		{
			switch (era)
			{
				case Era.Wild:
				case Era.Primitive:
				case Era.Medieval: tileContainer = wildContainer;  break;
				case Era.Modern: tileContainer = roadContainer; break;
				default: break;
			}
		}
		else
		{
			switch (era)
			{
				case Era.Wild:
					tileContainer = wildContainer;
					break;

				case Era.Primitive:
					dieRoll = UnityEngine.Random.Range(0, 2);
					switch (dieRoll)
					{
						case 0: tileContainer = primitiveFarmContainer; break;
						case 1: tileContainer = primitiveResidentialContainer; break;
					}
					break;

				case Era.Medieval:
					dieRoll = UnityEngine.Random.Range(0, 3);
					switch (dieRoll)
					{
						case 0: tileContainer = primitiveFarmContainer; break;
						case 1: tileContainer = medievalResidentialContainer; break;
						case 2: tileContainer = medievalCommercialContainer; break;
					}
					break;

				case Era.Modern:
					if (UnityEngine.Random.Range(0, 101) < residentialDensity) { tileContainer = residentialContainer;}
					else if (UnityEngine.Random.Range(0, 101) < commercialDensity) { tileContainer = commercialContainer;}
					else if (UnityEngine.Random.Range(0, 101) < industrialDensity) { tileContainer = industrialContainer;}
					else if (UnityEngine.Random.Range(0, 101) < farmDensity) { tileContainer = farmContainer;}
					else
					{
						if (farmDensity > residentialDensity && farmDensity > industrialDensity && farmDensity > commercialDensity) tileContainer = farmContainer;
						else if (commercialDensity > residentialDensity && commercialDensity > industrialDensity) tileContainer = commercialContainer;
						else if (industrialDensity > residentialDensity) tileContainer = commercialContainer;
						else
							tileContainer = residentialContainer;
					}
					break;

				default: break;
			}
		}

		tilesFound = new List<VisibleTile>();
		List<TileSide> matchSide = new List<TileSide>();
		matchSide.Add(valuesLeft);
		matchSide.Add(valuesTop);
		matchSide.Add(valuesRight);
		matchSide.Add(valuesDown);
		int rotation = 0;

		for (int i = 0; i < tileContainer.Count; i++)
		{
			TileData tileData = tileContainer[i];
			if (match(tileData, matchSide, ref rotation))
			{
				var t = new VisibleTile();
				t.tilePrefab.prefab = tileData.prefab.gameObject;
				t.tilePrefab.tileData = tileData;
				t.rotation = rotation;
				tilesFound.Add(t);
			}
		}
		
		int index = 0; 
		bool foundCorner = false;
		bool foundCuldesac = false;
		bool foundRoundabout = false;
		bool foundCrossroad = false;
		VisibleTile cornerTile = new VisibleTile();

		// Strip culdesacs and corners to make more city like maps
		if (!allowDeadEnds)
		{
			foreach (var tl in tilesFound)
			{
				if (tl.tilePrefab.tileData.prefab.name.Contains("Corner"))
				{
					foundCorner = true;
					cornerTile = tl;
					cornerTile.tilePrefab.prefab = tl.tilePrefab.prefab;
					cornerTile.tilePrefab.tileData = tl.tilePrefab.tileData;
					cornerTile.rotation = tl.rotation;
					break;
				}
				index++;
			}
			if (foundCorner) tilesFound.RemoveAt(index);

			index = 0;
			foreach (var tl in tilesFound)
			{
				if (tl.tilePrefab.tileData.prefab.name.Contains("Culdesac"))
				{
					foundCuldesac = true; break;
				}
				index++;
			}
			if (foundCuldesac) tilesFound.RemoveAt(index);
		}

		// Remove roundabouts by percentage change
		dieRoll = UnityEngine.Random.Range(0, 101);
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
			Debug.Log("Chose index " + dieRoll + " -- Range(0, " + (tilesFound.Count - 1) + ")");

			VisibleTile tle = null;
			if (tilesFound.Count > 0) tle = tilesFound[dieRoll];

			tile = tle;
			tile.tilePrefab.prefab = tle.tilePrefab.prefab;
			tile.tilePrefab.tileData = tle.tilePrefab.tileData;
			tile.rotation = tle.rotation;
		}
		else if (foundCorner)
		{
			tile = cornerTile;
			tile.tilePrefab.prefab = cornerTile.tilePrefab.prefab;
			tile.tilePrefab.tileData = cornerTile.tilePrefab.tileData;
			tile.rotation = cornerTile.rotation;
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

	public void InitMap()
	{
		UpdateSeed();
		visibleTiles = new Dictionary<Int2, VisibleTile>();
		prepareTilePrefabs();
		generateInitialWorld();
	}

	void Start()
	{
		InitMap();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Home))
		{
			Time.timeScale = 0f;
			Destroy(cityObject);
			InitMap();
			Time.timeScale = 1f;
		}
		else if (Input.GetKeyDown(KeyCode.End))
		{
			Time.timeScale = 1;
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
	}
}
