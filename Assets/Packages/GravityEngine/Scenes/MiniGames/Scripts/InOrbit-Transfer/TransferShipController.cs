using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple controller for the TransferShip component. 
/// 
/// Pressing X executes the transfer. 
/// </summary>
public class TransferShipController : MonoBehaviour
{

    [SerializeField]
    private NBody ship = null;

    private TransferShip transferShip;

    private bool done = false; 

    // Start is called before the first frame update
    void Start()
    {
        transferShip = ship.GetComponent<TransferShip>();
        if (transferShip == null) {
            Debug.LogError("Controller could not find TransferShip component on " + ship.gameObject.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X) && !done) {
            Debug.Log("Transfer maneuvers added to GE");
            transferShip.DoTransfer(null);
            done = true; 
        }
    }
}
