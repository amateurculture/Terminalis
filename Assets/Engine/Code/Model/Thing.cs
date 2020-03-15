using UnityEngine;

[System.Serializable]
public class Thing : MonoBehaviour
{
    public GameObject owner;
    public Sprite sprite;
    public float price = 1f;
    public float health = 100f;

    public virtual void Use(Agent agent, InventoryPanel panel, int index) { }
}
