using UnityEngine;

[System.Serializable]
public class Thing : MonoBehaviour
{
    public GameObject owner;
    public Sprite sprite;
    public float vitality = 100f;
    public float value = 1f;

    public virtual void Use(Agent agent, InventoryPanel panel, int index) { }
}
