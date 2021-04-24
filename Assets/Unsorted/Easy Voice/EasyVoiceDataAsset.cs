/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

//#define DEBUG_MESSAGES

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
// using UnityEditor; -- NO!

[Flags]
public enum LineIssue
{
    emptyLine = 1 << 0,
    badFileName = 1 << 1,
    duplicateBaseFileName = 1 << 2,
    duplicateClipReference = 1 << 3,
    duplicateAssetFileName = 1 << 4,
    clashingExistingAsset = 1 << 5,
    invalidSpeaker = 1 << 6
}

[Serializable]
public class EasyVoiceDataAsset : ScriptableObject
{
    public int dataVersion = 1;

    [SerializeField] private List<int> ids;
    [SerializeField] private List<string> groups;
    [SerializeField] private List<byte> statuses;
    [SerializeField] private List<string> speakerNames;
    [SerializeField] private List<string> speechTexts;
    [SerializeField] private List<string> fileNames;
    [SerializeField] private List<AudioClip> clips;
    [SerializeField] private List<LineIssue> issues;

    public void FirstInitialize()
    {
        dataVersion = EasyVoiceSettings.easyVoiceVersion;
    }

    private void OnEnable()
    {
        if (ids == null || groups == null || statuses == null || speakerNames == null || speechTexts == null || fileNames == null || clips == null)
        {
            // This means constructor (which we don't have) didn't make them and Unity didn't deserialize them from anything (perhaps we don't have serialized data)
            ids = new List<int>();
            groups = new List<string>();
            statuses = new List<byte>();
            speakerNames = new List<string>();
            speechTexts = new List<string>();
            fileNames = new List<string>();
            clips = new List<AudioClip>();
            issues = new List<LineIssue>();
        }

        // We have an asset object, so Unity actually saves it in assets and we don't need to manually hide flag it
        //hideFlags = 
    }

    /// <summary>
    /// This will return how many lines there are currently stored
    /// </summary>
    public int LineCount()
    {
        if (speechTexts == null) return 0; // graceful
        return speechTexts.Count;
    }

    public int GetId(int index)
    {
        if (index < 0 || index >= ids.Count) return 0;
        return ids[index];
    }

    public void SetId(int index, int newValue)
    {
        if (index < 0 || index >= ids.Count) return;
        ids[index] = newValue;
    }

    public string GetGroup(int index)
    {
        if (index < 0 || index >= groups.Count) return "";
        return groups[index];
    }

    public void SetGroup(int index, string newValue)
    {
        if (index < 0 || index >= groups.Count) return;
        newValue = newValue.Replace("\r\n", " ").Replace('\r', ' ').Replace('\r', ' ');
        if (groups[index] != newValue) SetOutputStatus(index, true);
        groups[index] = newValue;
    }

    public byte GetStatus(int index)
    {
        if (index < 0 || index >= statuses.Count) return 0;
        return statuses[index];
    }

    public void SetStatus(int index, byte newValue)
    {
        if (index < 0 || index >= statuses.Count) return;
        statuses[index] = newValue;
    }

    public bool GetOutputStatus(int index)
    {
        if (index < 0 || index >= statuses.Count) return false;
        return statuses[index] == 1; // for now
    }

    public void SetOutputStatus(int index, bool newValue)
    {
        if (index < 0 || index >= statuses.Count) return;
        statuses[index] = newValue ? (byte)1 : (byte)0; // for now
    }

    /// <summary>
    /// Note: This will not replace any default values
    /// </summary>
    public string GetSpeakerName(int index)
    {
        if (index < 0 || index >= speakerNames.Count) return "";
        return speakerNames[index];
    }

    public string GetSpeakerNameOrSelectDefault(int index)
    {
        if (index < 0 || index >= speakerNames.Count) return "";
        if (EasyVoiceSettings.instance != null)
        {
            if (speakerNames[index] == EasyVoiceSettings.defaultSpeakerNameString)
                return EasyVoiceSettings.instance.defaultVoice == EasyVoiceSettings.defaultSpeakerNameString ? "" : EasyVoiceSettings.instance.defaultVoice;
        }
        return speakerNames[index];
    }

