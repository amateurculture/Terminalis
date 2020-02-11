using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

public class UmaClothingTest : MonoBehaviour
{
    public UMATextRecipe[] textRecipe;
    public DynamicCharacterAvatar avatar;

    private void Start()
    {
        foreach (var recipe in textRecipe)
        {
            if (recipe == null)
            {
                avatar.BuildCharacter();
                break;
            }
            avatar.SetSlot(recipe);
        }
        avatar.BuildCharacter();
    }
}
