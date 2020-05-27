using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI widget for a Hohmann xfer. The only option is whether the goal is to rendezvous with an object in the target orbit
/// or start the transfer immediatly. 
/// </summary>
public class BiellipticUI : OrbitXferUI {

    public Slider excessRadiusSlider;
    public Text excessRadiusValue;

    public void SliderChanged(bool value) {
        BiellipticXfer beXfer = (BiellipticXfer)transfer;
        transfer = beXfer.CreateTransferCopy(excessRadiusSlider.value);
        excessRadiusValue.text = string.Format("{0:0.0}", excessRadiusSlider.value);
        UpdateUI(transfer);
    }
}
