using UnityEngine;

public class TerrainMap : MonoBehaviour
{
    public Terrain mimic;
    Terrain terrain;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrain.terrainData.SetHeights(0,0, mimic.terrainData.GetHeights(0,0,64, 64));
    }
}
