using UnityEngine;
using AC_System;
using UnityEngine.UI;

public class ToggleInvertHorizontal : MonoBehaviour
{
    GameObject player;
    PlayerController playerController;

    private void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player?.GetComponent<PlayerController>();
        GetComponent<Toggle>().isOn = playerController?.invertHorizontal ?? false;
    }

    public void Toggle(bool option)
    {
        if (playerController != null)
            playerController.invertHorizontal = option;
    }
}
