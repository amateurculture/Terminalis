using TMPro;
using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class Typerwriter : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;

    void Start()
    {
        textMeshPro.text = "";
    }

    public void AddDot()
    {
        textMeshPro.text += ".";
    }

    /*
    IEnumerator Type()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(.2f);
            textMeshPro.text += ".";
        }
    }
    private void OnEnable()
    {
        StartCoroutine(Type());
    }
    */
}
