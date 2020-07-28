using System.Collections;
using UnityEngine;

public class SlideTransition : Transition
{
    public float marginWidth = 0f;
    private Vector3 openPosition;
    private Vector3 closePosition;
    private Vector3 target;
    private RectTransform rect;
    public bool isVisible;
    public float speed = 10f;
    
    public enum Dir
    {
        FromRight,
        FromLeft,
        FromTop,
        FromBottom
    }
    public Dir direction = Dir.FromRight;

    private void Start()
    {
        closePosition = rect.anchoredPosition;
        openPosition = closePosition;

        switch (direction)
        {
            case Dir.FromTop: openPosition.y = marginWidth; break;
            case Dir.FromBottom: openPosition.y = marginWidth; break;
            case Dir.FromRight: openPosition.x = marginWidth; break;
            case Dir.FromLeft: openPosition.x = marginWidth; break;
        }
        target = openPosition;
    }

    private void OnEnable()
    {
        rect = gameObject.GetComponent<RectTransform>();
        Open();
    }

    public override void Open()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        target = openPosition;
        StartCoroutine(PushTransitionCoroutine());
    }

    public override void Close()
    {
        if (!gameObject.activeSelf) return;
        
        StopAllCoroutines();
        target = closePosition;
        StartCoroutine(PopTransitionCoroutine());
    }

    public void ToggleSlide()
    {
        StopAllCoroutines();
        if (isVisible) target = closePosition; else target = openPosition;
        StartCoroutine(PushTransitionCoroutine());
    }

    private IEnumerator PopTransitionCoroutine()
    {
        float i = 0;
        while (i < .8f)
        {
            rect.anchoredPosition = Vector3.Lerp(rect.anchoredPosition, target, i);
            i += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }
        rect.anchoredPosition = target;
        isVisible = false;
        gameObject.SetActive(false);

        NavigationStack.Instance.CompletePop();
    }

    private IEnumerator PushTransitionCoroutine()
    {
        float i = 0;
        while (i < .8f)
        {
            rect.anchoredPosition = Vector3.Lerp(rect.anchoredPosition, target, i);
            i += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }
        rect.anchoredPosition = target;
        isVisible = true;
        //if (!isVisible) gameObject.SetActive(false);
        //NavigationStack.Instance.CompletePush();
    }
}
