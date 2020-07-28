using System.Collections;
using TMPro;
using UnityEngine;
using UMA.PoseTools;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class BarkRandom : MonoBehaviour
{
    public TextMeshProUGUI speech;
    public TextMeshProUGUI directionality;
    public CanvasGroup canvasGroup;
    public float speechInterval = 5;
    public float typingInterval = .1f;
    public float bubbleInterval = 5f;
    public Vector2 pauseInterval = new Vector2(5, 10);
    public float fadeSpeed = 2; 
    public UMAExpressionPlayer expressionPlayer;
    public string[] opening;
    public string[] dialogue;

    private float timeToNext = 0;
    private float t = 0;
    private int barkIndex = 0;
    private string spokenText = "";
    private int typerwriterIndex = 0;
    private bool isTyping = false;
    private bool isFadingIn = false;
    private bool isFadingOut = false;
    private int previousBarkIndex = -1;
    private Coroutine typingCoroutine;
    private Coroutine fadeRoutine;

    bool isOpeningOver = false;

    IEnumerator TypeSpeech()
    {
        while (true)
        {
            yield return new WaitForSeconds(typingInterval);

            var character = spokenText[typerwriterIndex];
            speech.text += character;
            typerwriterIndex++;

            if (typerwriterIndex >= spokenText.Length)
            {
                isTyping = false;
                isFadingOut = true;
                fadeRoutine = StartCoroutine(FadeOut());
                StopCoroutine(typingCoroutine);
            }
        }
    }

    IEnumerator FadeIn()
    {
        while (isFadingIn)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha >= .95f)
            {
                canvasGroup.alpha = 1;
                isFadingIn = false;
                expressionPlayer.overrideMecanimJaw = true;
                typingCoroutine = StartCoroutine(TypeSpeech());
                t = 0;
                StopCoroutine(fadeRoutine);
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(bubbleInterval);
        expressionPlayer.overrideMecanimJaw = false;

        while (canvasGroup.alpha > .1f)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha <= .1f)
            {
                canvasGroup.alpha = 0f;
            }
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(Random.Range(pauseInterval.x, pauseInterval.y));
        t = 0;
        isFadingOut = false;
        StopCoroutine(fadeRoutine);
    }

    private void OnDisable()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        fadeRoutine = null;
        typingCoroutine = null;

        t = 0;
        timeToNext = 0;
        canvasGroup.alpha = 0;
        isFadingIn = false;
        isFadingOut = false;
        isTyping = false;
        isOpeningOver = false;
    }

    private void Start()
    {
        if (expressionPlayer != null) expressionPlayer.overrideMecanimJaw = false;
    }

    void Update()
    {
        if (isFadingIn || isFadingOut) return;

        if (!isTyping && timeToNext < Time.time)
        {
            timeToNext = Time.time + speechInterval;
            speech.text = "";
            typerwriterIndex = 0;
            isTyping = true;
            isFadingIn = true;
            t = 0;

            // Pick a random opening line and display that first (optional)
            if (!isOpeningOver && opening.Length > 0)
            {
                barkIndex = (int)(Random.value * opening.Length);
                spokenText = opening[barkIndex];
                isOpeningOver = true;
                fadeRoutine = StartCoroutine(FadeIn());
            }
            else
            {
                // Ensure bark chosen isn't the same as the previous one
                barkIndex = (int)(Random.value * dialogue.Length);

                while (barkIndex == previousBarkIndex)
                    barkIndex = (int)(Random.value * dialogue.Length);

                previousBarkIndex = barkIndex;
                spokenText = dialogue[barkIndex];
                fadeRoutine = StartCoroutine(FadeIn());
            }
        }
    }
}
