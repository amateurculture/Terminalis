using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchPosition : MonoBehaviour
{
    public Transform player;

    void Update()
    {
        if (Time.frameCount % 2 == 0)
        {
            transform.position = player.position;
        }
    }
}
