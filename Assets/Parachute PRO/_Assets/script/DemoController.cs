using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// This simple Demo Script shows how to use Parachute and Character controllers
/// </summary>
public class DemoController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] Character character;
    [SerializeField] ParachuteController parachute;

    [Space(10)]

    [Header("UI")]
    [SerializeField] Button btnOpenParachute;
    [SerializeField] Button btnDropParachute;



    void Start ()
    {
        // Change Physics update interval to get more stable behaviour
        Time.fixedDeltaTime = 0.005f;
        
        // Button 'Open' Listener
        btnOpenParachute.onClick.AddListener(()=> 
        {
            character.PlugInParachute(true); // logical
            parachute.Open(); // visual
        });

        // Button 'Drop' Listener
        btnDropParachute.onClick.AddListener(() =>
        {
            character.PlugInParachute(false); // logical
            parachute.Drop(); // visual
        });

        // Place parachute inside character (to move together)
        parachute.transform.parent = character.transform;

        // Ignore collision between backpack and character (to avoid visual bugs)
        Collider collCharacter = character.GetComponent<Collider>();
        Collider collBackpack = parachute.transform.Find("collider").GetComponent<Collider>();
        Physics.IgnoreCollision(collCharacter, collBackpack, true);
    }
}