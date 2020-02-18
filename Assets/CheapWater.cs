using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheapWater : MonoBehaviour
{
    public float scrollSpeed = 0.025f;
    public int frameSkip = 2;
    public Renderer rend;
    float adjustedSkip = 0;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        adjustedSkip = scrollSpeed * frameSkip; 
    }
    void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            float offset = Time.time * adjustedSkip;
            rend.material.SetTextureOffset("_MainTex", new Vector2(offset, offset));
        }
    }
}
