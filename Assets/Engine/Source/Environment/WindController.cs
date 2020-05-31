using UnityEngine;

public class WindController : MonoBehaviour
{
    public WindZone windZone;
    public ParticleSystem particles;
    [Range(0,360)] public float direction;
    [Range(0, 1)] public float speed;

    float windLerp;
    [HideInInspector] public float currentDirection;
    [HideInInspector] public float currentSpeed;
    float t1;
    bool isLerping;
    int frameSkip;

    enum WhatIsLerping
    {
        speed,
        rotation
    }
    WhatIsLerping whatIsLerping;

    private void Reset()
    {
        speed = .5f;
    }

    private void Start()
    {
        windLerp = 0.0001f;
        currentSpeed = speed;
        currentDirection = direction; 
        frameSkip = 60;
        windZone = GetComponent<WindZone>();

        UpdateWind();
    }

    private void UpdateWind()
    {
        if (windZone != null)
        {
            windZone.windMain = speed;
            windZone.mode = WindZoneMode.Directional;
            windZone.windPulseFrequency = 0f;
            windZone.windPulseMagnitude = 0f;
            windZone.windTurbulence = 0f;
        }
    }

    void UpdateRotation()
    {
        currentDirection = Mathf.Lerp(currentDirection, direction, t1);
        var x = Mathf.Cos(currentDirection * 0.0174532925f) * 1.5f;
        var z = Mathf.Sin(currentDirection * 0.0174532925f) * 1.5f;

        var fo = particles.forceOverLifetime;
        fo.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, x);
        curve.AddKey(1f, x);
        fo.x = new ParticleSystem.MinMaxCurve(1.5f, curve);

        curve = new AnimationCurve();
        curve.AddKey(0.0f, z);
        curve.AddKey(1f, z);
        fo.z = new ParticleSystem.MinMaxCurve(1.5f, curve);

        if (Mathf.Abs(currentDirection - direction) <= .1f)
        {
            currentDirection = direction;
            isLerping = false;
        }
    }

    void UpdateSpeed()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, speed, t1);

        if (Mathf.Abs(currentSpeed - speed) <= .1f)
        {
            currentSpeed = speed;
            windZone.windMain = speed;
            isLerping = false;
        }
    }

    private void Update()
    {
        if (isLerping)
        {
            t1 += Time.deltaTime * windLerp;

            switch (whatIsLerping)
            {
                case WhatIsLerping.rotation: UpdateRotation(); break;
                case WhatIsLerping.speed: UpdateSpeed(); break;
                default: break;
            }
        }
        else if (Time.frameCount % frameSkip == 0 && windZone != null)
        {
            if (currentDirection != direction)
            {
                whatIsLerping = WhatIsLerping.rotation;
                isLerping = true;
                t1 = 0;
            }
            else if (currentSpeed != speed)
            {
                whatIsLerping = WhatIsLerping.speed;
                isLerping = true;
                t1 = 0;
                windZone.windMain = speed;
            } 
            else UpdateWind();
        }
    }
}
