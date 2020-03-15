using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]

public class AudioCube : MonoBehaviour
{
    AudioSource soundClip;
    public float fadeoutDistance;

    int frameSkip;
    float distance;
    Transform cameraRig;
    Transform player;
    BoxCollider boxCollider;

    private void Reset()
    {
        fadeoutDistance = 50;
    }

    void Start()
    {
        soundClip = GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        boxCollider = GetComponent<BoxCollider>();
        cameraRig = Camera.main.transform;
        frameSkip = 4;
    }

    void Update()
    {
        if (Time.frameCount % frameSkip == 0 && soundClip != null && cameraRig != null)
        {
            if (boxCollider.bounds.Contains(cameraRig.position))
                soundClip.volume = 1;
            else
            {
                //distance = Vector3.SqrMagnitude(boxCollider.ClosestPoint(cameraRig.position) - cameraRig.position);
                distance = Vector3.Distance(boxCollider.bounds.ClosestPoint(cameraRig.position), cameraRig.position);

                if (distance <= fadeoutDistance)
                    soundClip.volume = (fadeoutDistance - distance) / fadeoutDistance;
                else
                    soundClip.volume = 0;
            }
        }
    }
}
