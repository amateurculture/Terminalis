using UnityEngine;

public class FoliageController : MonoBehaviour
{
    /**
     * Most plants grow to their maximum age and die; plants that do not senesce, will grow forever.
     */

    /***
     * Plant Logic:
     * 
     *   1) Am I old enough to die? if yes die, if no continue
     *   1) Is it the first day of spring and am I outside? if yes spawn 0-n plants, if no continue.
     * X 3) Is it the beginning of the day? if yes grow, if no continue.
     *   4) Do I have enough water to survive? if yes continue, if no die.
     */

    public TimeController timeController;
    
    // Todo Use age and max age for plants, right now this requires time controller to support years which it doesn't currently.
    public int age;
    public int maxAge;

    public float maxScale;
    [Range(0, 1f)] public float ageRate;

    Vector3 adjustedScale;
    float currentDay;

    private void Start()
    {
        currentDay = (timeController != null) ? timeController.day : 0;
    }

    void Update()
    {
        if (timeController != null && timeController.day > currentDay)
        {
            currentDay = timeController.day;

            if (maxScale == -1 || (adjustedScale.x <= maxScale && adjustedScale.y <= maxScale && adjustedScale.z <= maxScale))
            {
                adjustedScale = transform.localScale;
                adjustedScale.x += ageRate;
                adjustedScale.y += ageRate;
                adjustedScale.z += ageRate;
                transform.localScale = adjustedScale;
            }
        }
    }
}
