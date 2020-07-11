using Opsive.UltimateCharacterController.Camera;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GUI stack navigation including support for HUDs, interstitial screens, and menus.
/// </summary>

public class NavigationStack : Singleton<NavigationStack>
{
    public GameObject startMenu;
    public GameObject gameMenu;
    public GameObject background;
    [Space(10)]
    public GameObject hud;
    public GameObject god;
    public GameObject loadingScreen;
    public Stack<GameObject> stack;
    public bool startEnabled = false;

    protected NavigationStack() { }
    protected bool menuActive;
    protected bool hudActive;
    protected List<GameObject> viewControllers;
    protected GameObject player;
    protected bool previousHudSetting;
    GodCamera godCam;
    bool usingOrbitCam;
    bool previousHud;
    bool previousGod;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        player = GameObject.FindGameObjectWithTag("Player");
        viewControllers = new List<GameObject>();
        stack = new Stack<GameObject>();
        godCam = Camera.main.GetComponent<GodCamera>();
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
        else CloseMenu();
    }

    IEnumerator actionWait(GameObject outlet)
    {
        yield return new WaitForSeconds(1);

        // If a nav action exists on the panel, call DoAction()
        NavAction n = outlet.GetComponent<NavAction>();
        if (n != null) 
            n.DoAction();
    }

    internal void PushView(GameObject outlet)
    {
        previousHud = hud.activeSelf;
        previousGod = god.activeSelf;
        hud.SetActive(false);
        god.SetActive(false);
        if (background != null) background?.SetActive(true);
        if (stack.Count > 0) stack.Peek()?.SetActive(false);
        stack.Push(outlet);
        stack.Peek().SetActive(true);
        menuActive = true;

        StartCoroutine(actionWait(outlet));
    }

    internal void PopView()
    {
        if (stack.Peek() == startMenu) return;
        stack.Peek()?.SetActive(false);
        stack.Pop();
        if (stack.Count > 0) stack.Peek()?.SetActive(true); else CloseMenu();
    }

    internal void CloseMenu()
    {
        menuActive = false;

        //hudActive = false;
        //hud.SetActive(true);

        hud.SetActive(previousHud);
        god.SetActive(previousGod);
        hudActive = false;
        
        foreach (GameObject view in viewControllers) view.SetActive(false);
        stack.Clear();
        Time.timeScale = 1f;
    }

    public void EnterVehicle()
    {
        hud.SetActive(false);
    }

    public void ExitVehicle()
    {
        hud.SetActive(previousHudSetting);
    }

    private void Update()
    {
        if (Time.frameCount % 30 == 0) Cursor.lockState = CursorLockMode.Locked;

        if (!menuActive)
        {
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetButtonDown("Back"))
            {
                if (!godCam.enabled && player.activeSelf)
                {
                    hud.SetActive(!hud.activeSelf);
                    previousHudSetting = hud.activeSelf;
                }
            }
            else if (Input.GetButtonDown("Start") && player.activeSelf)
            {
                godCam.enabled = !godCam.enabled;

                if (godCam.enabled)
                {
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
                }
                else
                {
                    QualitySettings.SetQualityLevel(6);
                    god.SetActive(false);
                    hud.SetActive(previousHudSetting);
                    foreach (MonoBehaviour v in player.GetComponents<MonoBehaviour>()) v.enabled = true;

                    if (usingOrbitCam)
                        godCam.GetComponent<OrbitCam>().enabled = true;
                    else
                    {
                        godCam.GetComponent<CameraController>().enabled = true;
                        godCam.GetComponent<CameraControllerHandler>().enabled = true;
                    }
                }
            }
        }
        else if (stack.Peek() != startMenu && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetButtonDown("Back"))) CloseMenu();
    }
}
