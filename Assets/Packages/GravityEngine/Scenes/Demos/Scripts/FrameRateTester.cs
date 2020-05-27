using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FrameRateTester : MonoBehaviour {

	public Text frameText; 
	public AddDeleteTester addDelete;
    public bool masslessMode; 

	const int framesBetweenAdds = 25;

	const float TARGET_RATE = 60f;

	private int frameCount; 
	private int numBodies = 0; 

	bool stop; 

	private float fps = 60f;
	private int lowFrameCount = 0; 
	const int LOW_FRAME_LIMIT = 40;

    void Awake() {
        
    }

    // Update is called once per frame
    // System will try and keep frame rate at 60 
    // If below frame rate for 10 frames - call it
    void Update () {
		if (stop)
			return;

		if (frameCount++ > framesBetweenAdds) {
            if (masslessMode) {
                addDelete.AddBody("massless");
            } else {
                addDelete.AddBody("massive");
            }
            frameCount = 0;
			numBodies++;
		}
		// time average the fps a bit
        fps = 0.6f*(1.0f / Time.deltaTime) + 0.4f*fps;
		frameText.text = string.Format("N={0}  Rate={1}",numBodies, Mathf.Ceil (fps).ToString ());
		if (fps < TARGET_RATE) {
		 	if (lowFrameCount++ > LOW_FRAME_LIMIT) {
				stop = true;
				frameText.text = string.Format("Final: N={0}  Rate={1}",numBodies, Mathf.Ceil (fps).ToString ());
			}
		} else {
			lowFrameCount = 0;
		}
    }
}
