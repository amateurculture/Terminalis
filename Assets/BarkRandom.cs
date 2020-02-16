using System.Collections;
using TMPro;
using UnityEngine;
using UMA.PoseTools;

public class BarkRandom : MonoBehaviour
{
    public TextMeshProUGUI speech;
    public TextMeshProUGUI directionality;
    public CanvasGroup canvasGroup;
    public float speechInterval = 5;
    public float typingInterval = .1f;
    public float pauseInterval = 2;
    public float fadeSpeed = 2; 
    public UMAExpressionPlayer expressionPlayer;
    
    public string[] dialogue;

    private float timeToNext = 0;
    private float t = 0;
    private int barkIndex = 0;
    private string spokenText = "";
    private int typerwriterIndex = 0;
    private bool isTyping = false;
    private Coroutine routine;
    private bool isFadingIn = false;
    private bool isFadingOut = false;
    private Coroutine fadeRoutine;
    private int previousBarkIndex = -1;
    
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
                StopCoroutine(routine);
            }
        }
    }

    IEnumerator FadeIn()
    {
        yield return new WaitForSeconds(pauseInterval);

        while (isFadingIn)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha >= .95f)
            {
                canvasGroup.alpha = 1;
                isFadingIn = false;
                expressionPlayer.overrideMecanimJaw = true;
                routine = StartCoroutine(TypeSpeech());
                t = 0;
                StopCoroutine(fadeRoutine);
            }
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(pauseInterval);

        expressionPlayer.overrideMecanimJaw = false;

        while (isFadingOut)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha <= .1f)
            {
                canvasGroup.alpha = 0f;
                isFadingOut = false;
                t = 0;
                StopCoroutine(fadeRoutine);
            }
            yield return new WaitForEndOfFrame();
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();

        t = 0;
        timeToNext = 0;
        canvasGroup.alpha = 0;
        isFadingIn = false;
        isFadingOut = false;
        isTyping = false;
    }

    private void Start()
    {
        expressionPlayer.overrideMecanimJaw = false;
    }

    void Update()
    {
        if (isFadingIn || isFadingOut) return;

        if (!isTyping && timeToNext < Time.time)
        {
            // Ensure bark chosen isn't the same as the previous one
            barkIndex = (int)(Random.value * dialogue.Length);
            while (barkIndex == previousBarkIndex) 
                barkIndex = (int)(Random.value * dialogue.Length);
            previousBarkIndex = barkIndex;

            spokenText = dialogue[barkIndex];
            timeToNext = Time.time + speechInterval;
            speech.text = "";
            typerwriterIndex = 0;
            isTyping = true;
            isFadingIn = true;
            t = 0;
            fadeRoutine = StartCoroutine(FadeIn());
        }
    }
}
