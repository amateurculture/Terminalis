using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ShowTime : MonoBehaviour
{
    public int frameSkip = 5;
    TextMeshProUGUI textMesh;

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(Time.time);
            string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            textMesh.text = timeText;
        }
    }
}
