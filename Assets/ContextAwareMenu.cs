using UnityEngine;

public class ContextAwareMenu : MonoBehaviour
{
    GameObject player;
    public CanvasGroup canvasGroup;
    
    bool isFadingIn = false;
    bool isFadingOut = false;
    bool isInside = false;
    float t = 0;

    public float fadeDistance = 3.5f;
    public float fadeSpeed = 2f;
   
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (canvasGroup != null)
        {
            canvasGroup = canvasGroup.GetComponent<CanvasGroup>();
            canvasGroup.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isInside && !isFadingIn && !isFadingOut && Vector3.Distance(player.transform.position, transform.position) < fadeDistance)
        {
            isFadingIn = true;
            isFadingOut = false;
            isInside = true;
            t = 0;

            if (canvasGroup != null)
                canvasGroup.gameObject.SetActive(true);
        }

        if (isInside && !isFadingOut && !isFadingIn && Vector3.Distance(player.transform.position, transform.position) >= fadeDistance)
        {   
            isFadingOut = true;
            isFadingIn = false;
            isInside = false;
            t = 0;
        }

        if (isFadingIn)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha >= .95f)
            {
                canvasGroup.alpha = 1;
                isFadingIn = false;
            }
        }

        if (isFadingOut)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha <= .1f)
            {
                canvasGroup.alpha = 0f;
                isFadingOut = false;
                if (canvasGroup != null)
                    canvasGroup.gameObject.SetActive(false);
            }
        }
    }
}