    public void SetSpeakerName(int index, string newValue)
    {
        if (index < 0 || index >= speakerNames.Count) return;
        if (newValue == null) newValue = "";
        newValue = newValue.Replace("\r\n", " ").Replace('\r', ' ').Replace('\r', ' ').Replace('\t', ' ').Trim();
        if (newValue.Length > 250)
            newValue = newValue.Substring(0, 250);
        if (speakerNames[index] != newValue && GetSpeechText(index) != "") SetOutputStatus(index, true);
        if (speakerNames[index] != newValue)
        {
            speakerNames[index] = newValue;
            if (EasyVoiceSettings.instance != null && EasyVoiceSettings.instance.verificationActions != null && EasyVoiceSettings.instance.verificationActions.speakerNameVerificationFunction != null) EasyVoiceSettings.instance.verificationActions.speakerNameVerificationFunction.Invoke(index);
            MakeSureDefaultFileNameIsUnique(index);
            MakeSureOtherFileNamesAreUnique(index);
            if (EasyVoiceSettings.instance != null && EasyVoiceSettings.instance.verificationActions != null && EasyVoiceSettings.instance.verificationActions.fileNameOrAssetVerificationFunction != null) EasyVoiceSettings.instance.verificationActions.fileNameOrAssetVerificationFunction.Invoke(index);
        }
    }

    public string GetSpeechText(int index)
    {
        if (index < 0 || index >= speechTexts.Count) return "";
        return speechTexts[index];
    }

    public void SetSpeechText(int index, string newValue)
    {
        if (index < 0 || index >= speechTexts.Count) return;
        newValue = newValue.Replace("\r\n", " ").Replace('\r', ' ').Replace('\r', ' ');
        if (newValue.Length > 3600)
            newValue = newValue.Substring(0, 3600);
        if (newValue != speechTexts[index])
        {
            SetOutputStatus(index, true);
            speechTexts[index] = newValue;
            if (EasyVoiceSettings.instance != null && EasyVoiceSettings.instance.verificationActions != null && EasyVoiceSettings.instance.verificationActions.speechTextVerificationFunction != null) EasyVoiceSettings.instance.verificationActions.speechTextVerificationFunction.Invoke(index);
        }
    }

    /// <summary>
    /// NOTE: This will not return a default file name correctly filled, use GetFileNameOrDefault() for that
    /// </summary>
    public string GetFileName(int index)
    {
        if (index < 0 || index >= fileNames.Count) return "";
        return fileNames[index];
    }

    public string GetFileNameOrDefault(int index)
    {
        if (index < 0 || index >= fileNames.Count) return "";
        if (EasyVoiceSettings.instance != null)
        {
            if (fileNames[index] == EasyVoiceSettings.defaultFileNameString)
                return EasyVoiceSettings.instance.MakeFileNameFromTemplate(GetSpeakerName(index), GetId(index));
        }
        return fileNames[index];
    }

    public void SetFileName(int index, string newValue, bool forceVerification, bool markChanged = true)
    {
        if (newValue == EasyVoiceSettings.defaultFileNameString)
            Debug.LogWarning("Use SetDefaultFileName() instead of SetFileName(\"" + EasyVoiceSettings.defaultFileNameString + "\") -- the value will be sanitized and not actually defaulted!");
        if (index < 0 || index >= fileNames.Count) return;
        newValue = newValue.Trim();
        if (newValue.Length > 100)
            newValue = newValue.Substring(0, 100);
        if (fileNames[index] != newValue && GetSpeechText(index) != "" && markChanged) SetOutputStatus(index, true);
        newValue = SanitizeFileName(newValue);
        if (newValue != fileNames[index] || forceVerification)
        {
            fileNames[index] = newValue;
            MakeSureOtherFileNamesAreUnique(index);
            if (EasyVoiceSettings.instance != null && EasyVoiceSettings.instance.verificationActions != null && EasyVoiceSettings.instance.verificationActions.fileNameOrAssetVerificationFunction != null) EasyVoiceSettings.instance.verificationActions.fileNameOrAssetVerificationFunction.Invoke(index);
        }
    }

    public void SetDefaultFileName(int index)
    {
        if (index < 0 || index >= fileNames.Count) return;
        if (fileNames[index] != EasyVoiceSettings.defaultFileNameString)
        {
            SetOutputStatus(index, true);
            fileNames[index] = EasyVoiceSettings.defaultFileNameString;
            MakeSureDefaultFileNameIsUnique(fileNames.Count - 1);
            if (EasyVoiceSettings.instance != null && EasyVoiceSettings.instance.verificationActions != null && EasyVoiceSettings.instance.verificationActions.fileNameOrAssetVerificationFunction != null) EasyVoiceSettings.instance.verificationActions.fileNameOrAssetVerificationFunction.Invoke(index);
        }
    }

