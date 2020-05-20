using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/** \brief World generator for sample2 */
public class ProceduralWorld : MonoBehaviour 
{
	/** The player object */
	public Transform player;
	
	/** Tile prefabs */
	public GameObject[] prefabs;
	
	/** Size of tile ring generation zone */
	public int initialrange = 1;
	
	/** Size of tile */
	private float tileSize = 100;
	
	/** Number of connections of the tile 
	* 1-> generates 6 different borders
	* 2-> generates 70 different borders
	* 3-> generates 1044 different borders!!!
	*/
	private int tileConnections = 2;
	
	/** The pool object position (away from the camera) */
	public Vector3 poolObjectPosition;
	
	/** Ground Material*/
	public Material groundMaterial;
	
	/** Connection material */
	public Material connectionMaterial;
	
	/** Connection material */
	public Material centerMaterial;
	
	/** World instance */
	private World world;
	
	/** Algorithm due to Fredricksen, Kessler and Maiorana.
	*	Generates circular permutations with repetitions of k-elements
	*/
	static private List<List<int>> fkm(int n,int k)
	{
		List<List<int>> necklaces = new List<List<int>>();
		List<int> a = new List<int>(n);
		int i = -1;
		int j = 0;
		while (++i < n) 
			a.Add(0);
		necklaces.Add(new List<int>(a));
	    
	    while(true)
	    {
	    	i = n;
	    	while (--i >= 0)
	    	{
	    		if (a[i] < k - 1)
	    		{
	    			break;
	    		}
	    	}
	    	
	    	if (i < 0 )
	    	{
	    		break;
	    	}
	    	
	    	a[j = i++]++;
			while (++j < n) 
			{
				a[j] = a[j % i];
			}
			
			if (n % i == 0) 
				necklaces.Add(new List<int>(a));   			
	    }
		return necklaces;
	}
		
	/** Shifts the connection center based on the type of side(left,top,right,down)*/
	private void incrementConnectionOffset(ref float x, ref float z, int side, float cubeSize)
	{
		switch(side)
		{
			case 0: z += cubeSize;break;//left
			case 1: x += cubeSize;break;//top
			case 2: z -= cubeSize;break;//right
			case 3: x -= cubeSize;break;//down
		}
	}
	
	/** Places the connection center based on the type of side on the first position*/
	private void firstConnectionOffset(ref float x, ref float z, int side, float cubeSize)
	{
		switch(side)
		{
			case 0: x = -tileSize/2 + cubeSize/2; z = (-tileSize/2) + cubeSize*1.5f ; break;//left
			case 1: z = tileSize/2 - cubeSize/2; x = (-tileSize/2) + cubeSize*1.5f; break;//top
			case 2: x = tileSize/2 - cubeSize/2; z = (tileSize/2) - cubeSize*1.5f; break;//right
			case 3: z = -tileSize/2 + cubeSize/2; x = (+tileSize/2) - cubeSize*1.5f; break;//down
		}
	}
	
	/** Fill connection border data */
	private void setBorderConnections(ref Border tileBorder,bool[] connections,int side)
	{
		Side sidePattern = new Side();
		sidePattern.values = new List<bool>(tileConnections);
		switch(side)
		{
			case 0: 
			    for(int i= 0;i < tileConnections;++i)
				{
					sidePattern.values.Add(connections[i]);
					tileBorder.left = sidePattern;
				}
				break;
			case 1: 
				for(int i= 0;i < tileConnections;++i)
				{
					sidePattern.values.Add(connections[i]);
					tileBorder.top = sidePattern;
				}
				break;
			case 2:
				for(int i= 0;i < tileConnections;++i)
				{
					sidePattern.values.Add(connections[i]);
					tileBorder.right = sidePattern;
				}
				break;
			case 3: 
				for(int i= 0;i < tileConnections;++i)
				{
					sidePattern.values.Add(connections[i]);
					tileBorder.down = sidePattern;
				}
				break;
		}
	}
	
