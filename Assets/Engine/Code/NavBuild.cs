using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavBuild : MonoBehaviour
{
    NavMeshSurface surface;

    // Start is called before the first frame update
    void Start()
    {
        surface = GetComponent<NavMeshSurface>();
        surface.BuildNavMesh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
