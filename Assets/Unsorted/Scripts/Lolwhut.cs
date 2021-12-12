using UnityEngine;

public class Lolwhut : MonoBehaviour
{
    void Start()
    {
        RenderSettings.fog = false;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Color.black;
        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = player.transform.position;
        playerPos.y = -2;
        player.transform.position = playerPos;
    }
}
