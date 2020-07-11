using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DPadButton))]

public class ButtonList : MonoBehaviour
{
    public DPadButton dpad;
    List<Button> buttonList = new List<Button>();
    Button currentButton;
    int index;
    public bool preselect;

    private void Reset()
    {
        dpad = GetComponent<DPadButton>();
    }
     
    void OnEnable()
    {
        foreach (Button button in GetComponentsInChildren<Button>()) buttonList.Add(button);
        currentButton = buttonList.ToArray()[0];
        index = -1;
        if (preselect) SelectButton(index = 0);
    }

    void SelectButton(int index)
    {
        currentButton = buttonList[index];
        currentButton.Select();
        currentButton.OnSelect(null);
    }

    void Update()
    {
        if (dpad.up)
        {
            if (--index < 0) index = buttonList.Count - 1;
            SelectButton(index);
        }
        else if (dpad.down)
        {
            if (++index >= buttonList.Count) index = 0;
            SelectButton(index);
        }
    }
}
