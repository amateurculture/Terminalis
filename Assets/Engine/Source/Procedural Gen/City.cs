using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class City : Maze
{
	#region Public Variables

	[Header("Agrarian")]
	public Transform primitiveFarm;
	public Transform primitiveResidential;

	[Header("Medieval")]
	public Transform medievalResidential;
	public Transform medievalCommercial;
	public Transform medievalChurch; // re-education camp

	[Header("Imperial")]
	public Transform imperialRoad;
	public Transform imperialResidential;
	public Transform imperialCommercial;
	public Transform imperialIndustrial;
	public Transform imperialFire; // looking at you Crassus (didn't it used to be Cassius? also what about Encladius and Baranstein?)
	public Transform imperialChurch;

	[Header("Modernist")]
	public Transform road;
	public Transform farm;
	public Transform residential;
	public Transform commercial;
	public Transform industrial;
	public Transform hospital;
	public Transform school;
	public Transform university; // modern church of science, 2.0
	public Transform police;
	public Transform fire;
	public Transform museum;

	[Header("Dystopian")] // memoires found in a bathtub world, also includes gataca, fascist states, North Korea, et al.
	public Transform disinformationCenter; // alt-news

	[Header("Utopian")] // doesn't exist, included here as a joke
	public Transform immortalityCenter; // the cost of researching this would basically bankrupt the planet
	
	[Header("Tile Sets")]
	public Globals.Era era = Globals.Era.Primitive;

	[Range(0, 100)] public int wildDensity = 20;
	[Range(0, 100)] public int farmDensity = 20;
	[Range(0, 100)] public int residentialDensity = 20;
	[Range(0, 100)] public int commercialDensity = 20;
	[Range(0, 100)] public int industrialDensity = 20;

	public bool allowDeadEnds = false;

	[HideInInspector] [Tooltip("Each person on the screen represents 100 people")] public long population;
	[HideInInspector] [Tooltip("Credits -- generic unit of money")] public float gdp;
	[HideInInspector] [Tooltip("Bushel -- One bushel feeds 100 people")] public float food;
	[HideInInspector] [Tooltip("AQI Index -- 0-50 Good : 51-100 Moderate : 101-150 Sensitive : 151-200 Unhealthy : 201-300 Very Unhealthy : 301+ Hazardous")] [Range(0, 500)] public float pollution;

	[Header("Economy")]
	public float taxRate = .05f;
	public float wealth = 1000000f;

	#endregion

	#region Private Variables

	private List<TileData> primitiveFarmContainer;
	private List<TileData> primitiveResidentialContainer;
	private List<TileData> medievalResidentialContainer;
	private List<TileData> medievalCommercialContainer;
	private List<TileData> farmContainer;
	private List<TileData> roadContainer;
	private List<TileData> residentialContainer;
	private List<TileData> commercialContainer;
	private List<TileData> industrialContainer;

	#endregion

	#region Aggregators

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
		return population;
	}

	public int AggregatePollution()
	{
		if (cityObject == null) return -1;

		int pollution = 0;
		Tile[] tiles = cityObject.GetComponentsInChildren<Tile>();
		foreach (var tile in tiles) pollution += tile.pollution;
		return (tiles.Length > 0) ? pollution / tiles.Length : 0;
	}

	#endregion

	public override void PrepareTilePrefabs()
	{
		base.PrepareTilePrefabs();

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

	public override VisibleTile MatchTile(TileSide valuesLeft, TileSide valuesTop, TileSide valuesRight, TileSide valuesDown)
	{
		VisibleTile tile = null;
		List<TileData> tileContainer = new List<TileData>();
		int dieRoll = UnityEngine.Random.Range(0, 101);

		if (dieRoll < wildDensity)
		{
			switch (era)
			{
				case Globals.Era.Wild:
				case Globals.Era.Primitive:
				case Globals.Era.Medieval: tileContainer = baseContainer;  break;
				case Globals.Era.Modern: tileContainer = roadContainer; break;
				default: break;
			}
		}
		else
		{
			switch (era)
			{
				case Globals.Era.Primitive:
					dieRoll = UnityEngine.Random.Range(0, 2);
					switch (dieRoll)
					{
						case 0: tileContainer = primitiveFarmContainer; break;
						case 1: tileContainer = primitiveResidentialContainer; break;
					}
					break;

				case Globals.Era.Medieval:
					dieRoll = UnityEngine.Random.Range(0, 3);
					switch (dieRoll)
					{
						case 0: tileContainer = primitiveFarmContainer; break;
						case 1: tileContainer = medievalResidentialContainer; break;
						case 2: tileContainer = medievalCommercialContainer; break;
					}
					break;

				case Globals.Era.Modern:
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

				default:
					tileContainer = baseContainer;
					break;
			}
		}

		List<VisibleTile> tilesFound = new List<VisibleTile>();
		List<TileSide> matchSide = new List<TileSide>();
		matchSide.Add(valuesLeft);
		matchSide.Add(valuesTop);
		matchSide.Add(valuesRight);
		matchSide.Add(valuesDown);
		int rotation = 0;

		for (int i = 0; i < tileContainer.Count; i++)
		{
			TileData tileData = tileContainer[i];
			if (Match(tileData, matchSide, ref rotation))
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
			//Debug.Log("Chose index " + dieRoll + " -- Range(0, " + (tilesFound.Count - 1) + ")");

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
}
