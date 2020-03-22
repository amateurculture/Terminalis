using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI widget for a Hohmann xfer. The only option is whether the goal is to rendezvous with an object in the target orbit
/// or start the transfer immediatly. 
/// </summary>
public class HohmannUI : OrbitXferUI {

    public Toggle rendezvousToggle;

    public void RendezvousToggleChanged(bool value) {
        HohmannXfer hohmannXfer = (HohmannXfer)transfer;
        transfer = hohmannXfer.CreateTransferCopy(rendezvousToggle.isOn);
        UpdateUI(transfer);
    }
}
