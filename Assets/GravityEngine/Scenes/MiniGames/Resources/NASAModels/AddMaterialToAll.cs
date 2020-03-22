using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddMaterialToAll : MonoBehaviour
{

    public Material material;

    // Start is called before the first frame update
    void Start()
    {
        foreach (MeshRenderer mesh in gameObject.GetComponentsInChildren<MeshRenderer>()) {
            mesh.material = material;
        }
    }

  
}
