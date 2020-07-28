using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.SceneManagement;

/// <summary>
/// Brain
/// 
/// Author: Fiona Schultz
/// Last modified: Oct-12-2019
///
/// It's like, the brain. Man, woman, or something, you don't know!
/// 
/// </summary>

[System.Serializable]
public class Brain : MonoBehaviour
{
    public static Brain instance;
    public Globals.Government government = Globals.Government.Democracy;
    public Gradient colorGradient;
    public Color textColor;
    public Color buttonTextColor;
    public Color hoverColor;
    public Color equippedColor;
    public Color unusableColor;
    public TextAsset maleNames;
    public TextAsset femaleNames;
    public TextAsset lastNames;
    public Texture2D cursorTexture;

    [HideInInspector] public Agent player;
    [HideInInspector] public string sceneName = "Main";
    [HideInInspector] public Color colorCycle1;
    [HideInInspector] public Color colorCycle2;
    [HideInInspector] public List<Agent> automataList;
    [HideInInspector] public Globals.ShowStatusFlags showStatistics;
    [HideInInspector] public Scene currentScene;
    [HideInInspector] public Scene baseScene;
    private float incrementer;
    private float fastIncrementer;
    private bool isNew;

    private void Reset()
    {
        name = "Brain";
        tag = "Brain";
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
            Destroy(gameObject);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.GetComponent<Agent>();
        automataList.AddRange(FindObjectsOfType<Agent>());
        if (cursorTexture != null) Cursor.SetCursor(cursorTexture, new Vector2(32f, 155f), CursorMode.Auto);

        Time.timeScale = 1;
    }    

    public void TrackableEvent(Meme meme) 
    {
        /*
        foreach (Agent a in automataList)
        {
            float distance = Vector3.Distance(meme.dobj.transform.position, a.transform.position);
            
            if (distance < a.hearingRange)
                a.AddMemory(meme);
            
            if (distance < a.sightRange)
            {
                var targetDir = a.transform.position - player.transform.position;
                var angle = Vector3.Angle(targetDir, transform.forward);
                if (angle < a.fov) a.AddMemory(meme);
            }
        }
        */
    }

    private void OnEnable()
    {
        //SceneManager.sceneLoaded += OnSceneLoaded;
        //SceneManager.sceneUnloaded += OnSceneUnloaded;
       
        /*
        e.SaveProfile();
        e.ApplyProfile(p);
        */
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == baseScene.name) return;

        instance.currentScene = scene;
        instance.sceneName = scene.name;

        if (isNew)
        {
            var startMarker = GameObject.Find("Start");
            var pos = startMarker.transform.position;
            pos.y += 5f;

            if (startMarker != null && instance.player != null)
            {
                instance.player.GetComponent<CharacterController>().enabled = false;
                instance.player.transform.position = pos;
                instance.player.GetComponent<CharacterController>().enabled = true;
            }
        }
        NavigationStack.Instance.CloseMenu();
    }

    void OnSceneUnloaded(Scene scene)
    {
        Resources.UnloadUnusedAssets();
        instance.sceneName = Serializer.Load();
        SceneManager.LoadScene(Brain.instance.sceneName, LoadSceneMode.Single);
    }

    public void LoadScene(string sceneName, bool isNewGame)
    {
        this.isNew = isNewGame;

        if (sceneName == "") SceneManager.LoadScene(Serializer.Load());
        else
            SceneManager.LoadScene(sceneName);
    }

    private void Update()
    {
        if (Time.frameCount % 3 == 0)
        {
            incrementer += Time.unscaledDeltaTime * .05f;
            incrementer = incrementer > 1f ? 0 : incrementer;
            colorCycle1 = colorGradient.Evaluate(incrementer);

            fastIncrementer += Time.unscaledDeltaTime * .5f;
            fastIncrementer = fastIncrementer > 1f ? 0 : fastIncrementer;
            colorCycle2 = colorGradient.Evaluate(fastIncrementer);

            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public string getFemaleName()
    {
        string firstName;
        string[] records;
        records = femaleNames.text.Split('\n');
        firstName = records[Random.Range(0, records.Length)].Split(' ')[0];
        return firstName;
    }

    public string getMaleName()
    {
        string firstName;
        string[] records;
        records = maleNames.text.Split('\n');
        firstName = records[Random.Range(0, records.Length)].Split(' ')[0];
        return firstName;
    }

    public string getSurname()
    {
        string lastName;
        string[] records;
        records = lastNames.text.Split('\n');
        lastName = records[Random.Range(0, records.Length)].Split(' ')[0];
        return lastName;
    }

    public string getFullname(int gender)
    {
        string firstName = "";
        switch (gender)
        {
            case 0: firstName = getMaleName(); break;
            case 1: firstName = getFemaleName(); break;
            case 2: firstName = Random.Range(0, 1) == 0 ? getMaleName() : getFemaleName(); break;
            default: break;
        }
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase((firstName + " " + getSurname()).ToLower());
    }
}
