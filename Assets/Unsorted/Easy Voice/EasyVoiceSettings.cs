/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

//#define DEBUG_MESSAGES

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
// using UnityEditor; -- NO!

[Serializable]
public class EasyVoiceSettings : ScriptableObject
{
    public static EasyVoiceSettings instance; // singleton

    public const int easyVoiceVersion = 7;

    public const string defaultSpeakerNameString = "<*default*>";
    public const string defaultFileNameString = "<*default*>";
    //public const string easyvoiceAssetFolder = "Assets/Easy Voice/";
    public const string dataAssetName = "EasyVoiceData.asset"; // .asset is needed for user-made data assets
    public const string settingAssetName = "Assets/Easy Voice/EasyVoiceSettings.asset"; // .asset is needed for user-made data assets

    public const string progressDialogTitle = "Creating voice files...";
    public const string progressDialogText = "Please hold tight while we are generating your voice assets.";
    public const string documentationUrl = @"https://game-loop.com/easy-voice/documentation/";
    public const string forumUrl = @"https://game-loop.com/forum/";
    public const string assetUrl = @"http://u3d.as/9vL";

    [SerializeField]
    public EasyVoiceDataAsset data;

    [SerializeField]
    public DefaultQuerier defaultQuerier;

    public VerificationActions verificationActions;

    public List<string> voiceNames;
    public List<string> voiceDescriptions;
    public List<string> voiceGenders;
    public List<string> voiceAges;

    public string defaultVoice = defaultSpeakerNameString;

    public string defaultFolder = "/SFX/Voice/";

    public string baseDefaultFileName = "$ voice #";

    public bool linkClips = true;
    
    public bool importClips3D = true;

    [Serializable]
    public class AudioImportSettings
    {
        public bool applyCustomSettings = true;

        public bool forceToMono;// = false;

        public bool loadInBackground;// = false;

        public bool preloadAudioData = true;

        public enum LoadType
        {
            DecompressOnLoad,
            CompressedInMemory,
            Streaming,
        }

        public LoadType loadType = LoadType.DecompressOnLoad;

        public enum CompressionFormat
        {
            PCM,
            Vorbis,
            ADPCM,
            //MP3, -- Unity doesn't support this for overall/default settings
            //VAG, -- Unity doesn't support this for overall/default settings
            //HEVAG, -- Unity doesn't support this for overall/default settings
        }

        public CompressionFormat compressionFormat = CompressionFormat.Vorbis;

        public float quality = 1f;

        public enum SampleRate
        {
            PreserveSampleRate,
            OptimizeSampleRate,
            OverrideSampleRate,
        }

        public SampleRate sampleRateSetting = SampleRate.PreserveSampleRate;

        public uint overrideSampleRate = 44100; // Unity doesn't have a default
    }


    public AudioImportSettings audioImportSettings = new AudioImportSettings();
    
    // Outdated -- remove eventually
    public _AudioImporterFormat importClipsFormat = _AudioImporterFormat.Native;

    // Outdated -- remove eventually
    public _AudioImporterLoadType importClipsLoadType = _AudioImporterLoadType.CompressedInMemory;


    public bool exportCSVFileEncodingUTF8;

    public string osxFileFormat = "AIFFLE";


    public EasyVoiceQuerier querier;


    private bool queriedForVoiceList;

    public void Awake()
    {
        SetInstance();
    }

    public void SetInstance()
    {
        instance = this;
    }

    public void Initialize()
    {
#if DEBUG_MESSAGES
        Debug.Log("Initializing settings (querier = " + queriedForVoiceList + ")");
#endif

        //Hide();

        instance = this;

        if (querier == null)
        {
            InitializeQuerier();
        }

        if (!queriedForVoiceList)
        {
            if (querier != null)
            {
                queriedForVoiceList = true;
                querier.QueryForVoiceList(this);
            }
        }

        //if (defaultVoice == defaultSpeakerNameString)
        //    defaultVoice = GetFirstSpeakerNameOrDefault();
    }

