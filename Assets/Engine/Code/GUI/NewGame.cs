using UnityEngine;
using UMA.Examples;

public class NewGame : MonoBehaviour
{
    public MouseOrbitImproved mainCamera;
    public Player player; 
    public GameObject topToolbar;
    public GameObject bottomToolbar;
    public GameObject panels;
    public CharacterController playerController;

    public void toggleCharacterEditor()
    {
        if (!topToolbar.activeSelf)
        {
            topToolbar.SetActive(true);
            bottomToolbar.SetActive(true);
            panels.SetActive(true);
        }
        else
        {
            topToolbar.SetActive(false);
            bottomToolbar.SetActive(false);

            foreach (Transform panel in panels.transform)
            {
                panel.gameObject.SetActive(false);
            }
            panels.SetActive(false);
        }
    }

    public void StartGame()
    {
        mainCamera.SwitchTarget(player.transform);
    }
}
