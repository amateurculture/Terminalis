/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

//#define DEBUG_MESSAGES

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public enum WindowTab { None, Lines, Settings }

public class EasyVoiceEditorWindow : EditorWindow
{
    public static EasyVoiceEditorWindow window;

    
    public EasyVoiceSettings settings;


    private WindowTab currentTab;

    private AudioSource clipPlayer;


    /// <summary>
    /// 
    /// </summary>
    [MenuItem("Window/Easy Voice")]
    private static void Init()
    {
#if DEBUG_MESSAGES
        Debug.Log("Window initializing");
#endif

        Open();

        // Note that this only gets triggered when the windows is created, not deserialized when Unity starts with the window already open
        // In that case, the data/settings assets are already assigned as before and the data check won't see any nulls and won't initialize anything
        // This is why the below Update() detects when it is called for the first time and makes sure the data/settings are actually initialized
    }

    public static void Open()
    {
        // Get existing open window or if none, make a new one
        window = GetWindow<EasyVoiceEditorWindow>("Easy Voice", typeof(SceneView));
    }

    private static Texture2D backgroundTextureDark;
    private static Texture2D backgroundTextureLight;
    private static Texture2D backgroundBoxTextureDark;
    private static Texture2D backgroundBoxTextureLight;
    private static Texture2D backgroundBoxHighlightedTextureDark;
    private static Texture2D backgroundBoxHighlightedTextureLight;
    private static Texture2D logoTexture;
    private static Texture2D okayIconTexture;
    private static Texture2D warningIconTexture;
    private static Texture2D errorIconTexture;
    private static Texture2D dragLineTextureLight;
    private static Texture2D dragLineTextureDark;
    private static Texture2D unlinkTextureLight;
    private static Texture2D unlinkTextureDark;
    private static Texture2D playTextureLight;
    private static Texture2D playTextureDark;
    private static Texture2D stopTextureLight;
    private static Texture2D stopTextureDark;
    private static Texture2D refreshTextureLight;
    private static Texture2D refreshTextureDark;

    private void OnEnable()
    {
        backgroundTextureDark = (Texture2D)Resources.Load("EasyVoiceBackgroundDark", typeof(Texture2D));
        backgroundTextureLight = (Texture2D)Resources.Load("EasyVoiceBackgroundLight", typeof(Texture2D));
        backgroundBoxTextureDark = (Texture2D)Resources.Load("EasyVoiceBoxBackgroundDark", typeof(Texture2D));
        backgroundBoxTextureLight = (Texture2D)Resources.Load("EasyVoiceBoxBackgroundLight", typeof(Texture2D));
        backgroundBoxHighlightedTextureDark = (Texture2D)Resources.Load("EasyVoiceBoxHighlightedBackgroundDark", typeof(Texture2D));
        backgroundBoxHighlightedTextureLight = (Texture2D)Resources.Load("EasyVoiceBoxHighlightedBackgroundLight", typeof(Texture2D));
        logoTexture = (Texture2D)Resources.Load("EasyVoiceLogo", typeof(Texture2D));
        okayIconTexture = (Texture2D)Resources.Load("EasyVoiceOkayIcon", typeof(Texture2D));
        warningIconTexture = (Texture2D)Resources.Load("EasyVoiceWarningIcon", typeof(Texture2D));
        errorIconTexture = (Texture2D)Resources.Load("EasyVoiceErrorIcon", typeof(Texture2D));
        dragLineTextureLight = (Texture2D)Resources.Load("EasyVoiceDragLineIconLight", typeof(Texture2D));
        dragLineTextureDark = (Texture2D)Resources.Load("EasyVoiceDragLineIconDark", typeof(Texture2D));
        unlinkTextureLight = (Texture2D)Resources.Load("EasyVoiceUnlinkIconLight", typeof(Texture2D));
        unlinkTextureDark = (Texture2D)Resources.Load("EasyVoiceUnlinkIconDark", typeof(Texture2D));
        playTextureLight = (Texture2D)Resources.Load("EasyVoicePlayIconLight", typeof(Texture2D));
        playTextureDark = (Texture2D)Resources.Load("EasyVoicePlayIconDark", typeof(Texture2D));
        stopTextureLight = (Texture2D)Resources.Load("EasyVoiceStopIconLight", typeof(Texture2D));
        stopTextureDark = (Texture2D)Resources.Load("EasyVoiceStopIconDark", typeof(Texture2D));
        refreshTextureLight = (Texture2D)Resources.Load("EasyVoiceRefreshIconLight", typeof(Texture2D));
        refreshTextureDark = (Texture2D)Resources.Load("EasyVoiceRefreshIconDark", typeof(Texture2D));

#if DEBUG_MESSAGES
        Debug.Log("OnEnable");
#endif
        if (settings == null)
        {
            FindOrCreateSettings();
            //if (Application.platform == RuntimePlatform.OSXPlayer ||
            //    Application.platform == RuntimePlatform.OSXDashboardPlayer ||
            //    Application.platform == RuntimePlatform.OSXEditor ||
            //    Application.platform == RuntimePlatform.OSXWebPlayer)
            //{
            //    try
            //    {
            //        EditorUtility.DisplayProgressBar("Initializing...", "Please hold tight while we get the plugin ready for your OS X.", 0f);
            //        string fullPath = settings.GetFullClipDefaultPath();
            //        Directory.Exists(fullPath);
            //    }
            //    finally
            //    {
            //        EditorUtility.ClearProgressBar();
            //    }
            //}
        }

        if (settings != null)
        {
            settings.Initialize(); // they shouldn't change them though

            if (settings.data == null)
                FindOrCreateData();
            else
            {
                AssignDataAssetToSettings(settings.data);
            }
        }
    }

    private void AssignDataAssetToSettings(EasyVoiceDataAsset givenData)
    {
        settings.AssignDataAsset(givenData);
        EasyVoiceIssueChecker.CheckAllLineIssues();
    }

