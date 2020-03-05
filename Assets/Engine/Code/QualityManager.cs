using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class QualityManager : MonoBehaviour
{
    public ReflectionProbe reflectionProbe;
    [Range(0, 2)] public int qualityLevel;
    [Range(24, 240)] public int frameRate;
    public bool enableReflections;
    public bool enableAmbientOcclusion;
    public bool enableDepthOfField;
    public bool enableTonemapping;
    public bool enableHDR;

    bool previousEnableReflections;
    Camera postprocessingCamera;
    ColorGrading colorGrading;
    DepthOfField depthOfField;
    AmbientOcclusion ambientOcclusion;
    PostProcessVolume postProcessingVolume;
    int frameSkip;

    private void Reset()
    {
        frameRate = 60;
        qualityLevel = 2;
        enableHDR = false;
    }

    void Start()
    {
        frameSkip = 120;
        postprocessingCamera = Camera.main;
        postProcessingVolume = postprocessingCamera.GetComponent<PostProcessVolume>();
        postProcessingVolume.profile.TryGetSettings(out colorGrading);
        postProcessingVolume.profile.TryGetSettings(out depthOfField);
        postProcessingVolume.profile.TryGetSettings(out ambientOcclusion);
        QualitySettings.SetQualityLevel(qualityLevel);

        if (frameRate < 240)
            Application.targetFrameRate = frameRate;
        
        previousEnableReflections = enableReflections;
    }

    void Update()
    {
        // todo this logic will eventually be moved into a game menu and taken out of the update loop
        // todo investigate property drawers
        if (Time.frameCount % frameSkip == 0)
        {
            QualitySettings.SetQualityLevel(qualityLevel);
            Application.targetFrameRate = frameRate;
            colorGrading.active = enableTonemapping;
            depthOfField.active = enableDepthOfField;
            ambientOcclusion.active = enableAmbientOcclusion;
            postprocessingCamera.allowHDR = enableHDR;

            if (reflectionProbe != null && previousEnableReflections != enableReflections)
            {
                reflectionProbe.enabled = enableReflections;
                previousEnableReflections = enableReflections;

                if (enableReflections) reflectionProbe.RenderProbe();
            }
        }
    }
}
