using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUpdateOnEnable : MonoBehaviour
{
    public NavMeshData m_NavMeshData;
    private NavMeshDataInstance m_NavMeshInstance;
    // Global containers for all active mesh/terrain tags
    // The center of the build
    public Transform m_Tracked;

    // The size of the build bounds
    public Vector3 m_Size = new Vector3(80.0f, 20.0f, 80.0f);

    NavMeshData m_NavMesh;
    AsyncOperation m_Operation;
    NavMeshDataInstance m_Instance;
    List<NavMeshBuildSource> m_Sources = new List<NavMeshBuildSource>();

    /*
    void UpdateNavMesh(bool asyncUpdate = false)
    {
        NavMeshSourceTag.Collect(ref m_Sources);
        var defaultBuildSettings = NavMesh.GetSettingsByID(0);
        var bounds = QuantizedBounds();

        if (asyncUpdate)
            m_Operation = NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMesh, defaultBuildSettings, m_Sources, bounds);
        else
            NavMeshBuilder.UpdateNavMeshData(m_NavMesh, defaultBuildSettings, m_Sources, bounds);
    }
    */

    void OnEnable()
    {
        m_NavMeshInstance = NavMesh.AddNavMeshData(m_NavMeshData);
        //UpdateNavMesh(false);
    }

    void OnDisable()
    {
        NavMesh.RemoveNavMeshData(m_NavMeshInstance);
    }
}
