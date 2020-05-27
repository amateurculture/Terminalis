using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrbitXferUIFactory : MonoBehaviour
{
    public GameObject hohmannUIPrefab;
    public GameObject patchedConicUIPrefab;
    public GameObject biellipticUIPrefab;

    public GameObject interceptPrefab; 

    /// <summary>
    /// Factory method to instantiate and initialize a UI widget to present an orbit transfer. The
    /// specific transfer widget will present its input attributes and allow then to be changed. 
    /// 
    /// Select calls back into the controller. 
    /// 
    /// </summary>
    /// <param name="transfer"></param>
    /// <param name="controller"></param>
    /// <returns></returns>
    public GameObject GetUIWidget(OrbitTransfer transfer, OrbitMGController controller) {

        GameObject newWidget = null;

        if (transfer.GetType() == typeof(HohmannXfer)) {
            newWidget = Instantiate(hohmannUIPrefab) as GameObject;
        } else if (transfer.GetType() == typeof(PatchedConicXfer)) {
            newWidget = Instantiate(patchedConicUIPrefab) as GameObject;
        } else if (transfer.GetType() == typeof(BiellipticXfer)) {
            newWidget = Instantiate(biellipticUIPrefab) as GameObject;
        } else {
            Debug.LogError("Unsupported transfer type: " + transfer.GetType());
        }

        if (newWidget != null) {
            OrbitXferUI orbitXferUI = newWidget.GetComponent<OrbitXferUI>();
            if (orbitXferUI != null) {
                orbitXferUI.SetController(controller);
                orbitXferUI.UpdateUI(transfer);
            } else {
                Debug.LogError("No OrbitXferUI component on " + newWidget.name);
            }
        }

        return newWidget;
    }

    public GameObject GetInterceptWidget(TrajectoryData.Intercept intercept, OrbitMGController controller) {
        GameObject newWidget = Instantiate(interceptPrefab) as GameObject;

        if (newWidget != null) {
            OrbitXferUI orbitXferUI = newWidget.GetComponent<OrbitXferUI>();
            if (orbitXferUI != null) {
                orbitXferUI.SetController(controller);
                orbitXferUI.UpdateUI(intercept);
            } else {
                Debug.LogError("No OrbitXferUI component on " + newWidget.name);
            }
        }
        return newWidget;
    }
}
