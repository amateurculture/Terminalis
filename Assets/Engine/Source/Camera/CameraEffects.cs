using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraEffects : MonoBehaviour
{
    public bool isDrunk;

    Camera cameraMain;
    PostProcessVolume postProcessingVolume;
    LensDistortion lensDistortion;
    float _timePassed;
    float positionOrigin;

    void Start()
    {
        isDrunk = false;
        cameraMain = Camera.main;
        positionOrigin = .25f;
    }
    
    void Update()
    {
        if (isDrunk)
        {
            postProcessingVolume = cameraMain.GetComponent<PostProcessVolume>();
            postProcessingVolume.profile.TryGetSettings(out lensDistortion);
            var boolParam = new BoolParameter { value = false };
            lensDistortion.enabled = boolParam;
            _timePassed += Time.deltaTime;
            positionOrigin = Mathf.Lerp(positionOrigin, .75f, Mathf.PingPong(_timePassed, 1));

            var f = new FloatParameter() { value = positionOrigin };
            lensDistortion.intensityX = f;
            lensDistortion.intensityY = f;
        }
    }
}
