using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GUI stack navigation for HUDs, interstitial screens, God camera, and menus.
/// 
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class NavigationStack : Singleton<NavigationStack>
{
    public GameObject startMenu;
    public GameObject gameMenu;
    [Space(10)]
    public GameObject hud;
    public GameObject god;
    public GameObject smartphone;
    public GameObject loadingScreen;
    public GameObject eventMessage;
    public Stack<GameObject> stack;
    public bool startEnabled = false;
    
    protected bool menuActive;
    protected bool hudActive;
    List<GameObject> viewControllers;
    public GameObject player;
    GodCamera godCam;
    bool usingOrbitCam;
    bool previousHud;
    bool previousGod;
    bool inTransition;
    bool inCar;

    protected NavigationStack() { }
    UltimateCharacterLocomotion loco;
    Ability[] abilities;

    private void Awake()
    {
        viewControllers = new List<GameObject>();
        stack = new Stack<GameObject>();
        godCam = Camera.main.GetComponent<GodCamera>();
        previousHud = true;
        loco = player.GetComponent<UltimateCharacterLocomotion>();
        abilities = loco.Abilities;
    }

    private void OnEnable()
    {
        if (startEnabled) Time.timeScale = 0;

        stack = new Stack<GameObject>();

        foreach (Transform child in transform)
        {
            viewControllers.Add(child.gameObject);
            child.gameObject.SetActive(false);
        }

        if (startEnabled)
        {
            hud.SetActive(false);
            PushView(startMenu);
        }
        else 
            CloseMenu();
    }

    IEnumerator DelayedAction(GameObject outlet)
    {
        yield return new WaitForSecondsRealtime(2);

        NavAction n = outlet.GetComponent<NavAction>();
        if (n != null) 
            n.DoAction();

    }

    internal void PushView(GameObject outlet)
    {
        foreach (var f in abilities)
        {
            if (f.ToString().Contains("Jump"))
            {
                f.Enabled = false;
                break;
            }
        }

        inTransition = true;
        previousHud = hud.activeSelf;
        previousGod = god.activeSelf;
        hud.SetActive(false);
        god.SetActive(false);
        if (stack.Count > 0) stack.Peek()?.SetActive(false);
        stack.Push(outlet);
        stack.Peek().SetActive(true);
        menuActive = true;
        inTransition = false;

        StartCoroutine(DelayedAction(outlet));
    }

    public void CompletePop()
    {
        if (stack.Count <= 0) return;

        stack.Pop();

        if (stack.Count > 0)
            stack.Peek()?.SetActive(true);
        else
            CloseMenu();

        inTransition = false;
    }

    internal void PopView()
    {
        if (stack.Count <= 0) return;

        inTransition = true;
        GameObject p = stack.Peek();
        if (p == startMenu) return;

        Transition m = p.GetComponent<Transition>();

        if (m != null) 
            m.Close(); 
        else 
        { 
            p.SetActive(false); 
            CompletePop(); 
        }
    }

    public void CloseMenu()
    {
        inTransition = true;
        menuActive = false;
        hudActive = false;
        hud.SetActive(previousHud);
        god.SetActive(previousGod);
        Cursor.lockState = CursorLockMode.Locked;

        foreach (GameObject view in viewControllers) 
        {
            Transition m = view.GetComponent<Transition>();
            if (m != null) m.Close(); else view.SetActive(false); 
        }
        stack.Clear();
        Time.timeScale = 1;
        inTransition = false;

        foreach (var f in abilities)
        {
            if (f.ToString().Contains("Jump"))
            {
                f.Enabled = true;
                break;
            }
        }
    }

    public void EnterVehicle()
    {
        inCar = true;
        CloseMenu();
        hud.SetActive(false);
    }

    public void ExitVehicle()
    {
        inCar = false;
        hud.SetActive(previousHud);
    }

    void DisableGodCamera()
    {
        QualitySettings.SetQualityLevel(2);
        god.SetActive(false);
        hud.SetActive(previousHud);
        foreach (MonoBehaviour v in player.GetComponents<MonoBehaviour>()) v.enabled = true;

        if (usingOrbitCam)
            godCam.GetComponent<OrbitCam>().enabled = true;
        else
        {
            godCam.GetComponent<CameraController>().enabled = true;
            godCam.GetComponent<CameraControllerHandler>().enabled = true;
        }
        Cursor.lockState = CursorLockMode.Locked;
    }

    void EnableGodCamera()
    {
        CloseMenu(); 
        usingOrbitCam = godCam.GetComponent<OrbitCam>().enabled;
        QualitySettings.SetQualityLevel(0);
        hud.SetActive(false);
        god.SetActive(true);
        foreach (MonoBehaviour v in player.GetComponents<MonoBehaviour>()) v.enabled = false;
        godCam.GetComponent<OrbitCam>().enabled = false;
        godCam.GetComponent<CameraController>().enabled = false;
        godCam.GetComponent<CameraControllerHandler>().enabled = false;
        Vector3 t = Camera.main.transform.position;
        t.y = godCam.height;
        t.x -= godCam.height / 2;
        godCam.transform.position = t;
        godCam.transform.eulerAngles = new Vector3(65, 90, 0);
        Cursor.lockState = CursorLockMode.None;
    }

    bool DidToggleGod()
    {
        return !inCar && !menuActive && player.activeSelf && Input.GetButtonDown("Start");
    }

    bool DidToggleSmartphone()
    {
        return !inCar && !menuActive && !godCam.enabled && !inTransition && Input.GetButtonDown("Back");
    }

    bool DidClosePanel()
    {
        return stack.Count > 0 && stack.Peek() != startMenu && Input.GetButtonDown("Back");
    }

    private void Update()
    {
        if (DidToggleGod())
        {
            if (godCam.enabled = !godCam.enabled) EnableGodCamera(); else DisableGodCamera();
        }
        else if (DidToggleSmartphone())
        {
            if (player.activeSelf && smartphone.activeSelf) CloseMenu(); else PushView(smartphone);
        }
        else if (DidClosePanel()) 
        {
            CloseMenu(); 
        }
    }
}
