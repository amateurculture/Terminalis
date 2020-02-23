using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ShowTime : MonoBehaviour
{
    public int frameSkip;
    TextMeshProUGUI textMesh;
    public TimeController timeController;
    public bool isSundial;

    private void Reset()
    {
        frameSkip = 5;
    }

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            if (!isSundial || 
                (isSundial && timeController.hour > 6 && timeController.hour < 17)) {
                var timeText = (timeController.hour % 12) + ":" + timeController.minute.ToString("00") + " " + ((timeController.hour < 12) ? "AM" : "PM");
                textMesh.text = timeText; 
            } else
            {
                textMesh.text = "";
            }
        }
    }
}