    private void InitializeQuerier()
    {
        if (querier != null)
            return;

#if DEBUG_MESSAGES
        Debug.Log("Selecting querier as " + defaultQuerier + "...");
#endif
        switch (defaultQuerier)
        {
            case DefaultQuerier.Automatic:
#if DEBUG_MESSAGES
                Debug.Log("Selecting querier automatically for " + Application.platform);
#endif
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXEditor:
                        querier = new EasyVoiceQuerierMacOS();
                        break;
                    case RuntimePlatform.OSXPlayer:
                        querier = new EasyVoiceQuerierMacOS();
                        break;
                    case RuntimePlatform.WindowsPlayer:
                        querier = new EasyVoiceQuerierWinOS();
                        break;
                    case RuntimePlatform.WindowsEditor:
                        querier = new EasyVoiceQuerierWinOS();
                        break;

                    default:
                        break;
                }
                break;

            case DefaultQuerier.DefaultWindowsOS:
                querier = new EasyVoiceQuerierWinOS();
                break;

            case DefaultQuerier.DefaultMacOS:
                querier = new EasyVoiceQuerierMacOS();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool ValidVoiceName(string speakerName, bool allowDefault)
    {
        return (allowDefault && speakerName == defaultSpeakerNameString) || (voiceNames != null && voiceNames.Contains(speakerName));
    }

    public int GetSpeakerIndex(string currentSpeakerName, bool includeDefault)
    {
        if (voiceNames == null)
            return 0;

        return includeDefault ? voiceNames.IndexOf(currentSpeakerName) + 1 : voiceNames.IndexOf(currentSpeakerName);
    }

    public string[] GetSpeakerNamesForEditor(bool includeDefault, bool forSystem)
    {
        if (voiceNames == null || voiceNames.Count == 0)
        {
            return includeDefault ? new string[] { forSystem ? "System default" : "Default", "Custom" } : new string[] { "Custom" };
        }
        else
        {
            string[] temp = new string[includeDefault ? voiceNames.Count + 2 : voiceNames.Count + 1];
            if (includeDefault) temp[0] = forSystem ? "System default" : "Default";
            for (int i = 0; i < voiceNames.Count; i++)
            {
                temp[includeDefault ? i + 1 : i] = voiceNames[i];
            }
            temp[includeDefault ? voiceNames.Count + 1 : voiceNames.Count] = "Custom";
            return temp;
        }
    }

    public string GetSpeakerName(int index, bool defaultIncluded)
    {
        if (defaultIncluded && (index == 0 || index > voiceNames.Count + 1))
            return defaultSpeakerNameString;
        if (index == (defaultIncluded ? voiceNames.Count + 1 : voiceNames.Count))
            return "";
        return voiceNames[defaultIncluded ? index - 1 : index];
    }

    public void SetDefaultQuerier(DefaultQuerier newValue)
    {
        if (newValue != defaultQuerier)
        {
            defaultQuerier = newValue;
            querier = null;
            ClearQueriedData();
            queriedForVoiceList = false;
            Initialize(); // will also select new querier
        }
    }

    private void ClearQueriedData()
    {
        voiceNames = null;
        voiceDescriptions = null;
        voiceGenders = null;
        voiceAges = null;
    }

    //public string MakeFileName(string speakerName, List<string> fileNames)
    //{
    //    for (int i = 1; i < 10000; i++) -- using ids now
    //    {
    //        string tempName = MakeFileName(speakerName, );
    //        if (!fileNames.Contains(tempName))
    //        {
    //            return tempName;
    //        }
    //    }
    //    Debug.LogWarning("EasyVoice couldn't find a unique default file name after 10k attempts.");
    //    return MakeFileName(speakerName, 0); // 10k lines... sure
    //}

    public string MakeFileNameFromTemplate(string speakerName, int index)
    {
        if (speakerName == defaultSpeakerNameString)
        {
            if (defaultVoice == defaultSpeakerNameString)
                return BuildFileNameFromTemplate("Default", index);
            else
                return BuildFileNameFromTemplate(defaultVoice, index);
        }
        else
        {
            return EasyVoiceDataAsset.SanitizeFileName(BuildFileNameFromTemplate(speakerName, index));
        }
    }

    /// <summary> Make a file name from the default file name template and the specified repalcement speaker name and entry index </summary>
    private string BuildFileNameFromTemplate(string speakerName, int index)
    {
        string defaultFileName = GetDefaultFileName();

        // We want to replace ### with 003
        // (GetDefaultFileName always returns at least 1 #)

        int firstHashIndex = defaultFileName.IndexOf('#');
        int lastHashIndex = defaultFileName.LastIndexOf('#');

        return
            (defaultFileName.Substring(0, firstHashIndex) + index.ToString("D" + (lastHashIndex - firstHashIndex + 1)) + defaultFileName.Substring(lastHashIndex + 1))
            .Replace("$", speakerName);
    }

    public string GetFirstSpeakerNameOrDefault()
    {
        if (voiceNames.Count > 0)
            return voiceNames[0];
        else
            return defaultSpeakerNameString;
    }

    public void AssignDataAsset(EasyVoiceDataAsset newDataAsset)
    {
        if (newDataAsset.dataVersion < easyVoiceVersion)
        {
            data = newDataAsset;
            UpgradeAsset();
            Debug.Log("Your backwards-compatible EasyVoice data asset was successfully assigned from '" + "Assets/" + dataAssetName + "' and upgraded, you may now edit it.");
            //EditorUtility.SetDirty(this);
        }
        else if (newDataAsset.dataVersion > easyVoiceVersion)
        {
            Debug.LogError("Selected EasyVoice data asset version (" + newDataAsset.dataVersion + ") is higher than our version (" + easyVoiceVersion + "), please update the plugin before importing!");
            // We could let them import it... If there are no compile errors then stuff might be still compatible
            // The problem is, if there are issues, they won't be obvious or immediate as much as corrupt everything, which we really want to avoid
        }
        else
        {
            data = newDataAsset;
            //Debug.Log("Selected EasyVoice data asset was successfully assigned from '" + "Assets/" + EasyVoiceSettings.dataAssetName + "', you may now edit your lines.");
            //EditorUtility.SetDirty(this);
        }
    }
    
    public void UpgradeAsset()
    {
        Upgrade();

        Debug.Log("Upgrading EasyVoice data asset from version " + data.dataVersion + " to " + easyVoiceVersion);

        data.Upgrade(easyVoiceVersion);
    }

    /// <summary>
    /// This will, if necessary, upgrade the settings to the given version
    /// </summary>
    private void Upgrade()
    {
        bool changed = false;

        if (importClipsFormat != _AudioImporterFormat.Outdated)
        {
            switch (importClipsFormat)
            {
                case _AudioImporterFormat.Compressed:
                    audioImportSettings.compressionFormat = AudioImportSettings.CompressionFormat.Vorbis; // default, compressed
                    changed = true;
                    break;
                case _AudioImporterFormat.Native:
                    audioImportSettings.compressionFormat = AudioImportSettings.CompressionFormat.PCM; // "Uncompressed pulse-code modulation."
                    changed = true;
                    break;
            }
        }

        if (importClipsLoadType != _AudioImporterLoadType.Outdated)
        {
            switch (importClipsLoadType)
            {
                case _AudioImporterLoadType.CompressedInMemory:
                    audioImportSettings.loadType = AudioImportSettings.LoadType.CompressedInMemory;
                    changed = true;
                    break;
                case _AudioImporterLoadType.DecompressOnLoad:
                    audioImportSettings.loadType = AudioImportSettings.LoadType.DecompressOnLoad;
                    changed = true;
                    break;
                case _AudioImporterLoadType.StreamFromDisc:
                    audioImportSettings.loadType = AudioImportSettings.LoadType.Streaming; // I guess, no longer implies from disc?
                    changed = true;
                    break;
            }
        }

        if (!importClips3D)
        {
            audioImportSettings.forceToMono = true;
            changed = true;
        }

        if (changed)
        {
            Debug.Log("Upgraded EasyVoice settings to version " + easyVoiceVersion + ". Audio import settings in Unity 5 have changed and have been converted.");
        }
    }

    public bool SetDefaultFolder(string newValue)
    {
        if (defaultFolder != newValue)
        {
            defaultFolder = newValue;
            foreach (char c in Path.GetInvalidPathChars())
                defaultFolder = defaultFolder.Replace(c.ToString(), "");
            data.MakeSureAllDefaultFileNamesAreUnique();
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsDefaultFolderValid()
    {
        return defaultFolder.Length > 0 && defaultFolder.StartsWith("/") && defaultFolder.EndsWith("/");
    }

    public string GetFullClipDefaultPath()
    {
        return Application.dataPath + defaultFolder;
    }

    public void UnlinkDataAsset()
    {
        data = null;
    }

    public string GetDefaultFileName()
    {
        return baseDefaultFileName.IndexOf('#') != -1 ? baseDefaultFileName : baseDefaultFileName + " #";
    }

    public bool SetBaseDefaultFileName(string newValue)
    {
        newValue = EasyVoiceDataAsset.SanitizeFileName(newValue);
        newValue = newValue.Replace("\r\n", " ").Replace('\r', ' ').Replace('\r', ' ').Replace('\t', ' ').Trim();
        newValue = TrimStringFromRepeatedCharacters(newValue);
        if (newValue.IndexOf('#') == -1) newValue += " #";
        if (baseDefaultFileName != newValue)
        {
            baseDefaultFileName = newValue;
            data.MakeSureAllDefaultFileNamesAreUnique();
            return true;
        }
        else
        {
            return false;
        }
    }

    private string TrimStringFromRepeatedCharacters(string str)
    {
        StringBuilder stringBuilder = new StringBuilder(str.Length);
        bool hadHash = false;
        bool hadDollar = false;
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == '#')
            {
                if (!hadHash || str[i - 1] == '#') // okay to have a string of hashes
                {
                    hadHash = true;
                    stringBuilder.Append(str[i]);
                }
            }         
            else if (str[i] == '$')
            {
                if (!hadDollar)
                {
                    hadDollar = true;
                    stringBuilder.Append(str[i]);
                }
            }
            else
            {
                stringBuilder.Append(str[i]);
            }
        }
        return stringBuilder.ToString();
    }

    public bool SetDefaultVoice(string newValue)
    {
        //Debug.Log("Setting default voice to " + newValue + " (was " + defaultVoice + ")");
        newValue = newValue.Replace("\r\n", " ").Replace('\r', ' ').Replace('\r', ' ').Replace('\t', ' ').Trim();
        if (defaultVoice != newValue)
        {
            defaultVoice = newValue;
            data.MakeSureAllDefaultFileNamesAreUnique();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetLinkClips(bool newValue)
    {
        //if (linkClips != newValue) -- redundant if we don't act on it
        {
            linkClips = newValue;
            // VERIFY? technically issues stay the same, it is editor window that chooses to not display them
        }
    }
}

public enum DefaultQuerier
{
    Automatic,
    DefaultWindowsOS,
    DefaultMacOS
}

public enum _AudioImporterFormat { Compressed, Native, Outdated }

public enum _AudioImporterLoadType { CompressedInMemory, DecompressOnLoad, StreamFromDisc, Outdated }