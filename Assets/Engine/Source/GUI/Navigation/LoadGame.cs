using Opsive.UltimateCharacterController.Character;
using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class LoadGame : NavAction
{
    public Maze city;
    public GameObject player;
    Rigidbody rigid;
    UltimateCharacterLocomotion loco;

    private void Start()
    {
        rigid = player.GetComponent<Rigidbody>();
        loco = player.GetComponent<UltimateCharacterLocomotion>();
    }

    public override void DoAction()
    {
        if (city != null) city.InitMap();

        loco.enabled = false;
        rigid.velocity = Vector3.zero;
        Vector3 temp = player.transform.position;
        temp.y = 2;
        player.transform.position = temp;
        loco.enabled = true;

        NavigationStack.Instance.CloseMenu();
    }
}