    private void OnGUI()
    {
        int pixelScale = (int)EditorGUIUtility.pixelsPerPoint;

        // == LAYOUT WIDTH SIZING/SPACING == //
        spacingInfo = new SpacingInfo(
            Screen.width / pixelScale, 
            Screen.height / pixelScale); // TODO: only during layout pass?

        if (settings != null)
        {
            // Verify that our settings instance is set
            if (EasyVoiceSettings.instance == null)
                settings.SetInstance();

            if (settings.data != null)
            {
                if (settings.data.VerifyIntegrity())
                {
                    // assign while we are in editor namespace
                    settings.verificationActions = PullVerificationFunctions();

                    OnGUI_Tabs();

                    switch (currentTab)
                    {
                        case WindowTab.Lines:
                            OnGUI_LinesTab();
                            EditorUtility.SetDirty(settings.data);
                            break;

                        case WindowTab.Settings:
                            OnGUI_SettingsTab();
                            EditorUtility.SetDirty(settings);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    OnGUI_IntegrityCheckFail();                    
                }
            }
            else
            {
                OnGUI_FullDataAssetSelection();
            }
        }
        else
        {
            OnGUI_SettingsAssetSelection();
        }
    }

    private void FindOrCreateSettings()
    {
#if DEBUG_MESSAGES
        Debug.Log("Finding or creating settings");
#endif

        EasyVoiceSettings foundAsset = (EasyVoiceSettings)AssetDatabase.LoadAssetAtPath(EasyVoiceSettings.settingAssetName, typeof(EasyVoiceSettings));
        if (foundAsset != null)
        {
#if DEBUG_MESSAGES
            Debug.Log("Found settings asset");
#endif
            settings = foundAsset;
            //settings.Hide();
            EditorUtility.SetDirty(settings);
        }
        else
        {
            settings = ScriptableObject.CreateInstance<EasyVoiceSettings>();
            if (settings != null)
            {
                //settings.Hide();
                AssetDatabase.CreateAsset(settings, EasyVoiceSettings.settingAssetName);
#if DEBUG_MESSAGES
                Debug.Log("Created settings asset");
#endif
                settings.Initialize();
                EditorUtility.SetDirty(settings);
            }
        }
    }

    private WindowTab lastTab = WindowTab.None;

    private void OnGUI_Tabs()
    {
        if (currentTab == WindowTab.None)
            currentTab = WindowTab.Lines;

        // Tile the background texture
        Texture2D texture = EditorGUIUtility.isProSkin ? backgroundTextureDark : backgroundTextureLight;
        if (texture != null)
            GUI.DrawTextureWithTexCoords(
                new Rect(0, 0, Screen.width, Screen.height),
                texture,
                new Rect(0, 0, Screen.width / (float)texture.width, Screen.height / (float)texture.height),
                false);

        // Draw main menu

        GUI.BeginGroup(new Rect(2, 2, spacingInfo.totalWidth, SpacingInfo.menuHeight), "");

        if (logoTexture != null)
            GUI.DrawTexture(new Rect(0, 0, 33, 21), logoTexture);


        int toolbarButtonWidth = (spacingInfo.totalWidth - 40) / 2;

        GUI.SetNextControlName("LinesToggle"); // So we can "focus" it
        bool linesToggle = GUI.Toggle(new Rect(40, 2, toolbarButtonWidth, 18), currentTab == WindowTab.Lines, "Lines", OurStyles.ToolbarButton);
        if (linesToggle)
        {
            currentTab = WindowTab.Lines;
            if (currentTab != lastTab) // Make sure this only happens once -- when actual state is switched, future toggle re-activations don't count
            {
                GUI.FocusControl("LinesToggle"); // Make sure we don't mess up focused field values
            }
        }

        GUI.SetNextControlName("SettingsToggle"); // So we can "focus" it
        bool settingsToggle = GUI.Toggle(new Rect(40 + toolbarButtonWidth + 2, 2, toolbarButtonWidth, 18), currentTab == WindowTab.Settings, "Settings", OurStyles.ToolbarButton);
        if (settingsToggle)
        {
            currentTab = WindowTab.Settings;
            deleteConfirmActive = false; // no delete confirmations
            if (currentTab != lastTab) // Make sure this only happens once -- when actual state is switched, future toggle re-activations don't count
            {
                GUI.FocusControl("SettingsToggle"); // Make sure we don't mess up focused field values
            }
        }

        GUI.EndGroup();

        lastTab = currentTab;
    }

    private bool playingPreviewClip;

    private bool deleteConfirmActive;
    private int deleteConfirmIndex;
    private bool justCreatedLine;
    private int justCreatedLineIndex;
    private bool forceNoPlaceholderFocus;
    private int forceNoPlaceholderFocusIndex;
    private Vector2 linesScrollPosition;

    private string lastFocusedControl;

    /// <summary>
    /// This is a helper class/struct to manage all the sizing and spacing values for the window
    /// </summary>
    private struct SpacingInfo
    {
        // Fixed widths (identical for any size)
        public const int menuHeight = 20;
        public const int headerHeight = 16;
        public const int footerHeight = 30;
        public const int lineHeight = 20;
        private const int controlHeight = lineHeight - 4;
        public const int lineSpacing = 2;
        private const int controlSpacingX = 4;
        private const int controlSpacingY = 2;
        

        public readonly int totalWidth;

        private readonly int lineAlocatedWidth;
        private readonly int lineAlocatedFlexibleWidth;

        private readonly int totalHeight;


        public const int dragWidth = 16;

        public const int flagWidth = 16;
        private const int flagOffset = dragWidth + controlSpacingX;

        private const float speakerNameOffset = dragWidth + controlSpacingX + flagWidth + controlSpacingX;
        public readonly float speakerNamePopupWidth;
        private readonly float speakerNameButtonWidth;
        private readonly float speakerNameTextWidth;

        private readonly float speechTextOffset;
        public readonly float speechTextWidth;

        private readonly float fileNameOffset;
        public readonly float fileNameTextWidth;
        private readonly float fileNameClipFieldOffset;
        private readonly float fileNamePlayButtonWidth;
        private readonly float fileNameClipButtonWidth;
        private readonly float fileNameClipButtonOffset;
        private readonly float fileNameClipFieldWidth;

        private readonly float controlOffset;
        public const int controlWidth = 100;
        private const int deleteButtonWidth = controlWidth; // 50 - controlSpacingX;
        private const float deleteConfirmButtonWidth = controlWidth * 0.5f - controlSpacingX / 2;
        private const float deleteCancelButtonWidth = controlWidth * 0.5f - controlSpacingX / 2;
        //private const int moveLineButtonWidth = (controlWidth - deleteButtonWidth) / 2 - controlSpacingX;


        public SpacingInfo(int givenAvailableWidth, int givenAvailableHeight)
        {
            // |- Drag -|- Flag -|- Speaker (flex) -|- Speech (flex) -|- File name (flex) -|- Controls -|

            // |- Drag -|- Flag -|- Popup          -|- Text          -|- Clip | But       -|- Delete   -|
            // |- Drag -|- Flag -|- Text | But     -|- Text          -|- Text             -|- Yes | No -|
            

            // First, calculate how much flexible width we have so we can assign proportions of that to resizing fields
            totalWidth = givenAvailableWidth;
            lineAlocatedWidth = totalWidth - 20;
            lineAlocatedFlexibleWidth = lineAlocatedWidth - dragWidth - flagWidth - controlWidth - controlSpacingX * 4; // flexible take away constants

            totalHeight = givenAvailableHeight;

            // Drag

            // Flag
            
            // Speaker name selection/edit
            speakerNamePopupWidth = Mathf.Min(Mathf.Max(lineAlocatedFlexibleWidth * 0.15f, 80f), 160f) - controlSpacingX;
            speakerNameButtonWidth = 20;
            speakerNameTextWidth = speakerNamePopupWidth - speakerNameButtonWidth - controlSpacingX;

            // File name edit or clip selection (before file name width)
            fileNameTextWidth = Mathf.Min(Mathf.Max(lineAlocatedFlexibleWidth * 0.25f, 180f), 300f) - controlSpacingX;
            fileNamePlayButtonWidth = 20 - controlSpacingX;
            fileNameClipButtonWidth = 20 - controlSpacingX;
            fileNameClipFieldWidth = fileNameTextWidth - fileNameClipButtonWidth - fileNamePlayButtonWidth - 8; // extra object selector bubble here

            // Speech text
            speechTextOffset = speakerNameOffset + speakerNamePopupWidth + controlSpacingX;
            speechTextWidth = lineAlocatedFlexibleWidth - speakerNamePopupWidth - fileNameTextWidth - controlSpacingX;

            fileNameOffset = speechTextOffset + speechTextWidth + controlSpacingX; // after speech width is known
            fileNameClipFieldOffset = fileNameOffset + fileNameClipButtonWidth + controlSpacingX; // extra object selector bubble here
            fileNameClipButtonOffset = fileNameClipFieldOffset + fileNameClipFieldWidth + controlSpacingX; // extra object selector bubble here

            // Controls
            controlOffset = fileNameOffset + fileNameTextWidth + controlSpacingX;
        }

        public Rect GetScrollBoxLayoutRect(int issueStringHeight)
        {
            return new Rect(
                0, 
                2 + menuHeight + 4 + headerHeight + lineSpacing, 
                totalWidth, 
                totalHeight - headerHeight - menuHeight - 6 - lineSpacing * 2 - (issueStringHeight > 0 ? issueStringHeight + 2 : 0) - footerHeight - 24); // 24 is unity window header...
        }

        public Rect GetScrollBoxContentRect(int lineCount, Rect scrollBoxLayoutRect)
        {
            int height = lineCount * (lineHeight + lineSpacing);
            //return new Rect(0, 0, totalWidth - (height > scrollBoxLayoutRect.height ? 15 : 0), height);
            return new Rect(0, 0, totalWidth - 15, height); // we would need to calculate total line height first, then allocate totalWidth depending on that
        }

        public Rect GetIssueBoxRect(int issueStringHeight)
        {
            return new Rect(2, totalHeight - footerHeight - menuHeight - 6 - issueStringHeight, totalWidth-6, issueStringHeight);
        }

        public Rect GetHeaderRect()
        {
            return new Rect(2, menuHeight + 4, totalWidth-6, headerHeight);
        }

        public Rect GetNoLinesMessageRect()
        {
            return new Rect(4, headerHeight + 8, totalWidth-10, 36);
        }

        public Rect GetFooterRect()
        {
            return new Rect(2, totalHeight - footerHeight - menuHeight - 4, totalWidth-6, footerHeight);
        }

        public Rect GetLineRect(float lineOffsetTop)
        {
            return new Rect(2, lineOffsetTop, lineAlocatedWidth, lineHeight);
        }

        public Rect GetDragControlRect()
        {
            return new Rect(controlSpacingY, controlSpacingY, dragWidth, controlHeight);
        }

        public Rect GetFlagControlRect()
        {
            return new Rect(flagOffset, controlSpacingY, dragWidth, controlHeight);
        }

        public Rect GetSpeakerNamePopupRect()
        {
            return new Rect(speakerNameOffset, controlSpacingY, speakerNamePopupWidth, controlHeight);
        }

        public Rect GetSpeakerNameTextRect()
        {
            return new Rect(speakerNameOffset, controlSpacingY, speakerNameTextWidth, controlHeight);
        }

        public Rect GetSpeakerNameButtonRect()
        {
            return new Rect(speakerNameOffset + speakerNameTextWidth + controlSpacingX, controlSpacingY, speakerNameButtonWidth, controlHeight);
        }

        public Rect GetSpeechTextRect()
        {
            return new Rect(speechTextOffset, controlSpacingY, speechTextWidth, controlHeight);
        }

        public Rect GetFileNameClipPlayRect()
        {
            return new Rect(fileNameOffset, controlSpacingY, fileNamePlayButtonWidth, controlHeight);
        }

        public Rect GetFileNameClipFieldRect()
        {
            return new Rect(fileNameClipFieldOffset, controlSpacingY, fileNameClipFieldWidth, controlHeight);
        }

        public Rect GetFileNameClipButtonRect()
        {
            return new Rect(fileNameClipButtonOffset, controlSpacingY, fileNameClipButtonWidth, controlHeight);
        }

        public Rect GetFileNameTextRect()
        {
            return new Rect(fileNameOffset, controlSpacingY, fileNameTextWidth, controlHeight);
        }

        public Rect GetControlsRect()
        {
            return new Rect(controlOffset, controlSpacingY, controlWidth, controlHeight);
        }

        public Rect GetDeleteButtonRect()
        {
            return new Rect(controlOffset, controlSpacingY, deleteButtonWidth, controlHeight);
        }

        //public Rect GetMoveUpButtonRect()
        //{
        //    return new Rect(controlOffset + deleteButtonWidth + controlSpacingX, controlSpacingY, moveLineButtonWidth, controlHeight);
        //}
        //
        //public Rect GetMoveDownButtonRect()
        //{
        //    return new Rect(controlOffset + deleteButtonWidth + controlSpacingX + moveLineButtonWidth + controlSpacingX, controlSpacingY, moveLineButtonWidth, controlHeight);
        //}

        public Rect GetDeleteConfirmButtonRect()
        {
            return new Rect(controlOffset, controlSpacingY, deleteConfirmButtonWidth, controlHeight);
        }

        public Rect GetDeleteCancelButtonRect()
        {
            return new Rect(controlOffset + deleteConfirmButtonWidth + controlSpacingX, controlSpacingY, deleteCancelButtonWidth, controlHeight);
        }

        public static float GetLineY(int lineIndex)
        {
            return (lineHeight + lineSpacing) * lineIndex;
        }
    }
    private SpacingInfo spacingInfo;

    private struct LineDragInfo
    {
        public static int draggedLineIndex = 0;
        public static bool dragging = false;
        public static float dragStartMousePositionY;
        public static Vector2 lastTrueMousePosition;
        public static float lastLineY;
    }

    private struct DragAndDropInfo
    {
        public static Vector2? lastTrueMousePosition;
    }

    /// <summary>
    /// This class keeps a record of all the GUIStyles we use multiple times, so we can cache them and don't need to recreate each draw pass
    /// </summary>
    private static class OurStyles
    {
        private enum Skin { none, free, pro }
        private static Skin skin = Skin.none;

        private static bool SkinMatch()
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (skin == Skin.pro)
                {
                    return true;
                }
                else
                {
                    skin = Skin.pro;
                    return false;
                }
            }
            else
            {
                if (skin == Skin.free)
                {
                    return true;
                }
                else
                {
                    skin = Skin.free;
                    return false;
                }
            }
        }
        
        private static GUIStyle _emptyTextFieldStyle;
        public static GUIStyle EmptyTextFieldStyle
        {
            get
            {
                if (_emptyTextFieldStyle == null)
                {
                    _emptyTextFieldStyle = new GUIStyle(EditorStyles.textField) { normal = {textColor = Color.gray} };
                }
                return _emptyTextFieldStyle;
            }
        }

        private static GUIStyle _sortButtonStyle;
        public static GUIStyle SortButtonStyle
        {
            get
            {
                if (_sortButtonStyle == null)
                {
                    _sortButtonStyle = new GUIStyle(GUI.skin.button) { padding = new RectOffset(0, 0, 0, 0) };
                }
                return _sortButtonStyle;
            }
        }

        private static GUIStyle _wordWrappedLabel;
        public static GUIStyle WordWrappedLabel
        {
            get
            {
                if (_wordWrappedLabel == null)
                {
                    _wordWrappedLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
                }
                return _wordWrappedLabel;
            }
        }

