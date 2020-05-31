
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

public class ChangeClothing : MonoBehaviour
{
    DynamicCharacterAvatar avatar;
    public UMATextRecipe recipe;
    Button button;
    Color actualColor;

    public void Toggle(Color color)
    {
        if (color == null)
            return;

        if (color == button.colors.selectedColor) Remove(); else Add();  
    }

    public void Toggle()
    {
        if (actualColor == button.colors.selectedColor) Remove(); else Add();
    }

    public void Add()
    {
        DeselectUnusuableClothingSlots(recipe.wardrobeSlot);
        SetButtonColor(button, button.colors.selectedColor);
        avatar.SetSlot(recipe);
        avatar.BuildCharacter();
    }   

    void Remove()
    {
        SetButtonColor(button, Color.black);

        if (recipe != null)
        {
            avatar.ClearSlot(recipe.wardrobeSlot);
            avatar.BuildCharacter();
        }
    }

    void SetButtonColor(Button b, Color color)
    {
        var colors = b.colors;
        colors.normalColor = color;
        //colors.selectedColor = color;
        b.colors = colors;
        b.GetComponent<ChangeClothing>().actualColor = color;
    }

    public void SetRecipe(UMATextRecipe recipe)
    {
        transform.Find("Label").GetComponent<Text>().text = recipe.DisplayValue;
        this.recipe = recipe;
        this.button = GetComponent<Button>();
        avatar = GameObject.FindGameObjectWithTag("Player").GetComponent<DynamicCharacterAvatar>();

        foreach (KeyValuePair<string, UMATextRecipe> entry in avatar.WardrobeRecipes)
        {
            if (entry.Value == recipe)
            {
                SetButtonColor(GetComponent<Button>(), button.colors.selectedColor);
                break;
            }
        }
    }

    private void DeselectUnusuableClothingSlots(string slotName)
    {
        var buttons = transform.parent.GetComponent<WardrobeMenu>().buttonList;

        foreach (var b in buttons)
        {
            if (slotName.Equals(b.GetComponent<ChangeClothing>().recipe.wardrobeSlot))
            {
                SetButtonColor(b, Color.black);
            }
        }
    }
}
