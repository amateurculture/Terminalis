using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class World : MonoBehaviour 
{
	public int worldSeed;
	public TextMeshProUGUI worldSeedText;
	[HideInInspector] public Transform player;
	public GameObject[] prefabs;
	private List<TileData> tileContainer;
	public int initialrange = 6;
	public float tileSize = 64;
	public int tileConnections = 1;
	public Vector3 poolObjectPosition;
	static private GameObject poolObject;
	static private GameObject cityObject;
	private Dictionary<int, List<TilePrefab>> tilesPool;
	private Int2 previousPosition;
	private Dictionary<Int2, VisibleTile> tiles;
	
	private void prepareTilePrefabs()
	{
		tileContainer 	= new List<TileData>();
		tilesPool 		= new Dictionary<int, List<TilePrefab>>();
		poolObject = new GameObject();
		poolObject.transform.position = poolObjectPosition;
		poolObject.name = "poolObject";
		cityObject = new GameObject();
		cityObject.transform.position = Vector3.zero;
		cityObject.name = transform.name;

		int type = 0;
		int maxVisibleTiles = ((initialrange * 2) + 1) * ((initialrange * 2) + 2);
		foreach (GameObject obj in prefabs)
		{
			Border other = obj.GetComponent<Border>();
			TileData tileData = new TileData(type, other.left.values, other.top.values, other.right.values, other.down.values);			
			tileContainer.Add(tileData);
			
			List<TilePrefab> tileList = new List<TilePrefab>();
			
			for(int count=0;count < maxVisibleTiles; ++count)
			{
				TilePrefab tile = new TilePrefab();
				tile.tileData = new TileData(type, other.left.values, other.top.values, other.right.values, other.down.values);
				Vector3 position = new Vector3(0, 0, 0);
				GameObject objInstance = GameObject.Instantiate (obj, position, Quaternion.Euler(0,0,0) ) as GameObject;
				objInstance.transform.parent = poolObject.transform;
				objInstance.transform.localPosition = new Vector3(0, 0, 0);
				tile.prefab = objInstance;
				tile.used = false;
				tileList.Add(tile);
			}
			tilesPool.Add(type, tileList);
            ++type;
		}		
	}
	
	/** \brief Visible tile in game */
	class VisibleTile
	{
		/** Tile data*/
		public TilePrefab tilePrefab;
		/** Rotation to apply*/
		public int rotation;
		
		/** Default constructor */
		public VisibleTile()
		{
			tilePrefab = new TilePrefab();
		}
		
		/** Add tile to visible world grid and update the poolObject
		*\param index		position in world grid
		*\param newPrefab	tile data
		*/
		public void Instantiate(Int2 index, TilePrefab newprefab, float tileSize )
		{
			tilePrefab = newprefab;
			tilePrefab.prefab.transform.parent = cityObject.transform;
			tilePrefab.prefab.transform.position = new Vector3(index.y*tileSize, 0, index.x*tileSize);
			tilePrefab.prefab.transform.rotation = Quaternion.Euler(0,90 * rotation,0); 
			tilePrefab.used = true;
			
		}
		
		/** Remove tile from visible world grid and update the poolObject */
		public void Destroy () 
		{	
			tilePrefab.prefab.transform.parent = poolObject.transform;
			tilePrefab.prefab.transform.localPosition = new Vector3(0, 0, 0);
			tilePrefab.prefab.transform.rotation = Quaternion.Euler(0,0,0);
			tilePrefab.used = false;
			tilePrefab = null;
		}
		
	}
	
	/** Generate initial visible tile grid */
	public void generateInitialWorld()
	{	
	
		//diagonal
		Int2 index = new Int2(0,0);
		for (int i = -initialrange; i <= initialrange; ++i)
		{
			index.x = i;
			index.y = i;
			addWorldTile(index, generateTile(null, null, null, null));
		}
		
		//top triangle
		Int2 indexR = new Int2(0,0);
		Int2 indexD = new Int2(0,0);
		for (int col = initialrange - 1 ; col >= -initialrange; --col)
		{
			for (int row = col + 1 ; row <= initialrange; ++row)
			{
				index.y = col;
				index.x = row;
				
				indexR.y = col + 1;
				indexR.x = row;
				
				indexD.y = col;
				indexD.x = row - 1;
				
				addWorldTile(index, generateTile(null, tiles[indexD], null , tiles[indexR]));
				
			}
		}
		
		//down triangle
		Int2 indexT = new Int2(0,0);
		Int2 indexL = new Int2(0,0);
		for (int col = -initialrange + 1; col <= initialrange; ++col)
		{
			for (int row = col - 1; row >= -initialrange ; --row)
			{
				index.y = col;
				index.x = row;
				
				indexL.y = col - 1;
				indexL.x = row;
				
				indexT.y = col;
				indexT.x = row + 1;
				
				addWorldTile(index, generateTile(tiles[indexT], null, tiles[indexL], null));
				
			}
		}
		
	}
	
	/** Add tile to the visible grid and search for a free tile in the pool*/
	private void addWorldTile(Int2 index, VisibleTile tile)
	{
		tiles.Add(index, tile);

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
	
	/** Remove tile from the visible grid and free it in the pool*/
	private void removeWorldTile(Int2 index)
	{
		VisibleTile tile = tiles[index];
		tile.Destroy();
		tiles.Remove(index);
	}
	
	/** Calculates needed side pattern to match*/
	private TileSide extractSide(VisibleTile tile, int offset)
	{
		int index = (4 - (tile.rotation - offset)) % 4;
		List<bool> searchPattern= new List<bool>(tile.tilePrefab.tileData.border[index].tilePattern);
		searchPattern.Reverse();
		TileSide side = new TileSide(false,searchPattern);
		return side;
	}

	/** Calculates needed tile to the input pattern
	*\param topTile		tile at top side
	*\param downTile	tile at down side
	*\param leftTile	tile at left side
	*\param rightTile	tile at right side
	*/
	private VisibleTile generateTile(VisibleTile topTile, VisibleTile downTile, VisibleTile leftTile, VisibleTile rightTile)
	{
		TileSide valuesTop = new TileSide(true,null);
		TileSide valuesDown = new TileSide(true,null);
		TileSide valuesRight = new TileSide(true,null);
		TileSide valuesLeft = new TileSide(true,null);
		
		if (topTile!=null) valuesTop = extractSide(topTile, 3);
		if (downTile != null) valuesDown = extractSide(downTile,1);
		if (leftTile != null)valuesLeft = extractSide(leftTile, 2);
		if (rightTile != null) valuesRight = extractSide(rightTile, 0);
		
		return matchTile(valuesLeft, valuesTop, valuesRight, valuesDown);
	}
	
	/** Check if side pattern match */
	private bool equalSide(List<bool> bits1, List<bool> bits2)
	{
		for (int i = 0; i < tileConnections; ++i)
		{
			if (bits1[i] != bits2[i])
				return false;
		}
		return true;
	}
	
	/** Check if border pattern match and calculates needed tile rotation
	*\param tileData		tile to check
	*\param matchSide		pattern to match
	*\param[out] rotation	rotation to apply to the tile to match the pattern
	*/
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
			rotation = UnityEngine.Random.Range(0,4);
			return true;
		}
	}
	
	/** Search for a tile to match adjacent tiles */
	private VisibleTile matchTile(TileSide valuesLeft, TileSide valuesTop,TileSide valuesRight, TileSide valuesDown)
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
			if (match(tileData, matchSide,ref rotation))
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
		
		if(!found)
			Debug.Log("<color=red>Fatal error:</color> Missing tile with pattern: "+ valuesLeft.patternString() + " " + valuesTop.patternString() + " " + valuesRight.patternString()+ " " +valuesDown.patternString());
		return tile;
	}
		
	/** Calculates player coordinates inside world grid */
	Int2 calculatePosition()
	{
		Int2 position = new Int2(0,0);
		position.x = Mathf.FloorToInt((player.transform.position.z + (tileSize/2)) / tileSize);
		position.y = Mathf.FloorToInt((player.transform.position.x + (tileSize/2)) / tileSize);
		return position;
	}
	
	/** Generates new tiles on the down world grid side */
	void generateDown(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0,0);
		for (int i = -initialrange; i<= initialrange; ++i)
		{
			index.x = previousPosition.x + initialrange;
			index.y = previousPosition.y + i;
			
			removeWorldTile(index);
		}
		
		index.x = currentPosition.x - initialrange;
		index.y = currentPosition.y - initialrange;
		Int2 indexL = new Int2(0 , 0);
		Int2 indexT = new Int2(index.x + 1 , index.y);
		
		addWorldTile(index, generateTile(tiles[indexT], null, null , null));
		
		for (int i = -initialrange+1; i<= initialrange; ++i)
		{			
			index.y = currentPosition.y + i;
			index.x = currentPosition.x - initialrange;
			
			indexL.y = index.y - 1;
			indexL.x = index.x;
			
			indexT.y = index.y;
			indexT.x = index.x + 1;
			
			addWorldTile(index, generateTile(tiles[indexT], null, tiles[indexL] , null));
		}
	}
	
	/** Generates new tiles on the top world grid side */
	void generateTop(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0,0);
		for (int i = -initialrange; i<= initialrange; ++i)
		{
			index.x = previousPosition.x - initialrange;
			index.y = previousPosition.y + i;
			
			removeWorldTile(index);
		}
		
		index.x = currentPosition.x + initialrange;
		index.y = currentPosition.y - initialrange;
		Int2 indexL = new Int2(0 , 0);
		Int2 indexD = new Int2(index.x - 1 , index.y);
		
		addWorldTile(index, generateTile(null, tiles[indexD], null , null));
		
		for (int i = -initialrange+1; i<= initialrange; ++i)
		{			
			index.y = currentPosition.y + i;
			index.x = currentPosition.x + initialrange;
			
			indexL.y = index.y - 1;
			indexL.x = index.x;
			
			indexD.y = index.y;
			indexD.x = index.x - 1;
			
			addWorldTile(index, generateTile(null, tiles[indexD], tiles[indexL] , null));
		}	
	}
	
	/** Generates new tiles on the left world grid side */
	void generateLeft(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0,0);
		for (int i = -initialrange; i<= initialrange; ++i)
		{
			index.x = previousPosition.x + i;
			index.y = previousPosition.y + initialrange;
			
			removeWorldTile(index);
		}
		
		index.x = currentPosition.x - initialrange;
		index.y = currentPosition.y - initialrange;
		Int2 indexR = new Int2(index.x , index.y + 1);
		Int2 indexD = new Int2(0,0);
		
		addWorldTile(index, generateTile(null, null, null , tiles[indexR]));
		
		for (int i = -initialrange+1; i<= initialrange; ++i)
		{			
			index.y = currentPosition.y - initialrange;
			index.x = currentPosition.x + i;
			
			indexR.y = index.y + 1;
			indexR.x = index.x;
			
			indexD.y = index.y;
			indexD.x = index.x - 1;
			
			addWorldTile(index, generateTile(null, tiles[indexD], null , tiles[indexR]));
		}
	}
	
	/** Generates new tiles on the right world grid side */
	void generateRight(Int2 previousPosition, Int2 currentPosition)
	{
		Int2 index = new Int2(0,0);
		for (int i = -initialrange; i<= initialrange; ++i)
		{
			index.x = previousPosition.x + i;
			index.y = previousPosition.y - initialrange;
			
			removeWorldTile(index);
		}
		
		index.x = currentPosition.x - initialrange;
		index.y = currentPosition.y + initialrange;
		Int2 indexL = new Int2(index.x , index.y - 1);
		Int2 indexD = new Int2(0,0);
		
		addWorldTile(index, generateTile(null, null, tiles[indexL], null));
		
		for (int i = -initialrange+1; i<= initialrange; ++i)
		{			
			index.y = currentPosition.y + initialrange;
			index.x = currentPosition.x + i;
			
			indexL.y = index.y - 1;
			indexL.x = index.x;
			
			indexD.y = index.y;
			indexD.x = index.x - 1;
			
			addWorldTile(index, generateTile(null, tiles[indexD], tiles[indexL] , null));
		}
	}
	
	
	/** Initialize world */
	public void initMap()
	{
		tiles = new Dictionary<Int2, VisibleTile>();
		previousPosition = new Int2(0,0);
		prepareTilePrefabs();
		generateInitialWorld();
	}
	/** Checks if player move to new tile, generates new tiles in the player direcction and destroy tiles in the other side */
	public void updateMap()
	{
		Int2 currentPosition = calculatePosition();
		if (previousPosition.x < currentPosition.x)
		{
			generateTop(previousPosition,currentPosition);
		}
		
		if (previousPosition.x > currentPosition.x)
		{
			generateDown(previousPosition,currentPosition);
		}
		
		if (previousPosition.y > currentPosition.y)
		{
			generateLeft(previousPosition,currentPosition);
		}
		
		if (previousPosition.y < currentPosition.y)
		{
			generateRight(previousPosition,currentPosition);
		}
		
		previousPosition = 	currentPosition;
	}
	
	void Start () 
	{
		if (worldSeed < 0)
			worldSeed = UnityEngine.Random.Range(0, int.MaxValue);

		UnityEngine.Random.InitState(worldSeed);

		if (worldSeedText != null)
			worldSeedText.text = "DIMENSION SEED: " + worldSeed;

		player = GameObject.FindGameObjectWithTag("Player").transform;
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
			deleteMap();
			initMap();
			Destroy(poolObject);
		}
		else if (Input.GetKeyDown(KeyCode.End))
		{
			Time.timeScale = 1;
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
	}
}