        private static GUIStyle _settingsBox;
        public static GUIStyle SettingsBox
        {
            get
            {
                if (_settingsBox == null || !SkinMatch())
                {
                    _settingsBox = new GUIStyle(GUI.skin.box);
                    Texture2D texture = EditorGUIUtility.isProSkin ? backgroundBoxTextureDark : backgroundBoxTextureLight;
                    if (texture != null)
                        _settingsBox.normal.background = texture;
                }
                return _settingsBox;
            }
        }
        
        private static GUIStyle _line;
        public static GUIStyle Line
        {
            get
            {
                if (_line == null || !SkinMatch())
                {
                    _line = new GUIStyle(GUI.skin.box);
                    Texture2D texture = EditorGUIUtility.isProSkin ? backgroundBoxTextureDark : backgroundBoxTextureLight;
                    if (texture != null)
                        _line.normal.background = texture;
                }
                return _line;
            }
        }

        private static GUIStyle _draggedLine;
        public static GUIStyle DraggedLine
        {
            get
            {
                if (_draggedLine == null || !SkinMatch())
                {
                    _draggedLine = new GUIStyle(GUI.skin.box);
                    Texture2D texture = EditorGUIUtility.isProSkin ? backgroundBoxHighlightedTextureDark : backgroundBoxHighlightedTextureLight;
                    if (texture != null)
                        _draggedLine.normal.background = texture;
                }
                return _draggedLine;
            }
        }

        private static GUIStyle _toolbarButton;
        public static GUIStyle ToolbarButton
        {
            get
            {
                if (_toolbarButton == null)
                {
                    _toolbarButton = new GUIStyle(EditorStyles.toolbarButton) { margin = new RectOffset(0, 3, 6, 0) };
                }
                return _toolbarButton;
            }
        }

