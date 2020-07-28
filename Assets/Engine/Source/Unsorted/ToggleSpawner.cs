using UnityEngine;

public class ToggleSpawner : MonoBehaviour
{
    public GameObject[] prefabs;
    [Range(0, 1)] public float chance;

    private void Reset()
    {
        chance = .5f;
    }

    void Awake()
    {
        if (Random.value < chance)
        {
            var r = Random.Range(0, prefabs.Length - 1);
            Instantiate(prefabs[r], transform, false);
        }
    }
}
