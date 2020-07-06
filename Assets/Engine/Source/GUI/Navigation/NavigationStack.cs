using System.Collections.Generic;
using UnityEngine;

public class NavigationStack : Singleton<NavigationStack>
{
    public GameObject startMenu;
    public GameObject gameMenu;
    public GameObject background;
    [Space(10)]
    public GameObject hud;
    public Stack<GameObject> stack;
    public bool startEnabled = false;

    protected NavigationStack() { }
    protected bool menuActive;
    protected bool hudActive;
    protected List<GameObject> viewControllers;
    protected GameObject player;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        viewControllers = new List<GameObject>();
        stack = new Stack<GameObject>();
    }

    private void OnEnable()
    {
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

    internal void PushView(GameObject view)
    {
        Time.timeScale = 1f;

        if (background != null) background?.SetActive(true);
        if (stack.Count > 0) stack.Peek()?.SetActive(false);
        
        stack.Push(view);
        stack.Peek().SetActive(true);
        menuActive = true;
        Time.timeScale = 0f;
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
        Time.timeScale = 1f;
        menuActive = false;
        hudActive = false;
        hud.SetActive(true);
        foreach (GameObject view in viewControllers) view.SetActive(false);
        stack.Clear();
    }

    private void Update()
    {
        if (!menuActive)
        {
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetButtonDown("Back"))
            {
                hud.SetActive(!hud.activeSelf);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        else if (stack.Peek() != startMenu && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetButtonDown("Back"))) CloseMenu();
    }
}
