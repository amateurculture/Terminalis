using System.Collections;
using UnityEngine;

public class SlideTransition : MonoBehaviour
{
    public float marginWidth = 0f;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private Vector3 setPosition;
    private RectTransform rect;
    private bool isVisible = false;
    public float speed = 10f;
    
    public enum Dir
    {
        FromRight,
        FromLeft,
        FromTop,
        FromBottom
    }
    public Dir direction = Dir.FromRight;

    private void OnEnable()
    {
        rect = gameObject.GetComponent<RectTransform>();
        originalPosition = rect.anchoredPosition;
        targetPosition = originalPosition;
        setPosition = targetPosition;

        switch (direction)
        {
            case Dir.FromTop:
                targetPosition.y = marginWidth;
                break;
            case Dir.FromBottom:
                targetPosition.y = marginWidth;
                break;
            case Dir.FromRight:
                targetPosition.x = marginWidth;
                break;
            case Dir.FromLeft:
                targetPosition.x = marginWidth;
                break;
        }
    }

    public void OpenWindow()
    {
        setPosition = originalPosition;
        setPosition = targetPosition;
        StartCoroutine(SlideTransitionCoroutine());
    }

    public void CloseWindow()
    {
        if (isVisible)
        {
            setPosition = originalPosition;
            StartCoroutine(SlideTransitionCoroutine());
        }
    }

    public void ToggleSlide()
    {
        if (isVisible)
            setPosition = originalPosition;
        else
            setPosition = targetPosition;

        StartCoroutine(SlideTransitionCoroutine());
    }
    
    private IEnumerator SlideTransitionCoroutine()
    {
        while (Vector3.Distance(rect.anchoredPosition, setPosition) > 2)
        {
            rect.anchoredPosition = Vector3.Lerp(rect.anchoredPosition, setPosition, Time.deltaTime * speed);
            yield return new WaitForEndOfFrame();
        }
        rect.anchoredPosition = setPosition;
        isVisible = (isVisible) ? false : true;
    }
}
