using UnityEngine;

[RequireComponent(typeof(LineRenderer))]

public class DrawLine : MonoBehaviour
{
    public GameObject hand;
    LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0,hand.transform.position);
        lineRenderer.SetPosition(1, transform.localPosition);
    }

    private void Update()
    {
        lineRenderer.SetPosition(0, hand.transform.position);
        lineRenderer.SetPosition(1, transform.position);
    }
}
