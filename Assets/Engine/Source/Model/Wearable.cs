
using UMA;
using UMA.CharacterSystem;

public class Wearable : Equipment
{
    public float protection;
    
    public override void Use(Agent agent, InventoryPanel panel, int index)
    {
        var avatar = Brain.instance.player.GetComponent<DynamicCharacterAvatar>();
        var wearable = new UMATextRecipe();
        
        if (avatar != null)
        {
            panel.Equip(agent, index);

            wearable.name = this.name;
            avatar.SetSlot(wearable);
            avatar.BuildCharacter();
        }
    }
}
