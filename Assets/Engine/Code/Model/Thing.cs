using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Thing : MonoBehaviour
{
    public GameObject owner;
    public Sprite sprite;
    public float price = 1f;
    public float health = 1f;

    public virtual void Use(Agent agent, InventoryPanel panel, int index) { }

    /*
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (this.owner == other.gameObject || this.owner == null)
                hud.ShowText("Use " + this.name + "?", 999999, .5f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        hud.ShowText("", 0, .5f);
    }
    */
}
