using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class MirrorMenu : MonoBehaviour
{
    public DynamicCharacterAvatar avatar;
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public GameObject buttonPrefab;
    public Gradient colorGradient;
    public float gradientInterval = .05f;
    public float menuInterval = .25f;
    public UMATextRecipe[] recipes;
    [HideInInspector] public List<Button> buttonList;

    int selectionIndex = 0;
    Color actualColor = Color.black;
    ColorBlock colors;
    int oldIndex = -1;
    bool dPadPressed = false;
    float timeToNextButtonPress = 0;
    float scrollHeight = 200f;
    float colorIndex = 0;
    float colorIndex2 = 0;

    private void OnEnable()
    {
        GameObject newObj;
        buttonList = new List<Button>();

        foreach (Transform child in transform) Destroy(child.gameObject);

        foreach (var recipe in recipes)
        {
            if (recipe != null)
            {
                newObj = Instantiate(buttonPrefab, transform);
                newObj.GetComponent<ChangeClothing>().SetRecipe(recipe);
                buttonList.Add(newObj.GetComponent<Button>());
            }
        }
        scrollHeight = 25f * (buttonList.Count-5);
    }

    public void CenterToItem(RectTransform obj)
    {
        float normalizePosition = contentPanel.anchorMin.y - obj.anchoredPosition.y - 25;
        normalizePosition += (float)obj.transform.GetSiblingIndex() / (float)scrollRect.content.transform.childCount;
        normalizePosition /= scrollHeight;
        normalizePosition = Mathf.Clamp01(1 - normalizePosition);
        scrollRect.verticalNormalizedPosition = normalizePosition;
    }

    void SetClothingColor()
    {
        var slotName = buttonList[selectionIndex].GetComponent<ChangeClothing>().recipe.wardrobeSlot;
        if (colorIndex > 1) colorIndex = 0;
        if (colorIndex < 0) colorIndex = 1;
        colorIndex2 = colorIndex - .1f;
        if (colorIndex2 > 1) colorIndex = 0;
        if (colorIndex2 < 0) colorIndex2 = 0;

        var color1 = colorGradient.Evaluate(colorIndex);
        var color2 = colorGradient.Evaluate(colorIndex2);

        if (slotName == "Legs")
        {
            avatar.characterColors.SetColor("ClothingBottom01", color1);
            avatar.characterColors.SetColor("Skirt01", color1);
        }
        else if (slotName == "UnderwearLegs")
        {
            avatar.characterColors.SetColor("SocksColor01", color1);
        }
        else if (slotName == "UnderwearTop" || slotName == "UnderwearBottom")
        {
            avatar.characterColors.SetColor("UnderwearTop01", color1);
            avatar.characterColors.SetColor("Underwear01", color1);
        }
        else if (slotName == "Chest")
        {
            avatar.characterColors.SetColor("ClothingTop01", color1);
            avatar.characterColors.SetColor("OverClothing01", color1);
            avatar.characterColors.SetColor("ClothingTop02", color2);
            avatar.characterColors.SetColor("ClothingTop03", color2);
            avatar.characterColors.SetColor("ClothingTop04", color2);
        }
        else if (slotName == "OverClothing")
        {
            avatar.characterColors.SetColor("ClothingTop01", color1);
            avatar.characterColors.SetColor("OverClothing01", color1);
            avatar.characterColors.SetColor("ClothingTop02", color2);
            avatar.characterColors.SetColor("ClothingTop03", color2);
            avatar.characterColors.SetColor("ClothingTop04", color2);
        }
        else if (slotName == "Hair")
        {
            avatar.characterColors.SetColor("Hair", color1);
        }
        else if (slotName == "Feet")
        {
            avatar.characterColors.SetColor("Footwear01", color1);
        }
        else if (slotName == "MakeupMouth")
        {
            avatar.characterColors.SetColor("Lipstick", color1);
        }
        avatar.BuildCharacter();
    }

    private void LateUpdate()
    {
        if (oldIndex == -1)
        {
            colors = buttonList[selectionIndex].colors;
            actualColor = colors.normalColor;
            colors.normalColor = colors.highlightedColor;
            buttonList[selectionIndex].colors = colors;
        }
        oldIndex = selectionIndex;
        
        if (Input.GetKeyDown("joystick button 1"))
        {
            buttonList[selectionIndex].GetComponent<ChangeClothing>().Toggle(actualColor);
            actualColor = buttonList[selectionIndex].colors.normalColor;
            SetClothingColor();
            return;
        }
        var dPadHorizontalInput = Input.GetAxis("Dpad X");
        var dPadVerticalInput = Input.GetAxis("Dpad Y");

        if ((dPadVerticalInput == 1 || dPadVerticalInput == -1 || dPadHorizontalInput == 1 || dPadHorizontalInput == -1) && Time.time > timeToNextButtonPress)
        {
            timeToNextButtonPress = Time.time + menuInterval;
            dPadPressed = false;
        }
        
        if (dPadVerticalInput == 0 && dPadHorizontalInput == 0)
        {
            dPadPressed = false;
            timeToNextButtonPress = 0;
        }
        else if (dPadHorizontalInput == 1 && dPadPressed != true)
        {
            dPadPressed = true;
            colorIndex += gradientInterval;
            SetClothingColor();
        }
        else if (dPadHorizontalInput == -1 && dPadPressed != true)
        {
            dPadPressed = true;
            colorIndex -= gradientInterval;
            SetClothingColor();
        }
        else if (dPadVerticalInput == 1 && dPadPressed != true)
        {
            selectionIndex--;
            dPadPressed = true;
        } 
        else if (dPadVerticalInput == -1 && dPadPressed != true)
        {
            selectionIndex++;
            dPadPressed = true;
        }

        if (selectionIndex >= buttonList.Count)
            selectionIndex = 0;
        if (selectionIndex < 0)
            selectionIndex = buttonList.Count - 1;

        if (buttonList != null && buttonList.Count > 0) CenterToItem(buttonList[selectionIndex].GetComponent<RectTransform>());

        if (dPadPressed == false) return;

        colors = buttonList[oldIndex].colors;
        colors.normalColor = actualColor;
        buttonList[oldIndex].colors = colors;
        
        colors = buttonList[selectionIndex].colors;
        actualColor = colors.normalColor;
        colors.normalColor = colors.highlightedColor;
        buttonList[selectionIndex].colors = colors;
    }
}
