using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheapWater : MonoBehaviour
{
    //public float scrollSpeed = 0.25f;
    public int frameSkip = 5;
    public Renderer rend;
    float offset = 0;
    float adjustedSkip = 0;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        //adjustedSkip = scrollSpeed / (float)frameSkip; 
    }

    void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            offset += Time.deltaTime * .1f;
            rend.material.SetTextureOffset("_MainTex", new Vector2(offset, offset));
        }
    }
}
