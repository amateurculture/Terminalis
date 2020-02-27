using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class QualityManager : MonoBehaviour
{
    [Range(0, 10)] public int qualityLevel;
    [Range(24, 120)] public int frameRate;
    public int frameSkip;
    public bool enableHDR;

    Camera postprocessingCamera;
    PostProcessVolume volume;

    private void Reset()
    {
        frameRate = 60;
        frameSkip = 60;
        qualityLevel = 10;
        enableHDR = false;
    }

    void Start()
    {
        postprocessingCamera = Camera.main;
        volume = postprocessingCamera.GetComponent<PostProcessVolume>();
        Application.targetFrameRate = frameRate;
        QualitySettings.SetQualityLevel(qualityLevel);
    }

    void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            QualitySettings.SetQualityLevel(qualityLevel);
            Application.targetFrameRate = frameRate;
            volume.enabled = enableHDR;
        }
    }
}
