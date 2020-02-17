using UnityEngine;
using UMA.CharacterSystem.Examples;
using System.Collections;

public class InitUmaPanels : MonoBehaviour
{
    public TestCustomizerDD umaCustomizer;

    private void OnEnable()
    {
        StartCoroutine(bluh());
    }

    IEnumerator bluh()
    {
        yield return new WaitForSeconds(3);
        umaCustomizer.ShowHideBodyDNA();
        umaCustomizer.ShowHideFaceDNA();
        umaCustomizer.bodyEditor.transform.parent.gameObject.SetActive(true);
    }
}
