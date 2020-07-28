using UnityEngine;
using System.Collections;

public class PoliceLights : MonoBehaviour {



    public Light[] RedLights;
    public Light[] BlueLights;
	public bool active;
	public float time = 20;

	private float timer=0.0f;
	private int lightNum = 0;

    enum LightsMode {Active=1 , Inactive=2}
    private LightsMode lightsMode = LightsMode.Inactive;
	public AudioSource audiosource;

	void Start () {

		audiosource = GetComponent<AudioSource> ();

		if(audiosource == null)
			audiosource = gameObject.AddComponent<AudioSource>();

        if (lightsMode == LightsMode.Inactive)
        {
			Active ();

        }
        else if (lightsMode == LightsMode.Active)
        {
            lightsMode = LightsMode.Inactive;
		}
	}

	void Active () {
		
		if (lightsMode == LightsMode.Inactive)
			
		{
			lightsMode = LightsMode.Active;

		}
		else if (lightsMode == LightsMode.Active)
		{
			lightsMode = LightsMode.Inactive;
		}
	}
	void FixedUpdate () {


        if (lightsMode == LightsMode.Active)
        {
            timer = Mathf.MoveTowards(timer, 0.0f, Time.deltaTime * time);


            GetComponent<AudioSource>().enabled = true;

            if (timer == 0)
            {
                lightNum++;
                if (lightNum > 12) { lightNum = 1; }
                timer = 1.0f;
            }





            if (lightNum == 1 || lightNum == 3)
            {

                foreach (Light RedLight in RedLights)
                {
                    RedLight.enabled = true;
                }

                foreach (Light BlueLight in BlueLights)
                {
                    BlueLight.enabled = false;
                }
            }

            if (lightNum == 5 || lightNum == 7)
            {

                foreach (Light BlueLight in BlueLights)
                {
                    BlueLight.enabled = true;
                }

                foreach (Light RedLight in RedLights)
                {
                    RedLight.enabled = false;
                }
            }


            if (lightNum == 2 || lightNum == 4 || lightNum == 6 || lightNum == 8)
            {

                foreach (Light BlueLight in BlueLights)
                {
                    BlueLight.enabled = false;
                }

                foreach (Light RedLight in RedLights)
                {
                    RedLight.enabled = false;
                }
            }

        }
        else
        {
            GetComponent<AudioSource>().enabled = false;

            foreach (Light BlueLight in BlueLights)
            {
                BlueLight.enabled = false;
            }

            foreach (Light RedLight in RedLights)
            {
                RedLight.enabled = false;
            }


        }



	}



}
