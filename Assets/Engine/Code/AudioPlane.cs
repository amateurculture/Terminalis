using UnityEngine;

public class AudioPlane : MonoBehaviour
{
    public Transform meshPlane;
    public AudioSource soundClip;
    public float fadeoutDistance;
    int frameSkip;
    Transform player;
    float distance;

    private void Reset()
    {
        soundClip = GetComponent<AudioSource>();
        meshPlane = transform;
        fadeoutDistance = 50;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        frameSkip = 10;
    }

    void Update()
    {
        if (Time.frameCount % frameSkip == 0 && soundClip != null) {

            distance = Vector3.Distance(new Vector3(0, meshPlane.position.y, 0), new Vector3(0, player.position.y, 0));

            if (distance <= 0)
                soundClip.volume = 1;
            else if (distance <= fadeoutDistance)
                soundClip.volume = (fadeoutDistance - distance) / fadeoutDistance;
            else
                soundClip.volume = 0;
        }
    }
}
