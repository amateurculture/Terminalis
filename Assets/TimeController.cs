using UnityEngine;

public class TimeController : MonoBehaviour
{
    public float DayCounter; //Counts the amount of days that have passed

    public float DayTimeLength; //Sets how long each day lasts for. Higher values make the day go faster
    public float NightTimeLength; //Sets how long each night lasts for. Higher values make the night go faster

    public bool isDayTime; //On off Switch for Day time. Can use this so that things have different attributes during the day
    public bool isNightTime; //On off Switch for Night time. Can use this so that things have different attributes during the Night

    private bool EndOfDay; //Variable used to isolate the point in time when the next day will start 
    bool nextDay; //A check to see if it's time to move onto the next day.

    public Gradient fogColor;
    public Gradient ambientLight;
    public Gradient sunLight;
    Light sun;
    public int frameSkip = 50;

    private void Start()
    {
        sun = GetComponent<Light>();
    }

    void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            //Grabs the rotation of the Directional Light. These rotations are the period of time that there is light, creating the Day cycle
            if (transform.eulerAngles.x > 0 && transform.eulerAngles.x < 180)
            {
                isNightTime = false;
                isDayTime = true;
            }

            //Grabs the rotation of the Directional Light. These rotations are the period of time that there is darkness, creating the Night cycle
            else if (transform.eulerAngles.x > 180 && transform.eulerAngles.x < 360)
            {
                isDayTime = false;
                isNightTime = true;
            }

            //While the Directional Light is in the Day Cycle it'll rotate the camera on it's x axis to progress time at the speed of the Day Cycle.
            if (isDayTime)
                transform.Rotate(frameSkip * DayTimeLength * Time.deltaTime, 0, 0);

            //While the Directional Light is in the Day Cycle it'll rotate the camera on it's x axis to progress time at the speed of the Night Cycle.
            if (isNightTime)
                transform.Rotate(frameSkip * NightTimeLength * Time.deltaTime, 0, 0);


            //Change fog color based on time of day
            RenderSettings.fogColor = fogColor.Evaluate(transform.eulerAngles.x / 360);
            RenderSettings.ambientLight = ambientLight.Evaluate(transform.eulerAngles.x / 360);
            sun.intensity = sunLight.Evaluate(transform.eulerAngles.x / 360).grayscale;

            //Function for increasing the DayCounter Variable
            IncreaseNumberOfDaysSurvived();
        }
    }

    void IncreaseNumberOfDaysSurvived()
    {
        //Finds rough rotation in which we want to end a full day (midnight).
        if (transform.eulerAngles.x > 270 && transform.eulerAngles.x < 280)
        {
            EndOfDay = true;
        }
        else
        {
            nextDay = false;
            EndOfDay = false;
        }

        //If it's the end of the day but not the next day yet then we make the transition to the next day and increase the DayCounter to the next day.
        if (EndOfDay && !nextDay)
        {
            nextDay = true;
            DayCounter += 1;
        }
    }
}
