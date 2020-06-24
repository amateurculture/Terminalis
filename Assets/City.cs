using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class City : MonoBehaviour
{
	public TextMeshProUGUI seedText;
	[Tooltip("Set to -1 if you want a random seed")] public int randomSeed = -1;
	[Range(0,16)] public int radius = 6;
	public float tileSize = 64;
	public int tileConnections = 1;

	public enum Era
	{
		Wild = 1 << 0,
		Primitive = 1 << 1,
		//Medieval = 1 << 2,
		//Baroque = 1 << 3,
		Modern = 1 << 4,
		//Future = 1 << 5
	}
	public Era era = Era.Primitive;

	[Range(0, 100)] public int cityDensity;

	[Header("Wild")]
	public Transform wild;

	[Header("Primitive")]
	public Transform primitiveFarm;

	/*
	[Header("Medieval")]
	public Transform medievalResidential;
	public Transform medievalCommercial;

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

	[HideInInspector] public Transform player;
	private Vector3 poolObjectPosition = new Vector3(10000f, 10000f, 10000f);
	private List<TileData> tileContainer;
	private Dictionary<int, List<TilePrefab>> tilesPool;
	private Int2 previousPosition;
	private Dictionary<Int2, VisibleTile> visibleTiles;
	static private GameObject poolObject;
	static private GameObject cityObject;

	private void prepareTilePrefabs()
	{
		tileContainer = new List<TileData>();
		tilesPool = new Dictionary<int, List<TilePrefab>>();
		poolObject = new GameObject();
		poolObject.transform.position = poolObjectPosition;
		poolObject.name = "poolObject";
		cityObject = new GameObject();
		cityObject.transform.position = Vector3.zero;
		cityObject.name = transform.name;

		int type = 0;
		int maxVisibleTiles = (radius+1) * (radius+1); // ((radius * 2) + 1) * ((radius * 2) + 2);

		List<Transform> cityTiles = new List<Transform>();
		if (cityDensity == 0)
		{
			switch (era) 
			{ 
				case Era.Wild: 
				case Era.Primitive: 
					foreach (Transform obj in wild) cityTiles.Add(obj); 
					break;
				case Era.Modern: 
					foreach (Transform obj in road) cityTiles.Add(obj); 
					break;
				default: break;
			}
		} 
		else
		{
			switch (era)
			{
				case Era.Wild:
					foreach (Transform obj in wild) cityTiles.Add(obj);
					break;
				case Era.Primitive:
					foreach (Transform obj in wild) cityTiles.Add(obj);
					foreach (Transform obj in primitiveFarm) cityTiles.Add(obj); 
					break;
				case Era.Modern: 
					foreach (Transform obj in road) cityTiles.Add(obj);
					foreach (Transform obj in farm) cityTiles.Add(obj);
					foreach (Transform obj in residential) cityTiles.Add(obj);
					foreach (Transform obj in commercial) cityTiles.Add(obj);
					foreach (Transform obj in industrial) cityTiles.Add(obj);
					break;
				default: break;
			}
		}
		
		foreach (Transform obj in cityTiles)
		{
			Border other = obj.GetComponent<Border>();
			TileData tileData = new TileData(type, other.left.values, other.top.values, other.right.values, other.down.values);
			tileContainer.Add(tileData);
			List<TilePrefab> tileList = new List<TilePrefab>();

			for (int count = 0; count < maxVisibleTiles; ++count)
			{
				TilePrefab tilePrefab = new TilePrefab();
				tilePrefab.tileData = new TileData(type, other.left.values, other.top.values, other.right.values, other.down.values);
				Vector3 position = new Vector3(0, 0, 0);
				GameObject objInstance = Instantiate(obj.gameObject, position, Quaternion.Euler(0, 0, 0)) as GameObject;
				objInstance.transform.parent = poolObject.transform;
				objInstance.transform.localPosition = new Vector3(0, 0, 0);
				tilePrefab.prefab = objInstance;
				tilePrefab.used = false;
				tileList.Add(tilePrefab);
			}
			tilesPool.Add(type, tileList);
			++type;
		}
	}

	class VisibleTile
	{
		public TilePrefab tilePrefab;
		public int rotation;

		/** Default constructor */
		public VisibleTile()
		{
			tilePrefab = new TilePrefab();
		}

		public void Insantiate(Int2 index, TilePrefab newPrefab, float tileSize)
		{

		}

		public void Instantiate(Int2 index, TilePrefab newprefab, float tileSize)
		{
			tilePrefab = newprefab;
			tilePrefab.prefab.transform.parent = cityObject.transform;
			tilePrefab.prefab.transform.position = new Vector3(index.y * tileSize, 0, index.x * tileSize);
			tilePrefab.prefab.transform.rotation = Quaternion.Euler(0, 90 * rotation, 0);
			tilePrefab.used = true;
		}

		/** Remove tile from visible world grid and update the poolObject */
		public void Destroy()
		{
			tilePrefab.prefab.transform.parent = poolObject.transform;
			tilePrefab.prefab.transform.localPosition = new Vector3(0, 0, 0);
			tilePrefab.prefab.transform.rotation = Quaternion.Euler(0, 0, 0);
			tilePrefab.used = false;
			tilePrefab = null;
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

	private void addWorldTile(Int2 index, VisibleTile tile)
	{
		visibleTiles.Add(index, tile);

		if (tilesPool != null && tilesPool.Count > 0)
		{
			List<TilePrefab> tileInstances = tilesPool[tile.tilePrefab.tileData.type];
			foreach (TilePrefab tileprefab in tileInstances)
			{
				if (tileprefab.used == false)
				{
					tile.Instantiate(index, tileprefab, tileSize);
					break;
				}
			}
		}
	}

	private void removeWorldTile(Int2 index)
	{
		VisibleTile tile = visibleTiles[index];
		tile.Destroy();
		visibleTiles.Remove(index);
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
			if (bits1[i] != bits2[i])
				return false;
		}
		return true;
	}

	private bool match(TileData tileData, List<TileSide> matchSide, ref int rotation)
	{
		rotation = 0;

		int startIndexA = 0;
		int startIndexB = 0;

		TileSide side = matchSide[startIndexA];
		while (matchSide.Count > startIndexA)
		{
			side = matchSide[startIndexA];
			if (!side.noCheck)
			{
				break;
			}
			++startIndexA;
		}

		if (matchSide.Count > startIndexA)
		{
			for (startIndexB = 0; startIndexB < 4; ++startIndexB)
			{
				if (equalSide(tileData.border[startIndexB].tilePattern, side.tilePattern))
				{
					// first side match, check the rest of the border
					int rest = 1;
					for (rest = 1; rest < 4; ++rest)
					{
						TileSide mathTileSide = matchSide[(startIndexA + rest) % 4];
						TileSide tileSide = tileData.border[(startIndexB + rest) % 4];
						if (!mathTileSide.noCheck)
						{
							if (!equalSide(tileSide.tilePattern, mathTileSide.tilePattern))
							{
								break;
							}
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
			// tile dont match
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
		VisibleTile tile = new VisibleTile();

		//Split container search randomly
		int startSearch = UnityEngine.Random.Range(0, tileContainer.Count);
		List<TileSide> matchSide = new List<TileSide>();
		matchSide.Add(valuesLeft);
		matchSide.Add(valuesTop);
		matchSide.Add(valuesRight);
		matchSide.Add(valuesDown);
		bool found = false;

		int rotation = 0;
		for (int i = startSearch; i < tileContainer.Count; i++)
		{
			TileData tileData = tileContainer[i];
			if (match(tileData, matchSide, ref rotation))
			{
				found = true;
				tile.tilePrefab.tileData = tileData;
				tile.rotation = rotation;
				break;
			}
		}

		//search in the rest of container
		if (!found)
		{
			for (int i = 0; i < startSearch; i++)
			{
				TileData tileData = tileContainer[i];
				if (match(tileData, matchSide, ref rotation))
				{
					found = true;
					tile.tilePrefab.tileData = tileData;
					tile.rotation = rotation;
					break;
				}
			}
		}

		if (!found)
			Debug.Log("<color=red>Fatal error:</color> Missing tile with pattern: " + valuesLeft.patternString() + " " + valuesTop.patternString() + " " + valuesRight.patternString() + " " + valuesDown.patternString());
		return tile;
	}

	Int2 calculatePosition()
	{
		Int2 position = new Int2(0, 0);
		position.x = Mathf.FloorToInt((player.transform.position.z + (tileSize / 2)) / tileSize);
		position.y = Mathf.FloorToInt((player.transform.position.x + (tileSize / 2)) / tileSize);
		return position;
	}

	void generateDown(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0, 0);
		for (int i = -radius; i <= radius; ++i)
		{
			index.x = previousPosition.x + radius;
			index.y = previousPosition.y + i;

			removeWorldTile(index);
		}

		index.x = currentPosition.x - radius;
		index.y = currentPosition.y - radius;
		Int2 indexL = new Int2(0, 0);
		Int2 indexT = new Int2(index.x + 1, index.y);

		addWorldTile(index, generateTile(visibleTiles[indexT], null, null, null));

		for (int i = -radius + 1; i <= radius; ++i)
		{
			index.y = currentPosition.y + i;
			index.x = currentPosition.x - radius;

			indexL.y = index.y - 1;
			indexL.x = index.x;

			indexT.y = index.y;
			indexT.x = index.x + 1;

			addWorldTile(index, generateTile(visibleTiles[indexT], null, visibleTiles[indexL], null));
		}
	}

	void generateTop(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0, 0);
		for (int i = -radius; i <= radius; ++i)
		{
			index.x = previousPosition.x - radius;
			index.y = previousPosition.y + i;

			removeWorldTile(index);
		}

		index.x = currentPosition.x + radius;
		index.y = currentPosition.y - radius;
		Int2 indexL = new Int2(0, 0);
		Int2 indexD = new Int2(index.x - 1, index.y);

		addWorldTile(index, generateTile(null, visibleTiles[indexD], null, null));

		for (int i = -radius + 1; i <= radius; ++i)
		{
			index.y = currentPosition.y + i;
			index.x = currentPosition.x + radius;

			indexL.y = index.y - 1;
			indexL.x = index.x;

			indexD.y = index.y;
			indexD.x = index.x - 1;

			addWorldTile(index, generateTile(null, visibleTiles[indexD], visibleTiles[indexL], null));
		}
	}

	void generateLeft(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0, 0);
		for (int i = -radius; i <= radius; ++i)
		{
			index.x = previousPosition.x + i;
			index.y = previousPosition.y + radius;

			removeWorldTile(index);
		}

		index.x = currentPosition.x - radius;
		index.y = currentPosition.y - radius;
		Int2 indexR = new Int2(index.x, index.y + 1);
		Int2 indexD = new Int2(0, 0);

		addWorldTile(index, generateTile(null, null, null, visibleTiles[indexR]));

		for (int i = -radius + 1; i <= radius; ++i)
		{
			index.y = currentPosition.y - radius;
			index.x = currentPosition.x + i;

			indexR.y = index.y + 1;
			indexR.x = index.x;

			indexD.y = index.y;
			indexD.x = index.x - 1;

			addWorldTile(index, generateTile(null, visibleTiles[indexD], null, visibleTiles[indexR]));
		}
	}

	void generateRight(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0, 0);
		for (int i = -radius; i <= radius; ++i)
		{
			index.x = previousPosition.x + i;
			index.y = previousPosition.y - radius;

			removeWorldTile(index);
		}

		index.x = currentPosition.x - radius;
		index.y = currentPosition.y + radius;
		Int2 indexL = new Int2(index.x, index.y - 1);
		Int2 indexD = new Int2(0, 0);

		addWorldTile(index, generateTile(null, null, visibleTiles[indexL], null));

		for (int i = -radius + 1; i <= radius; ++i)
		{
			index.y = currentPosition.y + radius;
			index.x = currentPosition.x + i;

			indexL.y = index.y - 1;
			indexL.x = index.x;

			indexD.y = index.y;
			indexD.x = index.x - 1;

			addWorldTile(index, generateTile(null, visibleTiles[indexD], visibleTiles[indexL], null));
		}
	}

	public void initMap()
	{
		visibleTiles = new Dictionary<Int2, VisibleTile>();
		previousPosition = new Int2(0, 0);
		prepareTilePrefabs();
		generateInitialWorld();
	}

	public void updateMap()
	{
		Int2 currentPosition = calculatePosition();
		if (previousPosition.x < currentPosition.x) generateTop(previousPosition, currentPosition);
		if (previousPosition.x > currentPosition.x) generateDown(previousPosition, currentPosition);
		if (previousPosition.y > currentPosition.y) generateLeft(previousPosition, currentPosition);
		if (previousPosition.y < currentPosition.y) generateRight(previousPosition, currentPosition);
		previousPosition = currentPosition;
	}

	void Start()
	{
		player = GameObject.FindGameObjectWithTag("Player").transform;
		if (randomSeed < 0) randomSeed = Random.Range(0, int.MaxValue);
		if (seedText != null) seedText.text = "DIMENSION SEED: " + randomSeed;
		Random.InitState(randomSeed);

		initMap();
		Destroy(poolObject);
	}

	void deleteMap()
	{
		Destroy(cityObject);
		Destroy(poolObject);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Home))
		{
			Time.timeScale = 0f;
			deleteMap();
			initMap();
			Destroy(poolObject);
			Time.timeScale = 1f;
		}
		else if (Input.GetKeyDown(KeyCode.End))
		{
			Time.timeScale = 1;
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
	}
}