    public static string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c.ToString(), "");
        return fileName;
    }

    public void MakeSureAllDefaultFileNamesAreUnique()
    {
        for (int i = LineCount() - 1; i >= 0; i--)
            MakeSureDefaultFileNameIsUnique(i);
    }

    /// <summary>
    /// This makes sure the current DEFAULT file name is unique to all other file names (i.e. ID is unique and non-clashing with manually typed ones)
    /// </summary>
    private void MakeSureDefaultFileNameIsUnique(int index)
    {
        if (fileNames[index] != EasyVoiceSettings.defaultFileNameString)
            return; // for MakeSureOtherFileNamesAreUnique()

        //Debug.Log("Making sure of uniqueness for #" + index + " (id = " + ids[index] + ")");

        bool unique;
        int iterations = 0;
        do
        {
            string ourFileName = GetFileNameOrDefault(index); // loop changes id, so reget
            //Debug.Log("-> Checking for clashes with our name with " + ourFileName);

            unique = true;

            if (EasyVoiceSettings.instance != null && EasyVoiceSettings.instance.verificationActions != null && EasyVoiceSettings.instance.verificationActions.assetExistsFunction != null)
            {
                if (EasyVoiceSettings.instance.verificationActions.assetExistsFunction.Invoke(index))
                {
                    ids[index] = GenerateUniqueId(index, ids[index] + 1); // try next id
                    //Debug.Log("-> -> Clashing with existing asset, making new id instead: " + ids[index]);
                    unique = false;
                }
            }

            if (unique)
            {
                for (int otherIndex = 0; otherIndex < LineCount(); otherIndex++)
                {
                    if (otherIndex != index)
                    {
                        if (ourFileName == fileNames[otherIndex]) // this should only happen on manually typed ones, others have unique ids
                        {
                            ids[index] = GenerateUniqueId(index, ids[index] + 1); // try next id
                            //Debug.Log("-> -> Clashing with #" + otherIndex + " (" + fileNames[otherIndex] + "), making new id instead: " + ids[index]);
                            unique = false;
                        }
                    }
                }
            }

            iterations++;
        } while (!unique && iterations < 500); // 500 lines/assets all clashing?
    }

    private void MakeSureOtherFileNamesAreUnique(int index)
    {
        if (fileNames[index] == EasyVoiceSettings.defaultFileNameString)
            return; // for MakeSureDefaultFileNameIsUnique()

        //Debug.Log("Making sure other file names are unique agaisnt " + fileNames[index]);

        for (int otherIndex = 0; otherIndex < LineCount(); otherIndex++)
        {
            if (otherIndex != index)
            {
                if (fileNames[otherIndex] == EasyVoiceSettings.defaultFileNameString && GetFileNameOrDefault(otherIndex) == fileNames[index]) // we can use fileNames[index] directly because we already checked we are manually typed
                    MakeSureDefaultFileNameIsUnique(otherIndex);
            }
        }
    }

    public AudioClip GetClip(int index)
    {
        if (index < 0 || index >= clips.Count) return null;
        if (clips[index] == null) clips[index] = null; // null out the Unity null override
        return clips[index];
    }

    public void SetClip(int index, AudioClip newClip, bool markChanged = false)
    {
        if (index < 0 || index >= clips.Count) return;

        if (newClip == null)
        {
            ClearClip(index, markChanged);
            return;
        }

        if (markChanged && clips[index] != newClip) SetOutputStatus(index, true);
        if (clips[index] != newClip)
        {
            clips[index] = newClip;
            SetFileName(index, clips[index].name, true, markChanged);
        }
    }

    public void ClearClip(int index, bool markChanged = false)
    {
        if (index < 0 || index >= clips.Count) return;
        if (markChanged && clips[index] != null) SetOutputStatus(index, true);
        if (clips[index] != null)
        {
            clips[index] = null;
            SetFileName(index, fileNames[index], true); // redo, mostly for issues
        }
    }

    public LineIssue GetIssues(int index)
    {
        if (index < 0 || index >= issues.Count) return 0;
        return issues[index];
    }

    public void SetIssues(int index, LineIssue newIssues)
    {
        if (index < 0 || index >= clips.Count) return;
        issues[index] = newIssues;
    }

    public void SetIssue(int index, LineIssue chosenIssue, bool newValue)
    {
        if (index < 0 || index >= clips.Count) return;
        if (newValue)
            issues[index] |= chosenIssue;
        else
            issues[index] &= ~chosenIssue;
    }

    public bool HasIssue(int index, LineIssue saughtIssues)
    {
        if (index < 0 || index >= clips.Count) return false;
        return (issues[index] & saughtIssues) == saughtIssues;
    }

    public int AddNewLine()
    {
        issues.Add(0); // first, since other will try to fill this
        ids.Add(-1);
        groups.Add("");
        statuses.Add(0); // second, since others will trigger this
        speakerNames.Add("");
        speechTexts.Add("");
        fileNames.Add("");
        clips.Add(null);
        SetId(ids.Count - 1, GenerateUniqueId(ids.Count - 1)); // before file name
        SetSpeakerName(speakerNames.Count - 1, EasyVoiceSettings.defaultSpeakerNameString);
        SetSpeechText(speechTexts.Count - 1, "");
        SetDefaultFileName(fileNames.Count - 1); // after speaker name
        MakeSureDefaultFileNameIsUnique(fileNames.Count - 1); // after file names, since those changed
        SetOutputStatus(statuses.Count - 1, false); // don't actually mark first line for output
        return fileNames.Count - 1;
    }

    public int AddNewLine(int givenId, string givenGroup, byte givenStatus, string givenName, string givenText, string givenFileName, LineIssue givenIssues)
    {
        issues.Add(0); // first, since other will try to fill this
        ids.Add(-1);
        groups.Add("");
        statuses.Add(1);
        speakerNames.Add("");
        speechTexts.Add("");
        fileNames.Add("");
        SetGroup(givenId, givenGroup);
        SetSpeakerName(speakerNames.Count - 1, givenName);
        SetSpeechText(speechTexts.Count - 1, givenText);
        if (givenFileName == EasyVoiceSettings.defaultFileNameString)
            SetDefaultFileName(fileNames.Count - 1); // after speaker name
        else
            SetFileName(fileNames.Count - 1, givenFileName, true); // after speaker name
        clips.Add(null);
        SetIssues(issues.Count - 1, givenIssues);
        SetId(ids.Count - 1, givenId);
        MakeSureDefaultFileNameIsUnique(fileNames.Count - 1); // after file names, since those changed
        SetStatus(statuses.Count - 1, givenStatus); // last, so no update trigger
        return fileNames.Count - 1;
    }

    public void DeleteLine(int index, bool createEmptyLineOnNoLines = true)
    {
        if (index < 0 || index >= speakerNames.Count) return;
        ids.RemoveAt(index);
        groups.RemoveAt(index);
        statuses.RemoveAt(index);
        speakerNames.RemoveAt(index);
        speechTexts.RemoveAt(index);
        fileNames.RemoveAt(index);
        clips.RemoveAt(index);
        issues.RemoveAt(index);

        if (ids.Count == 0 && createEmptyLineOnNoLines)
            AddNewLine();
    }

    public void DeleteAllLines()
    {
        ids.Clear();
        groups.Clear();
        statuses.Clear();
        speakerNames.Clear();
        speechTexts.Clear();
        fileNames.Clear();
        clips.Clear();
        issues.Clear();
    }

    public void MoveLineUp(int index)
    {
        if (index < 1 || index >= LineCount()) return;
        Swap(index, index - 1);
    }

    public void MoveLineDown(int index)
    {
        if (index < 0 || index >= LineCount() - 1) return;
        Swap(index, index + 1);
    }
    
    public void MoveLine(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= LineCount()) return;
        if (toIndex < 0 || toIndex > LineCount()) return;
        if (fromIndex == toIndex) return;

        //Debug.Log("Inserting into " + toIndex + " from " + fromIndex);
        ids.Insert(toIndex, ids[fromIndex]);
        groups.Insert(toIndex, groups[fromIndex]);
        statuses.Insert(toIndex, statuses[fromIndex]);
        speakerNames.Insert(toIndex, speakerNames[fromIndex]);
        speechTexts.Insert(toIndex, speechTexts[fromIndex]);
        fileNames.Insert(toIndex, fileNames[fromIndex]);
        clips.Insert(toIndex, clips[fromIndex]);
        issues.Insert(toIndex, issues[fromIndex]);

        if (fromIndex > toIndex) fromIndex++;

        //Debug.Log("Removing " + fromIndex);
        ids.RemoveAt(fromIndex);
        groups.RemoveAt(fromIndex);
        statuses.RemoveAt(fromIndex);
        speakerNames.RemoveAt(fromIndex);
        speechTexts.RemoveAt(fromIndex);
        fileNames.RemoveAt(fromIndex);
        clips.RemoveAt(fromIndex);
        issues.RemoveAt(fromIndex);
    }

    private void Swap(int index, int otherIndex)
    {
        int tempId = ids[index];
        string tempGroup = groups[index];
        byte tempStatus = statuses[index];
        string tempSpeakerNames = speakerNames[index];
        string tempSpeechTexts = speechTexts[index];
        string tempFileNames = fileNames[index];
        AudioClip tempClips = clips[index];
        LineIssue tempIssues = issues[index];

        ids[index] = ids[otherIndex];
        groups[index] = groups[otherIndex];
        statuses[index] = statuses[otherIndex];
        speakerNames[index] = speakerNames[otherIndex];
        speechTexts[index] = speechTexts[otherIndex];
        fileNames[index] = fileNames[otherIndex];
        clips[index] = clips[otherIndex];
        issues[index] = issues[otherIndex];

        ids[otherIndex] = tempId;
        groups[otherIndex] = tempGroup;
        statuses[otherIndex] = tempStatus;
        speakerNames[otherIndex] = tempSpeakerNames;
        speechTexts[otherIndex] = tempSpeechTexts;
        fileNames[otherIndex] = tempFileNames;
        clips[otherIndex] = tempClips;
        issues[otherIndex] = tempIssues;
    }

    public void DuplicateLine(int index)
    {
        if (index < 0 || index >= LineCount()) return;

        ids.Insert(index + 1, GenerateUniqueId(index + 1));
        groups.Insert(index + 1, groups[index]);
        statuses.Insert(index + 1, statuses[index]);
        speakerNames.Insert(index + 1, speakerNames[index]);
        speechTexts.Insert(index + 1, speechTexts[index]);
        fileNames.Insert(index + 1, fileNames[index]);
        clips.Insert(index + 1, clips[index]);
        issues.Insert(index + 1, issues[index]);
    }

    /// <summary>
    /// This will run through all lines and return a unique int id
    /// </summary>
    private int GenerateUniqueId(int ourIndex, int minId = 1)
    {
        if (minId <= ids.Count)
        {
            for (int id = minId; id <= ids.Count; id++) // from 1 (or min), as this is actual ID value
            {
                if (!IdExists(ourIndex, id))
                {
                    return id;
                }
            }
        }

        for (int id = Mathf.Max(minId, ids.Count + 1); id <= minId + 1000; id++) // check more ids (anything more is just someone breaking things on purpose)
        {
            if (!IdExists(ourIndex, id))
            {
                return id;
            }
        }
        Debug.LogError("EasyVoice failed to generate a unique id at GenerateUniqueId()!"); // this shouldn't ever happen
        return -1;
    }

    private bool IdExists(int seekerIndex, int id)
    {
        for (int ind = 0; ind < ids.Count; ind++) // this is just iterating other ids, so 0-based index
        {
            if (ind == seekerIndex)
                continue; // don't check ourselves -- or we need 1 more iteration for id count

            if (ids[ind] == id)
            {
                return true;
            }
        }
        return false;
    }

    public void SortLinesByFileName(bool descending)
    {
        for (int i = 0; i < LineCount(); i++)
        {
            for (int k = i + 1; k < LineCount(); k++)
            {
                int compareOrdinal = String.CompareOrdinal(GetFileNameOrDefault(i), GetFileNameOrDefault(k));
                if (compareOrdinal > 0)
                {
                    if (!descending)
                        Swap(i, k);
                }
                else if (compareOrdinal < 0)
                {
                    if (descending)
                        Swap(i, k);
                }
            }
        }
        // This isn't very optimized and each swap is also not optimized
    }

    public void SortLinesBySpeaker(bool descending, EasyVoiceSettings settings)
    {
        for (int i = 0; i < LineCount(); i++)
        {
            for (int k = i + 1; k < LineCount(); k++)
            {
                int compareOrdinal = String.CompareOrdinal(GetSpeakerNameOrSelectDefault(i), GetSpeakerNameOrSelectDefault(k));
                if (compareOrdinal > 0)
                {
                    if (!descending)
                        Swap(i, k);
                }
                else if (compareOrdinal < 0)
                {
                    if (descending)
                        Swap(i, k);
                }
            }
        }
        // This isn't very optimized and each swap is also not optimized
    }

    /// <summary>
    /// This will, if necessary, upgrade the data asset to the given version
    /// There is no need to call this manually and Easy Voice will upgrade the asset automatically, when version mismatch is detected
    /// </summary>
    public void Upgrade(int newVersion)
    {
        if (newVersion <= 1)
            return; // Nothing to upgrade

        if (dataVersion < 2 && newVersion >= 2)
        {
            // Before v2, we didn't have clip references
            clips = new List<AudioClip>();
            for (int i = 0; i < LineCount(); i++)
                clips.Add(null);
        }

        if (dataVersion < 3 && newVersion >= 3)
        {
            // Before v3, we didn't have statuses
            statuses = new List<byte>();
            for (int i = 0; i < LineCount(); i++)
                statuses.Add(1);
        }

        if (dataVersion < 4 && newVersion >= 4)
        {
            // Before v4, we didn't have ids
            ids = new List<int>();
            for (int i = 0; i < LineCount(); i++)
            {
                ids.Add(-1);
                ids[i] = GenerateUniqueId(i);
            }
        }

        if (dataVersion < 5 && newVersion >= 5)
        {
            // Before v5, we didn't have groups
            groups = new List<string>();
            for (int i = 0; i < LineCount(); i++)
            {
                groups.Add("");
                groups[i] = "";
            }
        }

        if (dataVersion < 6 && newVersion >= 6)
        {
            // Before v6, we didn't have issues
            issues = new List<LineIssue>();
            for (int i = 0; i < LineCount(); i++)
            {
                issues.Add(0);
            }
        }

        dataVersion = newVersion;

        // TODO: VERIFY ALL ISSUES? (for now, there are no changes to versions that can cause or clear any issues)
    }

    public bool VerifyIntegrity()
    {
        return
            ids != null &&
            groups != null && groups.Count == ids.Count &&
            statuses != null && statuses.Count == ids.Count &&
            speakerNames != null && speakerNames.Count == ids.Count &&
            speechTexts != null && speechTexts.Count == ids.Count &&
            fileNames != null && fileNames.Count == ids.Count &&
            clips != null && clips.Count == ids.Count &&
            issues != null && issues.Count == ids.Count;
    }

    public string IntegrityErrorReport()
    {
        string str = "";
        if (ids == null) str += "The id list is null";
        else 
        {
            if (groups == null) str += "The groups list is null\r\n"; else if (groups.Count != ids.Count) str += "The groups list item count does not match the expected number\r\n";
            if (statuses == null) str += "The statuses list is null\r\n"; else if (statuses.Count != ids.Count) str += "The statuses list item count does not match the expected number\r\n";
            if (speakerNames == null) str += "The speakerNames list is null\r\n"; else if (speakerNames.Count != ids.Count) str += "The speakerNames list item count does not match the expected number\r\n";
            if (speechTexts == null) str += "The speechTexts list is null\r\n"; else if (speechTexts.Count != ids.Count) str += "The speechTexts list item count does not match the expected number\r\n";
            if (fileNames == null) str += "The fileNames list is null\r\n"; else if (fileNames.Count != ids.Count) str += "The fileNames list item count does not match the expected number\r\n";
            if (clips == null) str += "The clips list is null\r\n"; else if (clips.Count != ids.Count) str += "The clips list item count does not match the expected number\r\n";
            if (issues == null) str += "The issues list is null\r\n"; else if (issues.Count != ids.Count) str += "The issues list item count does not match the expected number\r\n";
        }
        return str;
    }
}

/// <summary>
/// This is a quick access pointer to the issue checker functions -- these are editor namespace -- so in order to call them properly, we need to pass them like so
/// </summary>
public class VerificationActions
{
    public readonly Action<int> speakerNameVerificationFunction;
    public readonly Action<int> speechTextVerificationFunction;
    public readonly Action<int> fileNameOrAssetVerificationFunction;
    public readonly Func<int, bool> assetExistsFunction;

    public VerificationActions(Action<int> speakerNameVerificationFunction, Action<int> speechTextVerificationFunction, Action<int> fileNameOrAssetVerificationFunction, Func<int, bool> assetExistsFunction)
    {
        this.speakerNameVerificationFunction = speakerNameVerificationFunction;
        this.speechTextVerificationFunction = speechTextVerificationFunction;
        this.fileNameOrAssetVerificationFunction = fileNameOrAssetVerificationFunction;
        this.assetExistsFunction = assetExistsFunction;
    }
}