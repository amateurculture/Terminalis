using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseButton : MonoBehaviour
{
    Button button;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    private void OnMouseOver()
    {
        Debug.Log("Mouse is over GameObject.");
        button.Select();
    }

    private void OnMouseExit()
    {
        button.OnSelect(null);
    }
}
