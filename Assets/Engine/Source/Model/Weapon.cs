using UnityEngine;

public class Weapon : Equipment
{
    public float damage = 10f;
    public float range = 0f;
    public Sprite reticle;
    
    private void Start()
    {
        mount = Brain.instance.player.GetComponent<UMAMountObject>();
    }

    public override void Use(Agent agent, InventoryPanel panel, int index)
    {
        panel.Equip(agent, index);
        if (mount != null)
            mount.MountObject(this.name);

        if (reticle != null)
            Cursor.SetCursor(reticle.texture, new Vector2(32, 32), CursorMode.Auto);
        else
            Cursor.SetCursor(Brain.instance.cursorTexture, new Vector2(32, 155), CursorMode.Auto);
    }
}
