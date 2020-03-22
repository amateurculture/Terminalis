using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrbitParamPanel : MonoBehaviour {

    public Text apogee_text;
    public Text perigee_text;
    public Text e_text;
    public Text i_text;
    public Text UOmega_text;
    public Text LOmega_text;

    public OrbitPredictor orbitPredictor;
    public GameObject orbitPanel;

	// Use this for initialization
	void Start () {
        orbitPredictor.gameObject.SetActive(true);
        orbitPanel.SetActive(true);
    }

    void OnEnabled() {
        Debug.Log(enabled);
        orbitPredictor.gameObject.SetActive(true);
        orbitPanel.SetActive(true);
    }

    void OnDisabled() {
        orbitPredictor.gameObject.SetActive(false);
        orbitPanel.SetActive(false);
    }

    // Update is called once per frame

    // Better with a monospace font, but don't want to import extra fonts into the asset
    private static string apogee_str  = "      Apogee: {0:00.00}";
    private static string perigee_str = "     Perigee: {0:00.00}";
    private static string ecc_str     = "Eccentricity: {0:00.00}";
    private static string incl_str    = " Inclination: {0:00.00}";
    private static string Omega_str   = "       Omega: {0:00.00}";
    private static string omega_str   = "       omega: {0:00.00}";

    void Update () {
        OrbitUniversal orbitU = orbitPredictor.GetOrbitUniversal();
        float apogee = (float) orbitU.GetApogee();
        apogee_text.text = string.Format(apogee_str, apogee);
        if (orbitU.eccentricity < 1f)
        {
            float perigee = (float) orbitU.GetPerigee();
            perigee_text.text = string.Format(perigee_str, perigee);
        } else
        {
            perigee_text.text = string.Format("     Perigee: N/A");
        }
        e_text.text = string.Format(ecc_str, orbitU.eccentricity);
        i_text.text = string.Format(incl_str, orbitU.inclination);
        UOmega_text.text = string.Format(Omega_str, orbitU.omega_uc);
        LOmega_text.text = string.Format(omega_str, orbitU.omega_lc);
    }
}