        private static GUIStyle _refreshButton;
        public static GUIStyle RefreshButton
        {
            get
            {
                if (_refreshButton == null)
                {
                    _refreshButton = new GUIStyle(GUI.skin.button) { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 2, 2) };
                }
                return _refreshButton;
            }
        }
    }

    private void OnGUI_LinesTab()
    {
        if (settings.querier == null)
        {
            EditorGUILayout.HelpBox("EasyVoice could not automatically select a valid text-to-speech platform/option for the current platform.", MessageType.Warning);
        }

        EditorGUILayout.Separator();

        int outputCount = 0;
        int outputOkayCount = 0;
        int outputIssueCount = 0;
        int outputProblemCount = 0;

        bool createNewLine = false; // this gets trigerred by something in the middle of line drawing (we can't just create mid-list iteration), then creates a new line at the end

        // Cache current event stuff
        Event currentEvent = Event.current;
        EventType currentEventType = currentEvent.type;

        if (settings.data.LineCount() > 0)
        {
            // == HEADERS == //

            GUI.BeginGroup(spacingInfo.GetHeaderRect(), EditorStyles.toolbar);

            //GUI.Label("Dr", GUILayout.Width(SpacingInfo.dragWidth));
            if (GUI.Button(spacingInfo.GetFlagControlRect(), "O", GUI.skin.label))
            {
                deleteConfirmActive = false;
                GenericMenu outputMenu = new GenericMenu();
                outputMenu.AddItem(new GUIContent("Enable all"), false, Enable);
                outputMenu.AddItem(new GUIContent("Disable all"), false, Disable);
                outputMenu.ShowAsContext();
            }
            if (GUI.Button(spacingInfo.GetSpeakerNamePopupRect(), "Voice", GUI.skin.label))
            {
                deleteConfirmActive = false;
                GenericMenu outputMenu = new GenericMenu();
                outputMenu.AddItem(new GUIContent("Sort ascending"), false, SortSpeakerAscending);
                outputMenu.AddItem(new GUIContent("Sort descending"), false, SortSpeakerDescending);
                outputMenu.ShowAsContext();
            }
            GUI.Label(spacingInfo.GetSpeechTextRect(), "Text to say");
            if (GUI.Button(spacingInfo.GetFileNameTextRect(), "File/asset name", GUI.skin.label))
            {
                deleteConfirmActive = false;
                GenericMenu outputMenu = new GenericMenu();
                outputMenu.AddItem(new GUIContent("Sort ascending"), false, SortFileNameAscending);
                outputMenu.AddItem(new GUIContent("Sort descending"), false, SortFileNameDescending);
                outputMenu.ShowAsContext();
            }
            GUI.Label(spacingInfo.GetControlsRect(), "Action");
        
            GUI.EndGroup();

            // If we are in delete mode, then de-focus when user selects/enters any interactable fields
            if (deleteConfirmActive)
            {
                if (currentEventType == EventType.Repaint) // during layout, indices change and user will select wrong controls and such
                {
                    string focusedControl = GUI.GetNameOfFocusedControl();
                    if (focusedControl != "" && focusedControl != "LinesToggle" && focusedControl != "DeleteConfirmButton" && focusedControl != "DeleteCancelButton")
                    deleteConfirmActive = false;
                }
            }

            // == LINES == //

            int issueStringHeight;
            string issueString = GetLineIssuesString(out issueStringHeight);
            if (deleteConfirmActive)
                issueStringHeight = 0; // we won't be printing issue list if delete confirm is active

            // Scroll view

            Rect scrollBoxLayoutRect = spacingInfo.GetScrollBoxLayoutRect(issueStringHeight);
            Rect scrollBoxContentRect = spacingInfo.GetScrollBoxContentRect(settings.data.LineCount(), scrollBoxLayoutRect);
            linesScrollPosition = GUI.BeginScrollView(scrollBoxLayoutRect, linesScrollPosition, scrollBoxContentRect);

            //GUI.Label(scrollBoxContentRect, logoTexture);
            //GUI.DrawTexture(scrollBoxContentRect, logoTexture);

            // Speaker names

            string[] speakerNames = settings.GetSpeakerNamesForEditor(true, false); // cache for performance

            // Drag

            // If we are dragging, decide whereabouts our new position is
            int dragInsertIndex = settings.data.LineCount(); // none by default
            if (LineDragInfo.dragging && LineDragInfo.lastLineY > -100000)
            {
                dragInsertIndex = (int)((LineDragInfo.lastLineY + SpacingInfo.lineHeight / 2) / (SpacingInfo.lineHeight + SpacingInfo.lineSpacing));
                dragInsertIndex = Mathf.Min(Mathf.Max(dragInsertIndex, 0), settings.data.LineCount());
                //Debug.Log("Drag drop index = " + dragInsertIndex);
            }

            // Drag & dropped objects

            AudioClip linkedClip = null;
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is AudioClip)
                {
                    linkedClip = (AudioClip)obj;
                    break;
                }
            }

            // Events

            // Check the mouse events related to dragging
            if (currentEventType == EventType.MouseUp)
            {
                if (LineDragInfo.dragging)
                {
                    LineDragInfo.dragging = false;
                    //Debug.Log("Done dragging line #" + LineDragInfo.draggedLineIndex + " -> " + dragInsertIndex);
                    if (LineDragInfo.draggedLineIndex != dragInsertIndex)
                        settings.data.MoveLine(LineDragInfo.draggedLineIndex, dragInsertIndex < LineDragInfo.draggedLineIndex ? dragInsertIndex : dragInsertIndex + 1); // it will do sanity checks within
                    currentEvent.Use();
                }
            }
            else if (currentEventType == EventType.MouseDrag)
            {
                if (LineDragInfo.dragging)
                {
                    currentEvent.Use(); // otherwise, UI won't update next frame
                    LineDragInfo.lastTrueMousePosition = currentEvent.mousePosition;
                    //Debug.Log("Dragging line #" + LineDragInfo.draggedLineIndex + ", line at " + LineDragInfo.lastLineY);
                }
            }

            if (currentEventType == EventType.DragUpdated)
            {
                DragAndDropInfo.lastTrueMousePosition = currentEvent.mousePosition;
            }

            // Now, go through all the data lines (we run 1 extra line for when we draw the dragged line above others)
            for (int lineIterator = 0; lineIterator <= settings.data.LineCount(); lineIterator++)
            {
                int lineIndex = 0;
                bool currentLineDragged = false;

                // When we are dragging, we always draw the dragged line last so that it is on top, so we wait for max lineIterator
                if (lineIterator == settings.data.LineCount())
                {
                    if (!LineDragInfo.dragging)
                        break;

                    currentLineDragged = true;
                    lineIndex = LineDragInfo.draggedLineIndex;
                }
                else
                {
                    lineIndex = lineIterator;
                }

                float currentLineY = SpacingInfo.GetLineY(lineIndex);

                // If we are dragging, adjust the line spacing
                if (LineDragInfo.dragging)
                {
                    if (lineIterator < settings.data.LineCount()) // for non-dragged lines
                    {
                        if (lineIndex >= dragInsertIndex && lineIndex < LineDragInfo.draggedLineIndex) // lines before move down, when we try to insert above them
                            currentLineY += SpacingInfo.lineHeight + SpacingInfo.lineSpacing;
                        if (lineIndex <= dragInsertIndex && lineIndex > LineDragInfo.draggedLineIndex) // lines after move up, wehn we try to insert below them
                            currentLineY -= SpacingInfo.lineHeight + SpacingInfo.lineSpacing;
                    }
                }

                if (LineDragInfo.dragging && LineDragInfo.draggedLineIndex == lineIterator) // not lineIndex
                {
                    //currentLineY = (SpacingInfo.lineHeight + SpacingInfo.lineSpacing) * lineIndex;
                    //GUI.backgroundColor = Color.black;
                    //GUILayout.BeginArea(spacingInfo.GetLineRect(currentLineY), "", GUI.skin.box);
                    //GUI.backgroundColor = Color.white;
                    //GUILayout.EndArea();
                    continue;
                }

                if (currentLineDragged)
                {
                    Rect lineRect = spacingInfo.GetLineRect(currentLineY);
                    lineRect.y += LineDragInfo.lastTrueMousePosition.y - LineDragInfo.dragStartMousePositionY;
                    lineRect.y = Mathf.Max(Mathf.Min(lineRect.y, (settings.data.LineCount() - 1) * (SpacingInfo.lineHeight + SpacingInfo.lineSpacing)), 0);
                    LineDragInfo.lastLineY = lineRect.y;
                    //GUI.backgroundColor = new Color(1,1,1,.5f); -- doesn't get any lighter, only tinted
                    GUILayout.BeginArea(lineRect, "", OurStyles.DraggedLine);
                    //GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUILayout.BeginArea(spacingInfo.GetLineRect(currentLineY), "", OurStyles.Line);
                }
                //GUILayout.BeginHorizontal(GUI.skin.box);

                // == DRAG == //

                Rect dragControlRect = spacingInfo.GetDragControlRect();
                GUI.Label(dragControlRect, SafeTextureContent(dragLineTextureDark, dragLineTextureLight, "=", LineDragInfo.dragging ? null : "Drag to re-order line"));

                if (currentEventType == EventType.MouseDown)
                {
                    if (!LineDragInfo.dragging)
                    {
                        if (dragControlRect.Contains(currentEvent.mousePosition))
                        {
                            deleteConfirmActive = false; // Make sure delete confirm is disabled (no focus trigger here otherwise)
                            if (currentEvent.button == 0) // left
                            {
                                LineDragInfo.draggedLineIndex = lineIndex;
                                LineDragInfo.dragging = true;
                                LineDragInfo.lastTrueMousePosition = new Vector2(currentEvent.mousePosition.x, currentEvent.mousePosition.y + currentLineY);
                                LineDragInfo.dragStartMousePositionY = LineDragInfo.lastTrueMousePosition.y;
                                LineDragInfo.lastLineY = -100001;
                                //Debug.Log("Starting to drag line #" + LineDragInfo.draggedLineIndex + " at mouse " + LineDragInfo.dragStartMousePositionY);
                                GUI.FocusControl("LinesToggle"); // cancel any non-drag control focus
                                currentEvent.Use();
                            }
                            else
                            {
                                GenericMenu outputMenu = new GenericMenu();
                                if (lineIndex == 0)
                                    outputMenu.AddDisabledItem(new GUIContent("Move up"));
                                else
                                    outputMenu.AddItem(new GUIContent("Move up"), false, DragHandleContextMoveUp, lineIndex);
                                if (lineIndex == settings.data.LineCount() - 1)
                                    outputMenu.AddDisabledItem(new GUIContent("Move down"));
                                else
                                    outputMenu.AddItem(new GUIContent("Move down"), false, DragHandleContextMoveDown, lineIndex);
                                outputMenu.AddSeparator("");
                                if (lineIndex == 0)
                                    outputMenu.AddDisabledItem(new GUIContent("Move to top"));
                                else
                                    outputMenu.AddItem(new GUIContent("Move to top"), false, DragHandleContextMoveTop, lineIndex);
                                if (lineIndex == settings.data.LineCount() - 1)
                                    outputMenu.AddDisabledItem(new GUIContent("Move to bottom"));
                                else
                                    outputMenu.AddItem(new GUIContent("Move to bottom"), false, DragHandleContextMoveBottom, lineIndex);
                                outputMenu.AddSeparator("");
                                outputMenu.AddItem(new GUIContent("Duplicate line"), false, DragHandleContextDuplicate, lineIndex);
                                outputMenu.ShowAsContext();
                            }
                        }
                    }
                }

                // == FLAG == //

                bool currentOutputStatus = settings.data.GetOutputStatus(lineIndex);

                Rect outputFlagRect = spacingInfo.GetFlagControlRect();
                if (currentOutputStatus)
                {
                    LineIssue issues = settings.data.GetIssues(lineIndex);
                    if (EasyVoiceClipCreator.IssuePreventsFileMaking(issues))
                        GUI.color = EditorGUIUtility.isProSkin ? new Color(1f, 0.6f, 0.6f) : new Color(1f, 0.5f, 0.5f); // red
                    else
                        GUI.color = EditorGUIUtility.isProSkin ? new Color(0.6f, 1f, 0.6f) : new Color(0.5f, 1f, 0.5f); // green
                }
                bool newOutputStatus = EditorGUI.Toggle(outputFlagRect, currentOutputStatus);
                if (newOutputStatus != currentOutputStatus)
                {
                    settings.data.SetOutputStatus(lineIndex, newOutputStatus);
                    EasyVoiceIssueChecker.CheckLineIssues(lineIndex); // recheck it when toggling, while we are at it, no other way to manually recheck
                }
                GUI.color = Color.white;
                GUI.Label(outputFlagRect, new GUIContent("", "Toggle output of this line")); // Checbox doesn't show tooltips when mouseover check area

                //EditorGUILayout.Popup(settings.GetSourceIndex(settings.data.GetSpeakerSource(i)), speakerSources, GUILayout.Width(speakerSourceWidth));

                // == SPEAKER NAME == //

                string currentSpeakerName = settings.data.GetSpeakerName(lineIndex);
                if (settings.ValidVoiceName(currentSpeakerName, true))
                {
                    Rect speakerPopupRect = spacingInfo.GetSpeakerNamePopupRect();
                    GUI.SetNextControlName("SpeakerName" + lineIndex);
                    settings.data.SetSpeakerName(lineIndex, settings.GetSpeakerName(
                        EditorGUI.Popup(speakerPopupRect, settings.GetSpeakerIndex(currentSpeakerName, true), speakerNames),
                        true));
                    GUI.Label(speakerPopupRect, new GUIContent("", "Choose a voice for this line")); // Popup needs content for each field for tooltips
                }
                else
                {
                    if (currentOutputStatus && settings.data.HasIssue(lineIndex, LineIssue.invalidSpeaker)) GUI.backgroundColor = Color.red;
                    Rect speakerTextRect = spacingInfo.GetSpeakerNameTextRect();
                    GUI.SetNextControlName("SpeakerName" + lineIndex);
                    settings.data.SetSpeakerName(lineIndex, EditorGUI.TextField(speakerTextRect, settings.data.GetSpeakerName(lineIndex)));
                    GUI.backgroundColor = Color.white;
                    GUI.Label(speakerTextRect, new GUIContent("", "Enter a custom speaker name for this line")); // Text field doesn't show tooltips when mouseover edit area
                    if (GUI.Button(spacingInfo.GetSpeakerNameButtonRect(), new GUIContent("X", "Reset to default speaker name")))
                    {
                        deleteConfirmActive = false;
                        settings.data.SetSpeakerName(lineIndex, EasyVoiceSettings.defaultSpeakerNameString);
                        GUI.FocusControl("LinesToggle"); // make sure we are not focused on a wrong control (probably speech edit box)
                    }
                }
                
                // == SPEECH TEXT == //

                string currentSpeechText = settings.data.GetSpeechText(lineIndex);

                bool needToFocusSpeechTextField = justCreatedLine && justCreatedLineIndex == lineIndex; // focus if this line was just created
                bool speechTextFieldFocused =
                    needToFocusSpeechTextField || // basically, we will focus it, so treat it as focused (and don't redraw again thus losing focus)
                    GUI.GetNameOfFocusedControl() == "SpeechTextField" + lineIndex;

                Rect speechTextRect = spacingInfo.GetSpeechTextRect();

                if (currentSpeechText == "" && !speechTextFieldFocused && (!forceNoPlaceholderFocus || forceNoPlaceholderFocusIndex != lineIndex))
                {
                    //GUI.contentColor = EditorGUIUtility.isProSkin ? Color.black : Color.gray; // Can't get free to work like this, pro works fine
                    GUI.SetNextControlName("SpeechTextField" + lineIndex);

                    if (currentOutputStatus) GUI.backgroundColor = Color.red;
                    EditorGUI.TextArea(speechTextRect, "<Enter text here>", OurStyles.EmptyTextFieldStyle);
                    if (currentOutputStatus) GUI.backgroundColor = Color.white;

                    //GUI.contentColor = Color.white;

                    if (lastFocusedControl != "SpeechTextField" + lineIndex && GUI.GetNameOfFocusedControl() == "SpeechTextField" + lineIndex)
                    {
                        GUI.FocusControl("LinesToggle");
                        forceNoPlaceholderFocus = true;
                        forceNoPlaceholderFocusIndex = lineIndex;
                    }
                }
                else
                {
                    GUI.SetNextControlName("SpeechTextField" + lineIndex);
                    settings.data.SetSpeechText(lineIndex, EditorGUI.TextArea(speechTextRect, currentSpeechText));

                    if (GUI.GetNameOfFocusedControl() == "SpeechTextField" + lineIndex)
                    {
                        bool pressedEnter = currentEventType == EventType.KeyUp && currentEvent.keyCode == KeyCode.Return; // must be before control consumes it
                        if (pressedEnter)
                        {
                            if (speechTextFieldFocused && settings.data.GetSpeechText(lineIndex) != "" && lineIndex == settings.data.LineCount() - 1)
                            {
                                //Debug.Log("Enter pressed on last");
                                GUI.FocusControl("LinesToggle"); // unfocus edit field
                                createNewLine = true; // tell it to create a new line at the end
                                currentEvent.Use(); // don't process Enter again (it will trigger on the new line)
                            }
                            else if (lineIndex == settings.data.LineCount() - 1)
                            {
                                //Debug.Log("Enter pressed on not last");
                                //GUI.FocusControl("LinesToggle"); // unfocus edit field
                            }
                        }
                    }

                    //if (lastFocusedControl != "SpeechTextField" + i && GUI.GetNameOfFocusedControl() == "SpeechTextField" + i)
                    //{
                    //    Debug.Log("Just got regular focused!");
                    //}
                }
                GUI.Label(speechTextRect, new GUIContent("", "Enter text to be spoken")); // Text field doesn't show tooltips when mouseover edit area

                if (currentEventType == EventType.Layout && (needToFocusSpeechTextField || (forceNoPlaceholderFocus && forceNoPlaceholderFocusIndex == lineIndex)))
                {
                    //GUI.FocusControl("SpeechTextField" + justCreatedLineIndex); //-- use below one for text fields
                    EditorGUI.FocusTextInControl("SpeechTextField" + lineIndex); // text fields also need to go into their editing state
                    if (GUI.GetNameOfFocusedControl() == "SpeechTextField" + lineIndex)
                    {
                        justCreatedLine = false;
                        forceNoPlaceholderFocus = false;
                    }
                }

                // == FILE NAME OR CLIP REFERENCE == //

                AudioClip ourClip = settings.data.GetClip(lineIndex);
                if (ourClip != null)
                {
                    // -- CLIP REFERENCE -- //
                    bool playingOurClip = playingPreviewClip && clipPlayer != null && ourClip == clipPlayer.clip;
                    if (GUI.Button(spacingInfo.GetFileNameClipPlayRect(), new GUIContent(SafeTextureContent(playingOurClip ? stopTextureDark : playTextureDark, playingOurClip ? stopTextureLight : playTextureLight, ">", "Play the clip")), OurStyles.SortButtonStyle))
                    {
                        deleteConfirmActive = false;
                        PlayClip(ourClip);
                    }
                    if (settings.data.HasIssue(lineIndex, LineIssue.duplicateAssetFileName)) GUI.backgroundColor = Color.red; //new Color(1.0f, 0, 0.5f);
                    else if (settings.data.HasIssue(lineIndex, LineIssue.duplicateClipReference)) GUI.backgroundColor = Color.red; //new Color(1.0f, 0.4f, 0);
                    Rect fileClipRect = spacingInfo.GetFileNameClipFieldRect();
                    GUI.SetNextControlName("ClipField" + lineIndex);
                    settings.data.SetClip(lineIndex, (AudioClip)EditorGUI.ObjectField(fileClipRect, ourClip, typeof(AudioClip), false));
                    GUI.backgroundColor = Color.white;
                    GUI.Label(fileClipRect, new GUIContent("", "Drop another AudioClip to replace currently linked file")); // Text field doesn't show tooltips when mouseover edit area
                    if (GUI.Button(spacingInfo.GetFileNameClipButtonRect(), new GUIContent(SafeTextureContent(unlinkTextureDark, unlinkTextureLight, "X", "Unlink from the asset and keep file name only")), OurStyles.SortButtonStyle))
                    {
                        deleteConfirmActive = false;
                        settings.data.ClearClip(lineIndex);
                        GUI.FocusControl("LinesToggle"); // because the mouse can be in the text field next to it (showing old value)
                    }
                }
                else
                {
                    // -- FILE NAME -- //

                    if (settings.data.HasIssue(lineIndex, LineIssue.duplicateAssetFileName)) GUI.backgroundColor = Color.red; //new Color(1.0f, 0, 0.5f);
                    else if (settings.data.HasIssue(lineIndex, LineIssue.duplicateBaseFileName)) GUI.backgroundColor = Color.red; //new Color(1, 0, 0);
                    else if (settings.data.HasIssue(lineIndex, LineIssue.clashingExistingAsset) && settings.linkClips) GUI.backgroundColor = Color.red; //new Color(1.0f, 0, 1.0f);
                    else if (settings.data.HasIssue(lineIndex, LineIssue.badFileName)) GUI.backgroundColor = Color.red; //new Color(1, 0, 0);

                    Rect fileNameRect = spacingInfo.GetFileNameTextRect();

                    string currentFileName = settings.data.GetFileName(lineIndex);
                    if (currentFileName == EasyVoiceSettings.defaultFileNameString)
                    {
                        string currentFullFileName = settings.data.GetFileNameOrDefault(lineIndex);
                        GUI.SetNextControlName("FileName" + lineIndex);
                        string newValue = EditorGUI.TextField(fileNameRect, currentFullFileName, OurStyles.EmptyTextFieldStyle);
                        if (newValue != currentFullFileName) // only update if user manually changed something, otherwise it will write in the auto-generated value
                            settings.data.SetFileName(lineIndex, newValue, false);
                    }
                    else
                    {
                        GUI.SetNextControlName("FileName" + lineIndex);
                        settings.data.SetFileName(lineIndex, EditorGUI.TextField(fileNameRect, settings.data.GetFileName(lineIndex)), false);
                    }

                    if (currentEventType == EventType.DragUpdated || currentEventType == EventType.DragPerform)
                    {
                        if (DragAndDropInfo.lastTrueMousePosition != null)
                        {
                            Rect dropRect = new Rect(fileNameRect.x, fileNameRect.y + currentLineY, fileNameRect.width, fileNameRect.height);
                            if (dropRect.Contains((Vector2)DragAndDropInfo.lastTrueMousePosition)) // need to use last seen real mouse position
                            {
                                if (linkedClip != null)
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                                    //Debug.Log("Dropped for " + lineIndex + ", rect = " + dropRect + ", mouse = " + DragAndDropInfo.lastTrueMousePosition);
                                    if (currentEventType == EventType.DragPerform)
                                    {
                                        currentEvent.Use();
                                        settings.data.SetClip(lineIndex, linkedClip);
                                    }
                                }
                            }
                        }
                    }
                    GUI.backgroundColor = Color.white;

                    GUI.Label(fileNameRect, new GUIContent("", "Enter output file name here or drop an existing AudioClip")); // Text field doesn't show tooltips when mouseover edit area
                }


                // == CONTROLS == //

                if (deleteConfirmActive && lineIndex == deleteConfirmIndex)
                {
                    // -- DELETE PROMPT CONTOLS -- //

                    //GUILayout.Label("Rly?", GUILayout.Width(delConfirmLabelWidth));
                    GUIStyle smallTextButtonStyle = new GUIStyle(GUI.skin.button);
                    smallTextButtonStyle.fontSize = 9;
                    GUIStyle redButtonStyle = new GUIStyle(smallTextButtonStyle);
                    GUI.backgroundColor = Color.red;
                    GUI.SetNextControlName("DeleteConfirmButton");
                    if (GUI.Button(spacingInfo.GetDeleteConfirmButtonRect(), "Delete", redButtonStyle))
                    {
                        settings.data.DeleteLine(lineIndex); // deleteConfirmIndex == i
                        deleteConfirmActive = false;
                    }
                    GUI.SetNextControlName("DeleteCancelButton");
                    GUI.backgroundColor = Color.white;
                    if (GUI.Button(spacingInfo.GetDeleteCancelButtonRect(), "Cancel", smallTextButtonStyle))
                    {
                        deleteConfirmActive = false;
                    }

                    //GUILayout.EndArea();
                    //currentLineY = (SpacingInfo.lineHeight + SpacingInfo.lineSpacing) * lineIndex;
                    //GUILayout.BeginArea(spacingInfo.GetLineRect(currentLineY));
                    //TextAnchor current = GUI.skin.label.alignment;
                    //GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    //GUILayout.Label("Are you sure you want to delete the above line?");
                    //GUI.skin.label.alignment = current;
                }
                else
                {
                    // -- REGULAR CONTROLS -- //

                    if (GUI.Button(spacingInfo.GetDeleteButtonRect(), new GUIContent("Delete", "Delete this line (this won't delete the created file if any)")))
                    {
                        deleteConfirmActive = true;
                        deleteConfirmIndex = lineIndex;
                        GUI.FocusControl("LinesToggle");
                    }
                    //else if (GUI.Button(spacingInfo.GetMoveDownButtonRect(), SafeTextureContent(arrowUpTexture, "U", "Move this line up"), sortButtonStyle))
                    //{
                    //    deleteConfirmActive = false; // since buttons don't grab focus
                    //    settings.data.MoveLineUp(lineIndex);
                    //    GUI.FocusControl("LinesToggle");
                    //}
                    //else if (GUI.Button(spacingInfo.GetMoveUpButtonRect(), SafeTextureContent(arrowDownTexture, "U", "Move this line down"), sortButtonStyle))
                    //{
                    //    deleteConfirmActive = false; // since buttons don't grab focus
                    //    settings.data.MoveLineDown(lineIndex);
                    //    GUI.FocusControl("LinesToggle");
                    //}
                }
                //GUILayout.EndHorizontal();

                //if (currentLineDragged)
                    GUILayout.EndArea();

                // Update out output okay/issue/error counters for summary
                if (settings.data.GetOutputStatus(lineIndex))
                {
                    if (settings.data.GetIssues(lineIndex) == 0)
                    {
                        outputCount++;
                        outputOkayCount++;
                    }
                    else
                    {
                        if (EasyVoiceClipCreator.IssuePreventsFileMaking(settings.data.GetIssues(lineIndex)))
                        {
                            outputProblemCount++;
                        }
                        else
                        {
                            outputCount++;
                            outputIssueCount++;
                        }
                    }
                }
            }

            GUI.EndScrollView();

            if (!deleteConfirmActive && issueString != "")
                EditorGUI.HelpBox(spacingInfo.GetIssueBoxRect(issueStringHeight), issueString, MessageType.Warning);
        }
        else
        {
            linesScrollPosition = EditorGUILayout.BeginScrollView(linesScrollPosition); // to match normal layout, with button at bottom

            EditorGUI.HelpBox(spacingInfo.GetNoLinesMessageRect(), "No lines have been added yet, click \"New line\" to add your first line.", MessageType.Info);

            GUI.EndScrollView();
        }

        if (currentEventType == EventType.DragExited || currentEventType == EventType.DragPerform)
        {
            DragAndDropInfo.lastTrueMousePosition = null;
        }

        GUI.BeginGroup(spacingInfo.GetFooterRect(), "", OurStyles.SettingsBox);

        if (deleteConfirmActive)
        {
            if (warningIconTexture != null) GUI.Label(new Rect(6, 6, 22, 22), new GUIContent(warningIconTexture));
            GUI.Label(new Rect(32, 7, 500, 20), "You have selected a line for deletion, click delete again to delete it or cancel.");
        }
        else
        {
            // We keep track of the label offset, then we measure the text we want to print and adjust this offset for further labels
            float labelOffset = 4;

            //// BETA VERSION TIMER
            //
            //GUI.Label(new Rect(labelOffset, 7, 210, 20), "RC (r143) DO NOT DISTRIBUTE");
            //
            //labelOffset += 200;
            //
            //if (DateTime.Now > new DateTime(2014, 10, 25))
            //{
            //    GUI.color = Color.red;
            //    GUI.Label(new Rect(labelOffset, 7, 200, 20), "      This version is expired!");
            //    GUI.color = Color.white;
            //}
            //else
            {
                if (createNewLine)
                {
                    GUI.FocusControl("LinesToggle");
                    // We need to create a new line, but this has to happen at the very end, i.e. here, so we just do that here
                    justCreatedLineIndex = settings.data.AddNewLine();
                    justCreatedLine = true;
                }
                else
                {
                    if (settings.data.LineCount() == 0) GUI.backgroundColor = Color.green;
                    if (GUI.Button(new Rect(labelOffset, 4, 100, 20), "New line"))
                    {
                        GUI.FocusControl("LinesToggle");
                        justCreatedLineIndex = settings.data.AddNewLine();
                        justCreatedLine = true;
                    }
                    if (settings.data.LineCount() == 0) GUI.backgroundColor = Color.white;
                }

                labelOffset += 104;

                if (settings.data.LineCount() > 0)
                {
                    if (outputCount == 0) GUI.enabled = false;
                    else GUI.backgroundColor = Color.green;
                    //if (GUI.Button(new Rect(108, 4, 100, 20), new GUIContent(outputCount > 0 ? "Create " + outputCount + " clips" : "Create clips", outputCount > 0 ? outputProblemCount > 0 ? "Create voice files for all error-free lines" : "Create voice files for all lines" : outputProblemCount > 0 ? "No error-free lines are selected for output, fix errors or select other lines" : "No lines are are selected for output, create more or select some lines first")))
                    if (GUI.Button(new Rect(labelOffset, 4, 100, 20), new GUIContent("Create voice", outputCount > 0 ? outputProblemCount > 0 ? "Create voice files for all error-free lines" : "Create voice files for all lines" : outputProblemCount > 0 ? "No error-free lines are selected for output, fix errors or select other lines" : "No lines are selected for output, create more or select some lines first")))
                    {
                        GUI.FocusControl("LinesToggle");
                        GUI.backgroundColor = Color.white;
                        switch (EasyVoiceClipCreator.CreateFilesPreCheck(settings))
                        {
                            case EasyVoiceClipCreator.CreateFilesCheckResult.ok:
                                CreateFiles();
                                break;

                            case EasyVoiceClipCreator.CreateFilesCheckResult.badDefaultFolder:
                                EditorUtility.DisplayDialog("Invalid directory", "The default directory you selected in the settings does not appear to be a valid sub-path of your Assets folder; please check your EasyVoice settings.", "Ok");
                                break;

                            case EasyVoiceClipCreator.CreateFilesCheckResult.missingDefaultFolder:
                                //if (EditorUtility.DisplayDialogComplex("Missing directory", "The default directory you selected in the settings does not exist.\r\nWould you like to attempt and create it first?", "Yes", "No", "Cancel") == 0)
                            {
                                string fullPath = settings.GetFullClipDefaultPath();
                                try
                                {
                                    Directory.CreateDirectory(fullPath);
                                    AssetDatabase.Refresh(); // immediate folder refresh, although below will do it too
                                    CreateFiles();
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError("EasyVoice failed to create the missing folder \"" + fullPath + "\" with error: " + e);
                                }
                            }
                                break;

                            case EasyVoiceClipCreator.CreateFilesCheckResult.querierFailedToVerifyApp:
                                Debug.LogError("EasyVoice could not locate its application at \"" + EasyVoiceQuerierWinOS.FileName() + "\"");
                                break;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    if (outputCount == 0) GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = false;
                    GUI.Button(new Rect(labelOffset, 4, 100, 20), new GUIContent("Create voice", "No lines have been created yet, create some lines first"));
                    GUI.enabled = true;
                }

                labelOffset += 112;

                if (outputOkayCount > 0)
                {
                    if (okayIconTexture != null) GUI.Label(new Rect(labelOffset, 6, 22, 22), new GUIContent(okayIconTexture));
                    //if (outputIssueCount > 0 || outputProblemCount > 0)
                    //{
                    string text = outputOkayCount + " line" + (outputOkayCount == 1 ? "" : "s") + " ready for output";
                    float textWidth = GUI.skin.label.CalcSize(new GUIContent(text)).x;
                    GUI.Label(new Rect(labelOffset + 24, 7, textWidth, 20), text);
                    labelOffset += textWidth + 40;
                    //}
                    //else
                    //{
                    //    string text = "All lines ready for output";
                    //    float textWidth = GUI.skin.label.CalcSize(new GUIContent(text)).x;
                    //    GUI.Label(new Rect(labelOffset + 24, 7, textWidth, 20), text); // redundant number
                    //    labelOffset += textWidth + 40;
                    //}

                }

                if (outputIssueCount > 0)
                {
                    if (warningIconTexture != null) GUI.Label(new Rect(labelOffset, 6, 22, 22), new GUIContent(warningIconTexture));
                    string text = outputIssueCount + " line" + (outputIssueCount == 1 ? " has" : "s have") + " minor issues";
                    float textWidth = GUI.skin.label.CalcSize(new GUIContent(text)).x;
                    GUI.Label(new Rect(labelOffset + 24, 7, textWidth, 20), text);
                    labelOffset += textWidth + 40;
                }

                if (outputProblemCount > 0)
                {
                    if (errorIconTexture != null) GUI.Label(new Rect(labelOffset, 6, 22, 22), new GUIContent(errorIconTexture));
                    string text = outputProblemCount + " line" + (outputProblemCount == 1 ? " has" : "s have") + " errors preventing output";
                    float textWidth = GUI.skin.label.CalcSize(new GUIContent(text)).x;
                    GUI.Label(new Rect(labelOffset + 24, 7, textWidth, 20), text);
                    labelOffset += textWidth + 40;
                }

                if (outputCount == 0 && outputIssueCount == 0 && outputProblemCount == 0)
                {
                    string text = "No lines selected for output";
                    float textWidth = GUI.skin.label.CalcSize(new GUIContent(text)).x;
                    GUI.Label(new Rect(labelOffset, 7, textWidth, 20), text);
                    labelOffset += textWidth + 40;
                }

            }
        }

        GUI.EndGroup();

        lastFocusedControl = GUI.GetNameOfFocusedControl();

        //Debug.Log("Verifications = " + EasyVoiceIssueChecker.verifyCount);
    }

    private static VerificationActions PullVerificationFunctions()
    {
        return new VerificationActions(
            EasyVoiceIssueChecker.VerifySpeakerName, 
            EasyVoiceIssueChecker.VerifySpeechText, 
            EasyVoiceIssueChecker.VerifyFileNameOrClip,
            EasyVoiceIssueChecker.AssetExists
        );
    }

    private void PlayClip(AudioClip audioClip)
    {
        if (audioClip == null) return; // safety sanity check

        if (clipPlayer == null)
        {
            GameObject go = new GameObject("Easy Voice Audio Preview Player");
            go.hideFlags = HideFlags.HideAndDontSave;
            //if (Camera.current != null)
            //    go.transform.position = Camera.current.transform.position;
            clipPlayer = go.AddComponent<AudioSource>();
            clipPlayer.panStereo = 0f; // no 3d
            clipPlayer.spatialBlend = 0f; // no 3d
        }

        if (clipPlayer == null) return; // safety sanity check

        if (clipPlayer.isPlaying && clipPlayer.clip == audioClip)
        {
            clipPlayer.Stop();
            playingPreviewClip = false;
        }
        else
        {
            clipPlayer.clip = audioClip;
            clipPlayer.loop = false;
            clipPlayer.Play();
            playingPreviewClip = true;
        }
    }

    private void Update()
    {
        if (playingPreviewClip)
        {
            if (clipPlayer != null)
            {
                if (!clipPlayer.isPlaying)
                {
                    playingPreviewClip = false;
                    Repaint();
                }
            }
        }
    }

    private void CreateFiles()
    {
        EditorUtility.DisplayProgressBar(EasyVoiceSettings.progressDialogTitle, EasyVoiceSettings.progressDialogText, 0.0f);
        try
        {
            EasyVoiceClipCreator.CreateFiles(settings); // magic
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void DragHandleContextMoveUp(object userData)
    {
        settings.data.MoveLineUp((int)userData);
    }

    private void DragHandleContextMoveDown(object userData)
    {
        settings.data.MoveLineDown((int)userData);
    }

    private void DragHandleContextMoveTop(object userData)
    {
        settings.data.MoveLine((int)userData, 0);
    }

    private void DragHandleContextMoveBottom(object userData)
    {
        settings.data.MoveLine((int)userData, settings.data.LineCount());
    }

    private void DragHandleContextDuplicate(object userData)
    {
        settings.data.DuplicateLine((int)userData);
    }

    private string GetLineIssuesString(out int issueStringHeight)
    {
        //List<List<LineIssue>> lineIssues = EasyVoiceIssueChecker.GetLineIssues(settings);

        int emptyLine = 0;
        int badFileName = 0;
        int duplicateBaseFileName = 0;
        int duplicateClipReference = 0;
        int duplicateAssetFileName = 0;
        int clashingExistingAsset = 0;
        int invalidSpeaker = 0;

        for (int i = 0; i < settings.data.LineCount(); i++)
        {
            LineIssue lineIssue = settings.data.GetIssues(i);

            //Debug.Log("Line " + i + " issue: " + lineIssue);

            if ((lineIssue & LineIssue.badFileName) == LineIssue.badFileName) badFileName++;
            if ((lineIssue & LineIssue.duplicateBaseFileName) == LineIssue.duplicateBaseFileName) duplicateBaseFileName++;
            if ((lineIssue & LineIssue.duplicateClipReference) == LineIssue.duplicateClipReference) duplicateClipReference++;
            if ((lineIssue & LineIssue.duplicateAssetFileName) == LineIssue.duplicateAssetFileName) duplicateAssetFileName++;
            if (settings.linkClips) // only show this if we are actually linking clips, otherwise clashes are fine
                if ((lineIssue & LineIssue.clashingExistingAsset) == LineIssue.clashingExistingAsset) clashingExistingAsset++;
            if (settings.data.GetOutputStatus(i)) // only show these when the line is actually selected for output
            {
                if ((lineIssue & LineIssue.emptyLine) == LineIssue.emptyLine) emptyLine++;
                if ((lineIssue & LineIssue.invalidSpeaker) == LineIssue.invalidSpeaker) invalidSpeaker++;
            }
        }

        string issues = "";

        int issueLineCount = 0;

        if (emptyLine > 0)
        {
            issueLineCount++;
            issues += (issues.Length > 0 ? "\r\n" : "") + "There " + (emptyLine > 1 ? "are" : "is") + " " + emptyLine + " line" + (emptyLine > 1 ? "s" : "") + " with empty speech text.";
        }
        if (badFileName > 0)
        {
            issueLineCount++;
            issues += (issues.Length > 0 ? "\r\n" : "") + "There " + (badFileName > 1 ? "are" : "is") + " " + badFileName + " invalid file name" + (badFileName > 1 ? "s" : "") + " in the list, the audio clips won't be generated for these.";
        }
        if (duplicateBaseFileName > 0)
        {
            issueLineCount++;
            issues += (issues.Length > 0 ? "\r\n" : "") + "There " + (duplicateBaseFileName > 1 ? "are" : "is") + " " + duplicateBaseFileName + " duplicate file name" + (duplicateBaseFileName > 1 ? "s" : "") + " in the list, generated files will overwrite each other.";
        }
        if (duplicateClipReference > 0)
        {
            issueLineCount++;
            issues += (issues.Length > 0 ? "\r\n" : "") + "There " + (duplicateClipReference > 1 ? "are" : "is") + " " + duplicateClipReference + " duplicate reference" + (duplicateClipReference > 1 ? "s" : "") + " to audio clips in the list, re-generated files will overwrite each other.";
        }
        if (duplicateAssetFileName > 0)
        {
            issueLineCount++;
            issues += (issues.Length > 0 ? "\r\n" : "") + "There " + (duplicateAssetFileName > 1 ? "are" : "is") + " " + duplicateAssetFileName + " file name" + (duplicateAssetFileName > 1 ? "s" : "") + " clashing with existing clip references, re-generated files will overwrite each other.";
        }
        if (clashingExistingAsset > 0)
        {
            issueLineCount++;
            issues += (issues.Length > 0 ? "\r\n" : "") + "There " + (clashingExistingAsset > 1 ? "are" : "is") + " " + clashingExistingAsset + " file name" + (clashingExistingAsset > 1 ? "s" : "") + " clashing with existing asset files, generated files will overwrite them.";
        }
        if (invalidSpeaker > 0)
        {
            issueLineCount++;
            issues += (issues.Length > 0 ? "\r\n" : "") + "There " + (invalidSpeaker > 1 ? "are" : "is") + " " + invalidSpeaker + " speaker name" + (invalidSpeaker > 1 ? "s" : "") + " that " + (invalidSpeaker > 1 ? "are" : "is") + " not in the available speaker list.";
        }

        issueStringHeight = issues == "" ? 0 : Mathf.Max(40, 11 * issueLineCount + 6);

        //Debug.Log(issues);

        return issues;
    }

    private void Enable()
    {
        for (int i = 0; i < settings.data.LineCount(); i++)
            settings.data.SetOutputStatus(i, true);
    }

    private void Disable()
    {
        for (int i = 0; i < settings.data.LineCount(); i++)
            settings.data.SetOutputStatus(i, false);
    }

    private void SortFileNameAscending()
    {
        settings.data.SortLinesByFileName(false);
    }

    private void SortFileNameDescending()
    {
        settings.data.SortLinesByFileName(true);
    }

    private void SortSpeakerAscending()
    {
        settings.data.SortLinesBySpeaker(false, settings);
    }

    private void SortSpeakerDescending()
    {
        settings.data.SortLinesBySpeaker(true, settings);
    }

    private static GUIContent SafeTextureContent(Texture2D textureDark, Texture2D textureLight, string label, string hint)
    {
        return EditorGUIUtility.isProSkin ?
            textureDark != null ? hint != null ? new GUIContent(textureDark, hint) : new GUIContent(textureDark) : hint != null ? new GUIContent(label, hint) : new GUIContent(label) :
            textureLight != null ? hint != null ? new GUIContent(textureLight, hint) : new GUIContent(textureLight) : hint != null ? new GUIContent(label, hint) : new GUIContent(label);
    }

    //private static GUIContent SafeTextureContent(Texture2D texture, string label)
    //{
    //    return texture != null ? new GUIContent(texture) : new GUIContent(label);
    //}

    private bool advancedSettings;
    private Vector2 settingsScrollPosition;

    private void OnGUI_SettingsTab()
    {
        GUILayout.Label("", GUILayout.Height(26));

        settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);

        int labelWidth;

        EditorGUILayout.BeginVertical(OurStyles.SettingsBox);
        labelWidth = 150;

        GUILayout.Label("Defaults", EditorStyles.boldLabel);


        GUILayout.BeginHorizontal();
        GUILayout.Label("Default speaker voice:", GUILayout.Width(labelWidth));
        string[] speakerNames = settings.GetSpeakerNamesForEditor(true, true);
        string currentSpeakerName = settings.defaultVoice;
        if (settings.ValidVoiceName(currentSpeakerName, true))
        {
            if (settings.SetDefaultVoice(settings.GetSpeakerName(EditorGUILayout.Popup(settings.GetSpeakerIndex(currentSpeakerName, true), speakerNames, GUILayout.Width(labelWidth)), true)))
                EasyVoiceIssueChecker.CheckAllFileNamesOrClips();
        }
        else
        {
            settings.SetDefaultVoice(EditorGUILayout.TextField(settings.defaultVoice, GUILayout.Width(labelWidth - 24)));
            if (GUILayout.Button(new GUIContent("X", "Select voice from available voice list"), GUILayout.Width(24)))
            {
                if (settings.SetDefaultVoice(settings.GetFirstSpeakerNameOrDefault()))
                    EasyVoiceIssueChecker.CheckAllFileNamesOrClips();
                GUI.FocusControl("LinesToggle"); // make sure we are not focused on a wrong control (probably default file name edit box)
            }
        }

        if (GUILayout.Button(SafeTextureContent(refreshTextureDark, refreshTextureLight, "R", "Refresh the available system voice list"), OurStyles.RefreshButton, GUILayout.Width(20), GUILayout.Height(16)))
        {
            settings.querier.QueryForVoiceList(settings);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Default output folder:", GUILayout.Width(labelWidth));
        bool valid = settings.IsDefaultFolderValid();
        if (!valid)
            GUI.backgroundColor = Color.red;
        if (settings.SetDefaultFolder(EditorGUILayout.TextField(settings.defaultFolder, GUILayout.MaxWidth(400))))
            EasyVoiceIssueChecker.CheckAllFileNamesOrClips();
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (valid)
        {
            GUILayout.Label("", GUILayout.Width(labelWidth));
            string fullPath = settings.GetFullClipDefaultPath();
            //bool exists = Directory.Exists(fullPath); //-- very slow on Mac
            bool exists = new DirectoryInfo(fullPath).Exists;
            //FileAttributes fileAttributes = File.GetAttributes(fullPath);
            //bool exists = (FileAttributes.Directory & fileAttributes) != 0;
            if (!exists)
            {
                if (GUILayout.Button("Create", GUILayout.Width(150)))
                {
                    try
                    {
                        Directory.CreateDirectory(fullPath);
                        AssetDatabase.Refresh();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("EasyVoice failed to create the folder \"" + fullPath + "\" with error: " + e);
                    }
                }
            }
            //if (!exists)
            //    GUI.color = Color.red;
            GUILayout.Label(fullPath);
            //GUI.color = Color.white;
        }
        else
        {
            GUILayout.Label("", GUILayout.Width(labelWidth));
            GUILayout.Label("Path must be a sub-path of your /Assets/ folder, such as \"/Audio/TTS clips/\"");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Default file name:", GUILayout.Width(labelWidth));
        if (settings.SetBaseDefaultFileName(EditorGUILayout.TextField(settings.baseDefaultFileName, GUILayout.MaxWidth(400))))
            EasyVoiceIssueChecker.CheckAllFileNamesOrClips();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(labelWidth));
        GUILayout.Label("Preview: \"" + settings.MakeFileNameFromTemplate(settings.defaultVoice, 3) + "\"");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(labelWidth));
        GUILayout.Label("You can use '$' to insert the default voice name and '#' to insert a unique number (multiple '#' for zero-padding)", OurStyles.WordWrappedLabel);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();


        EditorGUILayout.Separator();



        EditorGUILayout.BeginVertical(OurStyles.SettingsBox);
        labelWidth = 200;

        GUILayout.Label("Assets", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Link lines with created audio clips?", GUILayout.Width(labelWidth));
        settings.SetLinkClips(GUILayout.Toggle(settings.linkClips, ""));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("When audio clips for lines are created, the generated asset will be linked to this line, so you can move/edit/rename it afterwards without losing the link to the line that created it.", OurStyles.WordWrappedLabel);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();


        EditorGUILayout.Separator();



        EditorGUILayout.BeginVertical(OurStyles.SettingsBox);
        labelWidth = 180;

        GUILayout.Label("Audio Clip Import Settings", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Apply custom settings", GUILayout.Width(labelWidth));
        settings.audioImportSettings.applyCustomSettings = GUILayout.Toggle(settings.audioImportSettings.applyCustomSettings, new GUIContent("Yes", "If checked, newly created audio assets will have their (default) import settings set to the value specified below."), GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();

        if (!settings.audioImportSettings.applyCustomSettings)
            GUI.enabled = false;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Force to mono:", GUILayout.Width(labelWidth));
        settings.audioImportSettings.forceToMono = GUILayout.Toggle(settings.audioImportSettings.forceToMono, "Yes");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Load in background:", GUILayout.Width(labelWidth));
        settings.audioImportSettings.loadInBackground = GUILayout.Toggle(settings.audioImportSettings.loadInBackground, "Yes");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Preload data:", GUILayout.Width(labelWidth));
        settings.audioImportSettings.preloadAudioData = GUILayout.Toggle(settings.audioImportSettings.preloadAudioData, "Yes");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Default load type:", GUILayout.Width(labelWidth));
        //settings.audioImportSettings.loadType = 
        //    EasyVoiceAudioClipImporter.LoadType(
        //        (AudioClipLoadType)EditorGUILayout.EnumPopup(
        //            EasyVoiceAudioClipImporter.LoadType(settings.audioImportSettings.loadType), GUILayout.Width(200)));
        settings.audioImportSettings.loadType = (EasyVoiceSettings.AudioImportSettings.LoadType)EditorGUILayout.EnumPopup(settings.audioImportSettings.loadType, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Default compression format:", GUILayout.Width(labelWidth));
        //settings.audioImportSettings.compressionFormat = 
        //    EasyVoiceAudioClipImporter.CompressionFormat(
        //        (AudioCompressionFormat)EditorGUILayout.EnumPopup(
        //            EasyVoiceAudioClipImporter.CompressionFormat(settings.audioImportSettings.compressionFormat), GUILayout.Width(200)));
        settings.audioImportSettings.compressionFormat = (EasyVoiceSettings.AudioImportSettings.CompressionFormat)EditorGUILayout.EnumPopup(settings.audioImportSettings.compressionFormat, GUILayout.Width(200));
        GUILayout.EndHorizontal();
        // Note that default option only shows PCM, Vorbis and ADPCM

        if (settings.audioImportSettings.compressionFormat == EasyVoiceSettings.AudioImportSettings.CompressionFormat.Vorbis) // only default with this
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Default compression quality:", GUILayout.Width(labelWidth));
            settings.audioImportSettings.quality = EditorGUILayout.Slider(Mathf.RoundToInt(settings.audioImportSettings.quality * 100f), 1, 100, GUILayout.Width(200)) / 100f;
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Default sample rate:", GUILayout.Width(labelWidth));
        //settings.audioImportSettings.sampleRateSetting =
        //    EasyVoiceAudioClipImporter.SampleRate(
        //        (AudioSampleRateSetting)EditorGUILayout.EnumPopup(
        //            EasyVoiceAudioClipImporter.SampleRate(settings.audioImportSettings.sampleRateSetting), GUILayout.Width(200)));
        settings.audioImportSettings.sampleRateSetting = (EasyVoiceSettings.AudioImportSettings.SampleRate)EditorGUILayout.EnumPopup(settings.audioImportSettings.sampleRateSetting, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        if (settings.audioImportSettings.sampleRateSetting == EasyVoiceSettings.AudioImportSettings.SampleRate.OverrideSampleRate)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Default compression quality:", GUILayout.Width(labelWidth));
            settings.audioImportSettings.overrideSampleRate = SampleRatePopup(settings.audioImportSettings.overrideSampleRate);
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button(new GUIContent("Reset to Unity defaults", "Revert import settings to match what Unity selects by default"), GUILayout.Width(200)))
            settings.audioImportSettings = new EasyVoiceSettings.AudioImportSettings();

        //if (!settings.audioImportSettings.applyCustomSettings)
            GUI.enabled = true;

        GUILayout.EndVertical();


        EditorGUILayout.Separator();



        EditorGUILayout.BeginVertical(OurStyles.SettingsBox);

        GUILayout.Label("Import/Export", EditorStyles.boldLabel);

        GUILayout.Label("Import or export the lines data to or from an external CSV file");

        GUILayout.BeginHorizontal();

        if (settings.data.LineCount() != 0)
        {
            if (GUILayout.Button("Export data to file...", GUILayout.Width(200)))
            {
                string savePath = EditorUtility.SaveFilePanel("Export Easy Voice data file", "", "Easy Voice Data.csv", "csv");
                if (savePath.Length != 0)
                {
                    EasyVoiceDataImporter.ExportData(savePath);
                }
            }
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button(new GUIContent("Export data to file...", "You cannot export 0 lines, please create at least 1 voice line before exporting"), GUILayout.Width(200));
            GUI.enabled = true;
        }

        if (GUILayout.Button("Import data from file...", GUILayout.Width(200)))
        {
            int option = -66;
            if (settings.data.LineCount() != 0)
                option = EditorUtility.DisplayDialogComplex("Importing over existing data", "Warning: Importing data from file will overwrite your existing lines, are you sure? You may also add to existing lines.", "Overwrite", "Append", "Cancel");

            if (option == -66 || option == 0 || option == 1)
            {
                string loadPath = EditorUtility.OpenFilePanel("Import EasyVoice CSV file", "", "csv");
                if (loadPath.Length != 0)
                {
                    if (EasyVoiceDataImporter.ImportData(loadPath, settings, option == 1) == EasyVoiceDataImporter.ImportResult.matchingClipsFound)
                    {
                        if (settings.linkClips)
                            if (EditorUtility.DisplayDialog("Link matching AudioClip assets?", "Your imported lines have file names that match existing AudioClips, do you want to automatically link these clips?", "Yes", "No"))
                                EasyVoiceDataImporter.LinkClipsAfterImport();
                    }
                }
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        settings.exportCSVFileEncodingUTF8 = EditorGUILayout.ToggleLeft("Set file to UTF8 encoding", settings.exportCSVFileEncodingUTF8, GUILayout.Width(170));

        GUILayout.Label("(this will preserve non-ASCII characters, but some software may be incompatible with this setting)", OurStyles.WordWrappedLabel);
        
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();


        EditorGUILayout.Separator();



        EditorGUILayout.BeginVertical(OurStyles.SettingsBox);

        GUILayout.Label("Reference", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        GUILayout.Label("External links", GUILayout.Width(100));

        if (GUILayout.Button("Documentation", GUILayout.Width(200)))
            Application.OpenURL(EasyVoiceSettings.documentationUrl);

        if (GUILayout.Button("Help/support", GUILayout.Width(200)))
            Application.OpenURL(EasyVoiceSettings.forumUrl);

        if (GUILayout.Button("Write a Review", GUILayout.Width(200)))
            Application.OpenURL(EasyVoiceSettings.assetUrl);

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();


        advancedSettings = EditorGUILayout.Foldout(advancedSettings, "Advanced settings");
        if (advancedSettings)
        {
            EditorGUILayout.BeginVertical(OurStyles.SettingsBox);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Force text-to-speech platform", GUILayout.Width(labelWidth));
            settings.SetDefaultQuerier((DefaultQuerier)EditorGUILayout.EnumPopup(settings.defaultQuerier, GUILayout.Width(200)));
            GUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Warning: selecting incompatible platform will cause speech generation to fail. Use this if Unity/EasyVoice cannot detect your platform automatically.", MessageType.Warning);

            if (settings.querier != null)
            {
                GUILayout.Label("Currently used text-to-speech platform: " + settings.querier.Name);
            }
            else
            {
                GUILayout.Label("Currently used text-to-speech: -none-");
            }

            List<string> voiceNames = settings.voiceNames; // thread safer
            if (voiceNames != null)
            {
                GUILayout.Label("Currently available speaker names:");
                foreach (string voiceName in voiceNames)
                {
                    GUILayout.Label("* " + voiceName);
                }
            }
            else
            {
                GUILayout.Label("No speakers are available");
            }

            // DEBUG
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Mac OS X output file format:", GUILayout.Width(200));
            settings.osxFileFormat = EditorGUILayout.TextField(settings.osxFileFormat, GUILayout.Width(200));
            if (GUILayout.Button("Default", GUILayout.Width(80)))
            {
                settings.osxFileFormat = "AIFFLE";
                GUI.FocusControl("SettingsToggle"); // Make sure we don't mess up focused field values
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginVertical(OurStyles.SettingsBox);

            GUILayout.Label("If you want to manually unlink (forget) the current data asset, you can unlink it and the lines tab will offer you to create a new one or select another", OurStyles.WordWrappedLabel);
            if (GUILayout.Button("Unlink current data asset", GUILayout.Width(200)))
            {
                settings.UnlinkDataAsset();
                currentTab = WindowTab.Lines; // because afterwards, creating new on linking will go back to settings
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private uint[] sampleRates = { 8000, 11025, 22050, 44100, 48000, 96000, 192000 };
    private string[] sampleRateNames = { "8,000 Hz", "11,025 Hz", "22,050 Hz", "44,100 Hz", "48,000 Hz", "96,000 Hz", "192,000 Hz" };

    private uint SampleRatePopup(uint value)
    {
        int index = -1;
        for (int i = 0; i < sampleRates.Length; i++)
        {
            if (sampleRates[i] == value)
            {
                index = i;
                break;
            }
        }
        index = EditorGUILayout.Popup(index, sampleRateNames, GUILayout.Width(200));
        return index >= 0 ? sampleRates[index] : sampleRates[3]; //44.1 Khz
    }


    private void FindOrCreateData()
    {
        EasyVoiceDataAsset foundAsset = (EasyVoiceDataAsset)AssetDatabase.LoadAssetAtPath("Assets/" + EasyVoiceSettings.dataAssetName, typeof(EasyVoiceDataAsset));
        if (foundAsset != null)
        {
            AssignDataAssetToSettings(foundAsset);
            EditorUtility.SetDirty(settings);
        }
        else
        {
            CreateDataAssetForSettings();
            if (settings.data != null)
                settings.data.AddNewLine();
            EditorUtility.SetDirty(settings);
        }
    }

    private void OnGUI_IntegrityCheckFail()
    {
        EditorGUILayout.HelpBox("There is a problem with your EasyVoice data asset (\"" + AssetDatabase.GetAssetPath(settings.data) + "\")!", MessageType.Error);

        GUILayout.Label(settings.data.IntegrityErrorReport());

        EditorGUILayout.Separator();

        GUILayout.Label("Unfortunatelly, this cannot be reliably resolved automatically (possible data loss), please verify your asset manually (check data in inspector in debug mode).");

        GUILayout.Label("(The internal data may have null lists or mismatched list item counts, this can be corrected manually by filling the data in the inspector.)");
    }

    private void OnGUI_FullDataAssetSelection()
    {
        EditorGUILayout.HelpBox("EasyVoice could not find or automatically create its data asset!", MessageType.Warning);

        EditorGUILayout.Separator();

        EasyVoiceDataAsset foundAsset = (EasyVoiceDataAsset)AssetDatabase.LoadAssetAtPath("Assets/" + EasyVoiceSettings.dataAssetName, typeof(EasyVoiceDataAsset));
        if (foundAsset != null)
        {
            EditorGUILayout.BeginVertical(OurStyles.SettingsBox);
            GUILayout.Label("You can use the existing data asset at \"Assets/" + EasyVoiceSettings.dataAssetName + "\":");
            if (GUILayout.Button("Use existing data (recommended)", GUILayout.Width(300)))
            {
                AssignDataAssetToSettings(foundAsset);
                EditorUtility.SetDirty(settings);
                currentTab = WindowTab.Lines; // switch to lines if we were in settings
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }

        EditorGUILayout.BeginVertical(OurStyles.SettingsBox);
        GUILayout.Label("You may want to try and create a new one at \"Assets/" + EasyVoiceSettings.dataAssetName + "\":");
        if (GUILayout.Button("Create data asset" + (foundAsset == null ? " (recommended)" : ""), GUILayout.Width(300)))
        {
            EasyVoiceDataAsset existingAsset = AssetDatabase.LoadAssetAtPath("Assets/" + EasyVoiceSettings.dataAssetName, typeof(EasyVoiceDataAsset)) as EasyVoiceDataAsset;

            bool okay = true;

            if (existingAsset != null)
            {
                okay = EditorUtility.DisplayDialog("Overwrite existing asset?", "There already is an \"Assets/" + EasyVoiceSettings.dataAssetName + "\" asset, do you want to overwrite it?", "Yes", "No");
            }

            if (okay) CreateDataAssetForSettings();
            EditorUtility.SetDirty(settings);
        }
        GUILayout.Label("(You may move, rename or assign different data later)");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        EditorGUILayout.BeginVertical(OurStyles.SettingsBox);
        GUILayout.Label("Or assign an existing data asset from another location -- drop it here:");
        EasyVoiceDataAsset newDataAsset = (EasyVoiceDataAsset)EditorGUILayout.ObjectField(null, typeof(EasyVoiceDataAsset), false, GUILayout.Width(300));
        if (newDataAsset != null)
        {
            AssignDataAssetToSettings(newDataAsset);
            EditorUtility.SetDirty(settings);
        }
        EditorGUILayout.EndVertical();
    }

    private void CreateDataAssetForSettings()
    {
        settings.data = CreateInstance<EasyVoiceDataAsset>();
        //dataAsset.Initialize();
        if (settings.data != null)
        {
            settings.data.FirstInitialize();
            AssetDatabase.CreateAsset(settings.data, "Assets/" + EasyVoiceSettings.dataAssetName);
            Debug.Log("Your EasyVoice data asset was successfully created at '" + "Assets/" + EasyVoiceSettings.dataAssetName + "', you may move it if you wish.");
        }
        else
        {
            Debug.LogError("Failed to create an EasyVoice data asset!");
        }
    }

    private void OnGUI_SettingsAssetSelection()
    {
        EditorGUILayout.HelpBox("EasyVoice could not find or create its settings asset at \"" + EasyVoiceSettings.settingAssetName + "\"!", MessageType.Error);
        if (GUILayout.Button("Try to find or re-create settings asset"))
        {
            FindOrCreateSettings();
        }

        GUILayout.Label("Or assign an existing setting asset from another location -- drop it here:");
        EasyVoiceSettings newSettingsAsset = (EasyVoiceSettings)EditorGUILayout.ObjectField(null, typeof(EasyVoiceSettings), false);
        if (newSettingsAsset != null)
        {
            settings = newSettingsAsset;
            //settings.Hide();
            EditorUtility.SetDirty(settings);
        }
        GUILayout.Label("Note: If the settings asset is not at \"" + EasyVoiceSettings.settingAssetName + "\", EasyVoice won't find it next run.");
    }
}