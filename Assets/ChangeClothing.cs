
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

        if (color == Color.blue) Remove(); else Add();  
    }

    public void Toggle()
    {
        if (actualColor == Color.blue) Remove(); else Add();
    }

    public void Add()
    {
        DeselectUnusuableClothingSlots(recipe.wardrobeSlot);
        SetButtonColor(button, Color.blue);
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
        colors.selectedColor = color;
        b.colors = colors;
        b.GetComponent<ChangeClothing>().actualColor = color;
    }

    public void SetRecipe(UMATextRecipe recipe)
    {
        transform.Find("Label").GetComponent<Text>().text = recipe.DisplayValue;
        this.recipe = recipe;
        this.button = GetComponent<Button>();
        avatar = GameObject.FindGameObjectWithTag("Player").GetComponent<DynamicCharacterAvatar>();
        actualColor = Color.black; // todo starting clothing button color should be based on if the player is wearing something, not just set to black
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
