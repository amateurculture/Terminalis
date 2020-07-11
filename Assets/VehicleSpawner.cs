using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public GameObject[] vehiclePrefabs;
    [Range(0, 1)] public float chance;

    private void Reset()
    {
        chance = .5f;
    }

    void Awake()
    {
        if (Random.value < chance)
        {
            GameObject obj = Instantiate(vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)]);
            obj.transform.position = transform.position;
            obj.transform.rotation = transform.rotation;
        }
    }
}
