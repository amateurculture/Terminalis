using UMA;
using UMA.CharacterSystem;
using UnityEngine;

public class ChangeClothing : MonoBehaviour
{
    public DynamicCharacterAvatar avatar;
    public UMATextRecipe recipe;

    public void Add()
    {
        avatar.ClearSlot(recipe.wardrobeSlot);
        avatar.SetSlot(recipe);
        avatar.BuildCharacter();
    }

    public void Toggle()
    {
        // TODO lol... it never does what I need it to do... 
    }

    /*
    void AddClothing(DynamicCharacterAvatar avatar, UMATextRecipe recipe)
    {
        if (recipe != null)
        {
            avatar.SetSlot(recipe);
            avatar.BuildCharacter();
        }
    }
    void RemoveClothing(DynamicCharacterAvatar avatar, string recipe)
    {
        if (recipe != null)
        {
            avatar.ClearSlot(recipe);
            avatar.BuildCharacter();
        }
    }
    */
}
