using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;

[System.Serializable]
public class Pressable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Outlet action;
    public Color color;
    public Color hover;
    TextMeshProUGUI buttonText;
    bool isHovered;

    void Reset()
    {
        color = Color.white;
        hover = new Color(1, 1, 1, .35f);
    }

    void Start()
    {
        buttonText = GetComponentsInChildren<TextMeshProUGUI>()[0];
        buttonText.color = color;
    }

    void Update()
    {
        if (isHovered && Input.GetMouseButtonDown(0))
        {
            action.Action();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonText.color = hover;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.color = color;
        isHovered = false;
    }
}
