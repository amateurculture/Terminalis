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
    Color currentColor = Color.black;
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

    public void SnapTo(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        Vector3 targetAdjusted = target.position;
        targetAdjusted.x += 50;

        Vector3 contentPanelAdjusted = contentPanel.position;
        contentPanelAdjusted.y -= 50;

        contentPanel.anchoredPosition =
            (Vector2)scrollRect.transform.InverseTransformPoint(contentPanelAdjusted)
            - (Vector2)scrollRect.transform.InverseTransformPoint(targetAdjusted);
    }
    
    private void LateUpdate()
    {
        if (oldIndex == -1)
        {
            colors = buttonList[selectionIndex].colors;
            currentColor = colors.normalColor;
            colors.normalColor = colors.highlightedColor;
            buttonList[selectionIndex].colors = colors;
        }

        oldIndex = selectionIndex;

        if (Input.GetKeyDown("joystick button 1"))
        {
            buttonList[selectionIndex].GetComponent<ChangeClothing>().Toggle();
            currentColor = buttonList[selectionIndex].colors.normalColor;
            return;
        }

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

        if (dPadPressed == false)
            return;

        if (selectionIndex < 0)
            selectionIndex = 0;

        colors = buttonList[oldIndex].colors;
        colors.normalColor = currentColor;
        buttonList[oldIndex].colors = colors;
        
        colors = buttonList[selectionIndex].colors;
        currentColor = colors.normalColor;
        colors.normalColor = colors.highlightedColor;
        buttonList[selectionIndex].colors = colors;

        //SnapTo(buttonList[selectionIndex].GetComponent<RectTransform>());
    }
}
