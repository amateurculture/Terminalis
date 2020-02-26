using UnityEngine;
using System.Collections;

/// <summary>
/// A script that controls the wind. This should be attached to
/// the empty that has the WindZone component.
/// </summary>
public class WindControl : Singleton<WindControl>
{
    //A script that allows the WindZone component to be controlled through scripting.
    //Code provided through this thread: https://forum.unity.com/threads/access-terrain-wind-setting-and-wind-zone-in-unity3d-3-0-02f.60593/.
    private ScriptableWindzoneInterface WindZone;
    //The Transform component of this game object.
    private Transform newTransform;

    public float curvePos = .1f;
    public float snowLevel = .1f;

    void OnEnable()
    {
        //Add ScriptableWindzoneInterface to gain access to WindZone properties.
        //The script doesn't work when demoing using a webplayer so all wind is turned off when doing so.
#if !UNITY_WEBPLAYER
        WindZone = gameObject.AddComponent(typeof(ScriptableWindzoneInterface)) as ScriptableWindzoneInterface;
        WindZone.Init();
#endif
        newTransform = GetComponent<Transform>();
        //SceneManager.instance.OnNewMin += MinUpdate;
        //SceneManager.instance.OnNewDay += RandomizeWindiness;
        MinUpdate();
        RandomizeWindiness();
    }

    /// <summary>
    /// Every minute change the wind's direction.
    /// </summary>
    void MinUpdate()
    {
        ChangeDirection();
    }

    /// <summary>
    /// Changes the direction of the wind.
    /// </summary>
    public void ChangeDirection(Vector3 newDirection = default(Vector3))
    {
        //If nothing is inputted then randomize the direction. Y-axis is zeroed out
        //because vertical wind looks weird.
        if (newDirection == default(Vector3))
        {
            newDirection = Random.insideUnitSphere;
            newDirection.y = 0;
            newDirection = newDirection.normalized;
        }
        StopAllCoroutines();
        //Start the transition.
        StartCoroutine(ChangeDirectionRoutine(newDirection));
    }

    //How fast the wind should take to transition from one direction to another.
    public float directionChangeSpeed;
    //The current direction of the wind.
    public Vector3 direction
    {
        get { return _direction; }
    }
    private Vector3 _direction;

    //Gradually transitions the direction of the wind to the one inputted.
    IEnumerator ChangeDirectionRoutine(Vector3 newDirection)
    {
        Quaternion initRotation = newTransform.rotation;
        Quaternion goalRotation = Quaternion.LookRotation(newDirection);
        while (initRotation != goalRotation)
        {
            newTransform.rotation = Quaternion.RotateTowards(newTransform.rotation, goalRotation, directionChangeSpeed * Time.deltaTime);
            _direction = newTransform.forward;
            yield return null;
        }
    }

    //Two curves indicating the minimum and maximum windiness over the year.
    public AnimationCurve minWindOverYear, maxWindOverYear;
    //A curve indicating how windy is usually is over the year.
    public AnimationCurve likelyWindinessOverYear;
    //A curve indicating how much influence the likelyWindinessOverYear curve should have.
    public AnimationCurve likelyInfluence;
    public float maxDailyWindiness
    {
        get { return _maxDailyWindiness; }
    }
    private float _maxDailyWindiness;

    /// <summary>
    /// Randomizes how windy it should be for the day.
    /// </summary>
    void RandomizeWindiness()
    {
        //Get a normalized value for how windy is normally is around this time of year.
        float likelyWindiness = likelyWindinessOverYear.Evaluate(curvePos);
        //Randomize how influential likelyWindiness will be in the calculation.
        float influence = likelyInfluence.Evaluate(Random.value);
        //Get the minimum windiness for this time of year. Lower the value based on how much
        //snow there is. This is done to avoid frozen trees without leaves flinging around.
        float minWindiness = minWindOverYear.Evaluate(curvePos);
        minWindiness = Mathf.Lerp(minWindiness, 0, snowLevel);
        float maxWindiness = maxWindOverYear.Evaluate(curvePos);
        _maxDailyWindiness = maxWindiness;
        //Get a random value between the min and max wind levels and then factor in the likely windiness.
        float randomWindiness = Mathf.Lerp(minWindiness, maxWindiness, Random.value);
        float windiness = Mathf.Lerp(randomWindiness, likelyWindiness, influence);
        windiness = Mathf.Clamp(windiness, minWindiness, maxWindiness);
        //Finally, apply the wind value.
        SetValues(windiness);
    }

    public Terrain terrain;
    //The wind value of the WindZone component when windiness is set to 0;
    public float minMainWind;
    //The wind value of the WindZone component when windiness is set to 1;
    public float maxMainWind;
    //The turbulence value of the WindZone component when windiness is set to 0;
    public float minTurbulence;
    //The turbulence value of the WindZone component when windiness is set to 1;
    public float maxTurbulence;
    //The waving grass amount on the terrain when windiness is set to 0;
    public float minGrassWave;
    //The waving grass amount on the terrain when windiness is set to 1;
    public float maxGrassWave;
    //A normalized value representing how windy it is.
    public float windiness
    {
        get { return _windiness; }
    }
    private float _windiness;

    /// <summary>
    /// Applies wind changes based on windiness inputted.
    /// </summary>
    void SetValues(float newWindiness)
    {
        float turbulence = Mathf.Lerp(minTurbulence, maxTurbulence, newWindiness);
        float mainWind = Mathf.Lerp(minMainWind, maxMainWind, newWindiness);
        WindZone.WindMain = mainWind;
        WindZone.WindTurbulence = turbulence;
        TerrainData terrainData = terrain.terrainData;
        terrainData.wavingGrassAmount = Mathf.Lerp(minGrassWave, maxGrassWave, newWindiness);
        _windiness = newWindiness;
    }
}