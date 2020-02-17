using System.Collections.Generic;
using UMA;
using UnityEngine;
using UnityEngine.UI;

public class WardrobeMenu : MonoBehaviour
{
    public UMATextRecipe[] clothingRecipes;
    public GameObject prefab;
    public List<Button> buttonList;
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public float menuInterval = .2f;

    int selectionIndex = 0;
    Color actualColor = Color.black;
    ColorBlock colors;
    int oldIndex = -1;
    bool dPadPressed = false;

    private void OnEnable()
    {
        GameObject newObj;
        buttonList = new List<Button>();

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var recipe in clothingRecipes)
        {
            if (recipe != null)
            {
                newObj = Instantiate(prefab, transform);
                newObj.GetComponent<ChangeClothing>().SetRecipe(recipe);
                buttonList.Add(newObj.GetComponent<Button>());
            }
        }
    }

    public void CenterToItem(RectTransform obj)
    {
        float normalizePosition = contentPanel.anchorMin.y - obj.anchoredPosition.y - 25;
        normalizePosition += (float)obj.transform.GetSiblingIndex() / (float)scrollRect.content.transform.childCount;
        normalizePosition /= 1000f;
        normalizePosition = Mathf.Clamp01(1 - normalizePosition);
        scrollRect.verticalNormalizedPosition = normalizePosition;
    }
    float timeToNextButtonPress = 0;

    private void LateUpdate()
    {
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

        var dPadInput = Input.GetAxis("DpadVertical");

        if ((dPadInput == 1 || dPadInput == -1) && Time.time > timeToNextButtonPress)
        {
            timeToNextButtonPress = Time.time + menuInterval;
            dPadPressed = false;
        }

        // TODO add ability for holding DPad buttons to keep scrolling the clothing list automatically
        if (dPadInput == 0)
        {
            dPadPressed = false;
        }
        if ((dPadInput == 1 || Input.GetKeyDown(KeyCode.DownArrow)) && dPadPressed != true)
        {
            selectionIndex--;
            dPadPressed = true;
        } 
        else if ((dPadInput == -1 || Input.GetKeyDown(KeyCode.DownArrow)) && dPadPressed != true)
        {
            selectionIndex++;
            dPadPressed = true;
        }

        if (selectionIndex >= buttonList.Count)
            selectionIndex = 0;
        if (selectionIndex < 0)
            selectionIndex = buttonList.Count - 1;

        
        CenterToItem(buttonList[selectionIndex].GetComponent<RectTransform>());

        if (dPadPressed == false)
            return;

        colors = buttonList[oldIndex].colors;
        colors.normalColor = actualColor;
        buttonList[oldIndex].colors = colors;
        
        colors = buttonList[selectionIndex].colors;
        actualColor = colors.normalColor;
        colors.normalColor = colors.highlightedColor;
        buttonList[selectionIndex].colors = colors;

    }
}
