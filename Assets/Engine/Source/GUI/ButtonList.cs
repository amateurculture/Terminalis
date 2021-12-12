using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DPadButton))]

public class ButtonList : MonoBehaviour
{
    public DPadButton dpad;
    public bool preselect;

    List<Pressable> pressableList = new List<Pressable>();
    Pressable currentPressable;
    int index;

    private void Reset()
    {
        dpad = GetComponent<DPadButton>();
    }
     
    void OnEnable()
    {
        /*
        foreach (Pressable pressable in GetComponentsInChildren<Pressable>()) 
            pressableList.Add(pressable);

        currentPressable = pressableList.ToArray()[0];
        index = -1;

        if (preselect) 
            SelectPressable(index = 0);
        */
    }

    void SelectPressable(int index)
    {
        currentPressable = pressableList[index];
        // currentPressable.Select();
        // currentPressable.OnSelect(null);
    }

    void Update()
    {
        if (dpad.up)
        {
            if (--index < 0) 
                index = pressableList.Count - 1;

            SelectPressable(index);
        }
        else if (dpad.down)
        {
            if (++index >= pressableList.Count) 
                index = 0;

            SelectPressable(index);
        }
    }
}
