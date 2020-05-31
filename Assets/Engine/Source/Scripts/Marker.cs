using UnityEngine;
using TMPro;

public class Marker : MonoBehaviour
{
    void Start()
    {
        GameObject textObject = transform.Find("Text").gameObject;

        if (textObject != null)
        {
            TextMeshPro t = textObject.GetComponent<TextMeshPro>();

            if (transform.parent != null)
            {
                t.text = transform.parent.name;
                Thing thing = transform.parent.gameObject.GetComponent<Thing>();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }
}
