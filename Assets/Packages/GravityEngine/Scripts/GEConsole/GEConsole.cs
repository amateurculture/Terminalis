using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Command line console for interacting with GE and its objects during game
/// exection.  (Kudos to Jonathan Blow for sharing his developement
/// of this feature in JAI on You Tube, from which the idea comes). 
/// 
/// Console is opened, enlarged and closed via the back-quote key.
/// 
/// Commands can be typed and status information will be displayed in the scrollable window. 
/// 
/// Console Commands:
/// Classes register their own. Use '?' to see a list of those available in a given scene. 
/// 
/// This component is typically used by adding the GEConsole prefab to a scene. 
/// 
/// </summary>
public class GEConsole : MonoBehaviour {

    public GameObject consolePanel;
    public Text historyText;
    public InputField inputField;
    [Tooltip("Start GE in single step mode")]
    [SerializeField]
    private bool startInSingleStep = false;

    private enum ConsoleState  { CLOSED, SMALL, LARGE };
    private ConsoleState consoleState;

    private List<GEConsoleCommand> commands;
    private HashSet<string> declarers; 

    private float smallHeight = 0.2f * Screen.height;
    private float largeHeight = 0.7f * Screen.height;

    private const float widthFactor = 0.9f;

    // @TODO: Make list of strings so can recall, age out etc. 
    private string history = "";

    private RectTransform rt;

    public static void RegisterCommandIfConsole(GEConsoleCommand cmd) {
        GEConsole gec = Instance();
        if (gec != null) {
            gec.RegisterCommand(cmd);
        }

    }

    private static GEConsole instance;

    public static GEConsole Instance() {
        if (instance == null) {
            GEConsole[] consoles = (GEConsole[])Object.FindObjectsOfType(typeof(GEConsole));
            if (consoles.Length > 0) {
                instance = consoles[0];
            }
        }
        return instance;

    }

    // Use this for initialization. Must be at Awake so other classes can register commands
    // when they Start()
    void Awake () {
        consoleState = ConsoleState.CLOSED;
        LoadCommands();
        smallHeight = 0.2f * Screen.height;
        largeHeight = 0.7f * Screen.height;
        rt = consolePanel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(widthFactor * rt.sizeDelta.x, smallHeight);
        if (startInSingleStep) {
            GravityEngine.Instance().EvolveOneFixedUpdate();
        }
    }

    // Update is called once per frame
    void Update () {


        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            switch (consoleState) {
                case ConsoleState.CLOSED:
                    // activate the console
                    consoleState = ConsoleState.SMALL;
                    consolePanel.SetActive(true);
                    rt.sizeDelta = new Vector2( rt.sizeDelta.x, smallHeight);
                    // set focus in the input field
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject, null);
                    inputField.OnPointerClick(new PointerEventData(EventSystem.current));
                    break;
                case ConsoleState.SMALL:
                    // make active console bigger
                    consoleState = ConsoleState.LARGE;
                    rt.sizeDelta = new Vector2( rt.sizeDelta.x, largeHeight);
                    // backquote was added to input filed, remove it
                    inputField.text = inputField.text.Replace("`", "");
                    break;
                case ConsoleState.LARGE:
                    // disable the console
                    consoleState = ConsoleState.CLOSED;
                    consolePanel.SetActive(false);
                    break;
                default:
                    Debug.LogError("Unknown state");
                    break;
            }

        }
        // Want Input field to handle all keystrokes if the console is open
        // Clear any input events. This work since the console is on the UI layer
        // (might need to force this Update to run ahead of others in some cases?)
        if (consoleState != ConsoleState.CLOSED) {
            Input.ResetInputAxes();
        }
	}

    private string RunCommand(string command) {

        string[] words = command.Trim().Split(' ');

        if (words[0] == "?") {
            // show help
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            // @TODO: Fixed width font
            foreach (string declarer in declarers) {
                string title = string.Format("\nClass: {0}\n", declarer);
                sb.Append(title);
                sb.Insert(sb.Length, "=", title.Length-1);
                sb.Append("\n");
                foreach (GEConsoleCommand cmd in commands) {
                    if (cmd.declaringClass != declarer) {
                        continue;
                    }
                    string shortForm = "";
                    if (cmd.names.Length == 2) {
                        shortForm = cmd.names[1];
                    }
                    string[] helpLines = new string[] { "" };
                    if (cmd.help != null) {
                        helpLines = cmd.help.Split('\n');
                    }
                    // offset any remaining help lines past command and short-cut columns
                    sb.Append(string.Format("{0,20}  {1,5}   {2}\n", cmd.names[0], shortForm, helpLines[0]));
                    if (helpLines.Length > 1) {
                        for (int i = 1; i < helpLines.Length; i++) {
                            sb.Append(string.Format("                             {0}\n", helpLines[i]));
                        }
                    }
                }
            }
            return sb.ToString();
        }

        foreach ( GEConsoleCommand cmd in commands) {
            foreach (string name in cmd.names) {
                if (name == words[0]) {
                    return cmd.Run(words);
                }
            }
        }
        return string.Format("Unknown command :{0}:\n ? for help", words[0]);
    }

    public void OnInputChange(string s) {

    }

    // Callback for InputField
    public void EndEdit(string s) {

        history += "<color=red>" + inputField.text + "</color>\n";
        historyText.text = history;
        string output = RunCommand(inputField.text); 
        // add output to history
        history += "<color=yellow>" + output + "</color>";
        historyText.text = history;
        inputField.text = "";
        // reset focus so can keep typing
        if (EventSystem.current.currentSelectedGameObject != inputField.gameObject) {
            EventSystem.current.SetSelectedGameObject(inputField.gameObject, null);
        }
        inputField.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    public void AddToHistory(string info) {
        history += "<color=yellow>" + info + "</color>";
        historyText.text = history;
    }

    private void RegisterCommand(GEConsoleCommand cmd) {
        cmd.declaringClass = cmd.GetType().DeclaringType.Name;
        declarers.Add(cmd.declaringClass);
        commands.Add(cmd);
    }

    //==========================================================================================

    /// <summary>
    /// Commands for the console
    /// </summary>
    public abstract class GEConsoleCommand
    {
        //! Full name first (one that will be used when help is displayed)
        public string[] names;
        //! Help string (shown when '?' is entered)
        public string help;
        //! Name of declaring class (will be filled in automatically)
        public string declaringClass; 
        //! Function to be run by the console, returning the output to be displayed in the console. 
        public abstract string Run(string[] args);
    }

    // classes in a scene generally register their own commands (see e.g. GravityEngine)
    private void LoadCommands() {
        commands = new List<GEConsoleCommand>();
        declarers = new HashSet<string>();
    }


}
