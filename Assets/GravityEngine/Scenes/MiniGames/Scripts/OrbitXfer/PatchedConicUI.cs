using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI widget for a Hohmann xfer. The only option is whether the goal is to rendezvous with an object in the target orbit
/// or start the transfer immediatly. 
/// </summary>
public class PatchedConicUI : OrbitXferUI {

    public Slider lambdaSlider;
    public Text lambdaValue;

    void Start() {
        lambdaSlider.value = (float) PatchedConicXfer.LAMBDA1_DEFAULT;
        lambdaValue.text = string.Format("{0:0.0}", lambdaSlider.value);
    }

    public void SliderChanged(bool value) {
        PatchedConicXfer conicXfer = (PatchedConicXfer) transfer;
        float lambda = lambdaSlider.value;
        lambdaValue.text = string.Format("{0:0.0}", lambdaSlider.value);
        transfer = conicXfer.CreateTransferCopy(lambda);
        UpdateUI(transfer);
    }

 
}
