using System.Collections;
using UnityEngine;

public class TrafficLights : MonoBehaviour {
	
	public GameObject Red;
	public GameObject Yellow;
	public GameObject Green;
	public float redTime = 20; 
	public float greenTime = 30; 
	public float yellowTime = 1; 

	void Start ()
	{
		Red.SetActive (true);
		Yellow.SetActive (false);
		Green.SetActive (false);
	    StartCoroutine(Pos_1());
	}

	IEnumerator Pos_1()
	{
		yield return new WaitForSeconds(redTime);
		Red.SetActive(false);
		Green.SetActive(true);

		yield return new WaitForSeconds(greenTime);
		Green.SetActive(false);
		Yellow.SetActive(true);

		yield return new WaitForSeconds(yellowTime);
		Yellow.SetActive(false);
		Red.SetActive(true);

		StartCoroutine(Pos_1());
	}
}
