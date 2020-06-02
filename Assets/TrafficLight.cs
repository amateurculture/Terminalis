using System.Collections;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public Light redLight;
    public Light greenLight;
    public Light yellowLight;
    public Light oppositeRedLight;
    public Light oppositeGreenLight;
    public Light oppositeYellowLight;

    void Start()   
    {
        StartCoroutine(TrafficLightCoroutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    void DisableAllLights()
    {
        redLight.enabled = false;
        greenLight.enabled = false;
        yellowLight.enabled = false;
        oppositeGreenLight.enabled = false;
        oppositeRedLight.enabled = false;
        oppositeYellowLight.enabled = false;
    }

    IEnumerator TrafficLightCoroutine()
    {
        DisableAllLights();
        greenLight.enabled = true;
        oppositeRedLight.enabled = true;
        yield return new WaitForSeconds(30f);

        greenLight.enabled = false;
        yellowLight.enabled = true;
        yield return new WaitForSeconds(3.5f);

        DisableAllLights();
        oppositeGreenLight.enabled = true;
        redLight.enabled = true;
        yield return new WaitForSeconds(30f);

        oppositeGreenLight.enabled = false;
        oppositeYellowLight.enabled = true;
        yield return new WaitForSeconds(3.5f);

        StartCoroutine(TrafficLightCoroutine());
    }
}
