using System.Collections.Generic;
using UMA;
using UnityEngine;
using UnityEngine.UI;

public class MirrorController : MonoBehaviour
{
    public UMATextRecipe[] clothingRecipes;
    public GameObject prefab;
    public List<Button> buttonList;
    int selectionIndex = 0;
    Color actualColor = Color.black;
    ColorBlock colors;
    int oldIndex = -1;
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    bool dPadPressed = false;

    private void Start()
    {
        GameObject newObj;
        buttonList = new List<Button>();

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

        // TODO add ability for holding DPad buttons to keep scrolling the clothing list automatically
        if (Input.GetAxis("DpadVertical") == 0)
        {
            dPadPressed = false;
        }
        if ((Input.GetAxis("DpadVertical") == 1 || Input.GetKeyDown(KeyCode.DownArrow)) && dPadPressed != true)
        {
            selectionIndex--;
            dPadPressed = true;
        } 
        else if ((Input.GetAxis("DpadVertical") == -1 || Input.GetKeyDown(KeyCode.DownArrow)) && dPadPressed != true)
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