	/** Creates tiles base from a tile pattern list */ 
	private void createPrefabs(List<List<int>> tilePatterns)
	{
		float planeScale = tileSize/10f;
		float cubeSide = (tileSize/((tileConnections*2)+1));	
		prefabs = new GameObject[tilePatterns.Count];
		
		int counter = 0;
		foreach (List<int> pattern in tilePatterns)
		{
			GameObject tilePrefab = new GameObject();
			tilePrefab.name = "Tile" + counter.ToString();
			
			//ground plane
			GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			groundPlane.transform.localScale = new Vector3(planeScale,1,planeScale);
			
			groundPlane.transform.parent = tilePrefab.transform;
			groundPlane.transform.localPosition = new Vector3(0,0,0);
			groundPlane.GetComponent<MeshRenderer>().material = groundMaterial;
			
			//connections left-top-right-down
			int sideType = 0;
			float cx = 0;
			float cz = 0;
			Border tileBorder = tilePrefab.AddComponent<Border>();
			tileBorder.size = tileConnections;
			foreach (int side in pattern)
			{				
				bool[] connections = intToPattern(side);
				firstConnectionOffset(ref cx, ref cz, sideType, cubeSide);
				for (int connectionPosition = 0; connectionPosition < tileConnections; ++connectionPosition)
				{
					if (connections[connectionPosition] == true)
					{
						GameObject connectionCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
						connectionCube.transform.localScale = new Vector3(cubeSide,cubeSide,cubeSide);
						connectionCube.transform.parent = tilePrefab.transform;
						connectionCube.transform.localPosition = new Vector3(cx,0,cz);
						connectionCube.GetComponent<MeshRenderer>().material = connectionMaterial;
					}
					incrementConnectionOffset(ref cx,ref cz, sideType, cubeSide);//one space between connections
					incrementConnectionOffset(ref cx,ref cz, sideType, cubeSide);
					
					
					setBorderConnections(ref tileBorder, connections, sideType);
				}
				++sideType;		
			}
			//center
			GameObject centerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			centerCube.transform.localScale = new Vector3(cubeSide*(tileConnections*2-1),cubeSide,cubeSide*(tileConnections*2-1));
			centerCube.transform.parent = tilePrefab.transform;
			centerCube.transform.localPosition = new Vector3(0,0,0);
			centerCube.GetComponent<MeshRenderer>().material = centerMaterial;					
			
			prefabs[counter] = tilePrefab;
			++counter;
		}
	}
	
	/** Decode pattern type into a connection array*/
	private bool[] intToPattern(int sideType)
	{
		bool[] pattern = new bool[tileConnections];		
		
		
		//allow tile with side pattern 0
		//string s = Convert.ToString(sideType, 2);
		
		//discard tile with side pattern 0
		string s = Convert.ToString(sideType + 1, 2); //Convert to binary string
		
		pattern = s.PadLeft(tileConnections, '0') // Add 0's from left
			.Select(c => c == '1') // convert each char to int
				.ToArray(); // Convert IEnumerable from select to Array
		
		pattern.Reverse();
		return pattern;
	}
	
	// Use this for initialization
	void Start () 
	{
		int codebits = Convert.ToInt32(Math.Pow(2,tileConnections));
		//allow tile with side pattern 0
		//List<List<int>> necklaces = fkm(4, codebits);
		
		//discard tile with side pattern 0
		List<List<int>> necklaces = fkm(4, codebits-1);
		
		createPrefabs(necklaces);
				
		world = gameObject.AddComponent<World>();
		world.initialrange = initialrange;
		world.tileSize = tileSize;
		world.tileConnections = tileConnections;
		world.player = player;
		world.prefabs = prefabs;
		
		world.poolObjectPosition = poolObjectPosition;
	}
	
	// Update is called once per frame
	void Update () 
	{
	}
}