using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Code from: https://unity3d.com/learn/tutorials/modules/intermediate/live-training-archive/modal-window-pt2?playlist=17111

public class DisplayManager : MonoBehaviour
{

    public Text displayText;
    public float displayTime;
    public float fadeTime;

    private IEnumerator fadeAlpha;

    private static DisplayManager displayManager;

    public static DisplayManager Instance() {
        if (!displayManager) {
            displayManager = FindObjectOfType(typeof(DisplayManager)) as DisplayManager;
            if (!displayManager)
                Debug.LogError("There needs to be one active DisplayManager script on a GameObject in your scene.");
        }

        return displayManager;
    }

    public void DisplayMessage(string message) {
        displayText.text = message;
        SetAlpha();
    }

    void SetAlpha() {
        if (fadeAlpha != null) {
            StopCoroutine(fadeAlpha);
        }
        fadeAlpha = FadeAlpha();
        StartCoroutine(fadeAlpha);
    }

    IEnumerator FadeAlpha() {
        Color resetColor = displayText.color;
        resetColor.a = 1;
        displayText.color = resetColor;

        yield return new WaitForSeconds(displayTime);

        while (displayText.color.a > 0) {
            Color displayColor = displayText.color;
            displayColor.a -= Time.deltaTime / fadeTime;
            displayText.color = displayColor;
            yield return null;
        }
        yield return null;
    }
}