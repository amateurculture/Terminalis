using UnityEngine;

public class Equipment : Thing
{
    public Globals.BodyPart attachment = Globals.BodyPart.RightHand;
    protected UMAMountObject mount;
    public SlideTransition slidePanel;

    private void Start()
    {
        mount = Brain.instance.player.GetComponent<UMAMountObject>();
    }

    public override void Use(Agent agent, InventoryPanel panel, int index)
    {
        panel.Equip(agent, index);
        if (mount != null)
            mount.MountObject(this.name);
        
        Cursor.SetCursor(Brain.instance.cursorTexture, new Vector2(32,155), CursorMode.Auto);

        if (slidePanel != null)
        {
            slidePanel.OpenWindow();
        }
    }
}
 