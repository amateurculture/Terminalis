using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for UI widgets that summarize the attributes of the transfer. Common operations:
/// - present name
/// - show total time and dV
/// - list maneuvers 
/// are handled in the base class. 
/// 
/// Extensions to the base class provide user-interaction to adjust free parameters associated with the transfer. 
/// 
/// This implementation provides all the required user-parameter input using standard Unity UI elements. 
/// The expectation is it will be cloned and modifed to "juice up" transfers in a specific game design. 
/// 
/// </summary>
public class OrbitXferUI : MonoBehaviour {

    // Prefabs (usually UIPanels) for each of the orbit transfers

    public Text titleText;
    public Text summaryText;
    public Text maneuverText;

    protected OrbitTransfer transfer;
    protected TrajectoryData.Intercept intercept;
    protected OrbitMGController gameController; 

    /// <summary>
    /// Update the UI fields based on the transfer
    /// </summary>
	public void UpdateUI(OrbitTransfer transfer) {

        this.transfer = transfer;

        titleText.text = transfer.ToString();
        summaryText.text = string.Format("dV={0:0.00} time={1:0.00}", transfer.GetDeltaV(), transfer.GetDeltaT());

        // maneuvers
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Maneuvers:\n");
        foreach (Maneuver m in transfer.GetManeuvers()) {
            sb.Append(string.Format("time={0:0.0}  dV={1:0.0}\n", m.worldTime, m.dV ));
        }
        maneuverText.text = sb.ToString();
    }

    public void UpdateUI(TrajectoryData.Intercept intercept) {
        this.intercept = intercept;
        titleText.text = "Intercept";
        summaryText.text = string.Format("dV={0:0.00} time={1:0.00}", intercept.dV, intercept.dT);
        maneuverText.text = string.Format("time={0:0.0}  dV={1:0.0}\n", intercept.tp1.t, intercept.dV);
    }

    public void SetController(OrbitMGController controller) {
        this.gameController = controller;
    }

    public void OnSelectButton() {
        // tell the game controller an orbit transfer has been selected
        // (Could make this an event eventually - but keep demo code simple, with direct call paths)
        if (transfer != null) {
            gameController.OrbitTransferSelected(transfer);
        } else if (intercept != null) {
            gameController.InterceptSelected(intercept);
        } else {
            Debug.LogError("Misconfigured - no intercept or transfer");
        }
    }
}
