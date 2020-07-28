using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

public class BedMenu : MonoBehaviour
{
    public DynamicCharacterAvatar avatar;
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public GameObject buttonPrefab;
    public Gradient colorGradient;
    public float gradientInterval = .05f;
    public float menuInterval = .25f;
    public string[] buttonNames;
    float scrollHeight = 200f;

    
    [HideInInspector]
    public List<Button> buttonList;

    /*
    float colorIndex = 0;
    int selectionIndex = 0;
    Color actualColor = Color.black;
    ColorBlock colors;
    int oldIndex = -1;
    bool dPadPressed = false;
    float timeToNextButtonPress = 0;
    */
    
    private void OnEnable()
    {
        GameObject newObj;
        buttonList = new List<Button>();

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var name in buttonNames)
        {
            newObj = Instantiate(buttonPrefab, transform);
            newObj.transform.Find("Label").GetComponent<Text>().text = name;
            newObj.transform.Find("Price").GetComponent<Text>().text = "";
            buttonList.Add(newObj.GetComponent<Button>());
        }
    }

    public void CenterToItem(RectTransform obj)
    {
        float normalizePosition = contentPanel.anchorMin.y - obj.anchoredPosition.y - 25;
        normalizePosition += (float)obj.transform.GetSiblingIndex() / (float)scrollRect.content.transform.childCount;
        normalizePosition /= scrollHeight;
        normalizePosition = Mathf.Clamp01(1 - normalizePosition);
        scrollRect.verticalNormalizedPosition = normalizePosition;
    }

    private void LateUpdate()
    {
        /*
        if (oldIndex == -1)
        {
            colors = buttonList[selectionIndex].colors;
            actualColor = colors.normalColor;
            colors.normalColor = colors.highlightedColor;
            buttonList[selectionIndex].colors = colors;
        }
        
        //CenterToItem(buttonList[selectionIndex].GetComponent<RectTransform>());

        oldIndex = selectionIndex;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (Input.GetKeyDown("joystick button 1"))
        {
            buttonList[selectionIndex].GetComponent<ChangeClothing>().Toggle(actualColor);
            actualColor = buttonList[selectionIndex].colors.normalColor;
            return;
        }
        var dPadHorizontalInput = Input.GetAxis("DpadHorizontal");
        var dPadVerticalInput = Input.GetAxis("DpadVertical");

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
            if (colorIndex > 1) colorIndex = 0;
            if (colorIndex < 0) colorIndex = 1;
            var color1 = colorGradient.Evaluate(colorIndex);
            var clothingName = buttonList[selectionIndex].GetComponent<ChangeClothing>().recipe.wardrobeSlot;

            if (clothingName == "Legs")
            {
                avatar.characterColors.SetColor("ClothingBottom01", color1);
                avatar.characterColors.SetColor("Skirt01", color1);
            }
            else if (clothingName == "UnderwearLegs")
            {
                avatar.characterColors.SetColor("SocksColor01", color1);
            }
            else if (clothingName == "UnderwearTop" || clothingName == "UnderwearBottom")
            {
                avatar.characterColors.SetColor("UnderwearTop01", color1);
                avatar.characterColors.SetColor("Underwear01", color1);
            }
            else if (clothingName == "Chest")
            {
                avatar.characterColors.SetColor("ClothingTop01", color1);
                avatar.characterColors.SetColor("ClothingTop02", Color.white);
                avatar.characterColors.SetColor("ClothingTop03", Color.white);
                avatar.characterColors.SetColor("ClothingTop04", Color.white);
            }
            else if (clothingName == "Hair")
            {
                avatar.characterColors.SetColor("Hair", color1);
            }
            else if (clothingName == "Feet")
            {
                avatar.characterColors.SetColor("Footwear01", color1);
            }
            avatar.BuildCharacter();
        }
        else if (dPadHorizontalInput == -1 && dPadPressed != true)
        {
            dPadPressed = true;
            colorIndex -= gradientInterval;
            if (colorIndex > 1) colorIndex = 0;
            if (colorIndex < 0) colorIndex = 1;
            var color1 = colorGradient.Evaluate(colorIndex);
            var clothingName = buttonList[selectionIndex].GetComponent<ChangeClothing>().recipe.wardrobeSlot;

            if (clothingName == "Legs")
            {
                avatar.characterColors.SetColor("ClothingBottom01", color1);
                avatar.characterColors.SetColor("Skirt01", color1);
            }
            else if (clothingName == "UnderwearLegs")
            {
                avatar.characterColors.SetColor("SocksColor01", color1);
            }
            else if (clothingName == "Chest")
            {
                avatar.characterColors.SetColor("ClothingTop01", color1);
                avatar.characterColors.SetColor("ClothingTop02", Color.white);
                avatar.characterColors.SetColor("ClothingTop03", Color.white);
                avatar.characterColors.SetColor("ClothingTop04", Color.white);
            }
            else if (clothingName == "UnderwearTop" || clothingName == "UnderwearBottom")
            {
                avatar.characterColors.SetColor("UnderwearTop01", color1);
                avatar.characterColors.SetColor("Underwear01", color1);
            }
            else if (clothingName == "Hair")
            {
                avatar.characterColors.SetColor("Hair", color1);
            }
            else if (clothingName == "Feet")
            {
                avatar.characterColors.SetColor("Footwear01", color1);
            }
            avatar.BuildCharacter();
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


        CenterToItem(buttonList[selectionIndex].GetComponent<RectTransform>());

        if (dPadPressed == false) return;

        colors = buttonList[oldIndex].colors;
        colors.normalColor = actualColor;
        buttonList[oldIndex].colors = colors;

        colors = buttonList[selectionIndex].colors;
        actualColor = colors.normalColor;
        colors.normalColor = colors.highlightedColor;
        buttonList[selectionIndex].colors = colors;
    }
    */
    }
}
