/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class EasyVoiceClipCreator
{
    /// <summary>
    /// The assets that we created during last asset generation.
    /// The EasyVoiceAudioClipImporter will need this to know which files were created by EV, so custom import settings can be applied as needed.
    /// </summary>
    public static List<string> lastCreatedAssetFileNames = new List<string>();

    public enum CreateFilesCheckResult { ok, badDefaultFolder, missingDefaultFolder, querierFailedToVerifyApp }

    public static CreateFilesCheckResult CreateFilesPreCheck(EasyVoiceSettings settings)
    {
        if (!settings.IsDefaultFolderValid())
            return CreateFilesCheckResult.badDefaultFolder;

        string fullPath = settings.GetFullClipDefaultPath();
        if (!Directory.Exists(fullPath))
            return CreateFilesCheckResult.missingDefaultFolder;

        if (!settings.querier.VerifyApp())
            return CreateFilesCheckResult.querierFailedToVerifyApp;

        return CreateFilesCheckResult.ok; // no apparent problems
    }

    public static bool CreateFiles(EasyVoiceSettings settings)
    {
        if (CreateFilesPreCheck(settings) != CreateFilesCheckResult.ok) // this duplicates Editor Window UI, btu is quick enough
            return false;

        List<int> created = new List<int>();
        lastCreatedAssetFileNames.Clear(); // if this is not empty, something wen't wrong? well, we can't fix it anyway without knowing details, and if we know details, we probably already either addressed them or notified the user

        //List<List<LineIssue>> issues = EasyVoiceIssueChecker.GetLineIssues(settings);

        List<int> lineIndices = new List<int>();

        for (int i = 0; i < settings.data.LineCount(); i++)
        {
            if (!settings.data.GetOutputStatus(i))
                continue;

            EasyVoiceIssueChecker.CheckLineIssues(i);

            if (IssuePreventsFileMaking(settings.data.GetIssues(i))) // most overwriting is "okay"
                continue;

            lineIndices.Add(i);
        }

        for (int i = 0; i < lineIndices.Count; i++)
        {
            int lineIndex = lineIndices[i];

            string assetFileName; // this is Unity internal asset path starting with "Assets\" and including extension
            string fullFileName; // this is the file name, including the system path and extension
            GenerateFullFileName(lineIndex, out assetFileName, out fullFileName);

#if DEBUG_MESSAGES
            Debug.Log(assetFileName);
#endif

            settings.querier.AskToMakeFile(
                settings.data.GetSpeechText(lineIndex), 
                settings.data.GetSpeakerNameOrSelectDefault(lineIndex), 
                fullFileName, 
                settings.osxFileFormat);

            // Check if the file has the wrong extension
            if (Path.GetExtension(fullFileName) != settings.querier.FileExtension)
            {
#if DEBUG_MESSAGES
                Debug.Log("Audio clip file has a different extension than expected (\"" + settings.querier.FileExtension + "\")");
#endif

                int extensionIndex = fullFileName.LastIndexOf('.');
                if (extensionIndex != 0)
                {
                    string newName = fullFileName.Substring(0, extensionIndex) + settings.querier.FileExtension;

                    File.Move(fullFileName, newName);

                    extensionIndex = assetFileName.LastIndexOf('.');
                    if (extensionIndex != 0) // just in case asset file name is messed up
                        assetFileName = assetFileName.Substring(0, extensionIndex) + settings.querier.FileExtension;

#if DEBUG_MESSAGES
                    Debug.Log("Renamed audio clip asset to \"" + newName + "\" and internal asset path to \"" + assetFileName + "\"");
#endif
                }
            }

            created.Add(lineIndex);
            lastCreatedAssetFileNames.Add(assetFileName);

            settings.data.SetOutputStatus(lineIndex, false);

            EditorUtility.DisplayProgressBar(EasyVoiceSettings.progressDialogTitle, EasyVoiceSettings.progressDialogText, (i + 1f) / (lineIndices.Count + 1));
        }

#if UNITY_EDITOR
        //AssetDatabase.Refresh(); -- using AssetDatabase.ImportAsset instead
#endif

#if DEBUG_MESSAGES
        foreach (string lastCreatedAssetFileName in lastCreatedAssetFileNames)
            Debug.Log("Asset will be expected for linking: " + lastCreatedAssetFileName);            
#endif

        if (settings.linkClips)
        {
            for (int i = 0; i < created.Count; i++)
            {
                // Force asset to import
                AssetDatabase.ImportAsset(lastCreatedAssetFileNames[i], ImportAssetOptions.ForceSynchronousImport);

                // Get the new asset
                AudioClip foundClip = (AudioClip)AssetDatabase.LoadAssetAtPath(lastCreatedAssetFileNames[i], typeof(AudioClip));

                //if (foundClip != null)
                //{
                //    System.Threading.Thread.Sleep(500); // wait a little bit of OS to possibly refresh, then retry
                //    AssetDatabase.ImportAsset(lastCreatedAssetFileNames[i], ImportAssetOptions.ForceSynchronousImport);
                //    foundClip = (AudioClip)AssetDatabase.LoadAssetAtPath(lastCreatedAssetFileNames[i], typeof(AudioClip));
                //}

                if (foundClip != null)
                {
#if DEBUG_MESSAGES
                    Debug.Log("Audio clip to link was found at: " + lastCreatedAssetFileNames[i]);
#endif

                    //                    if (!lastCreatedAssetFileNames[i].EndsWith(settings.querier.FileExtension))
                    //                    {
                    //#if DEBUG_MESSAGES
                    //                        Debug.Log("Audio clip has a different extension than expected (\"" + settings.querier.FileExtension + "\")");
                    //#endif
                    //                        int extensionIndex = lastCreatedAssetFileNames[i].LastIndexOf('.');
                    //                        if (extensionIndex > 0 && extensionIndex > lastCreatedAssetFileNames[i].Length - 10) // some sanity
                    //                        {
                    //                            int slashIndex = lastCreatedAssetFileNames[i].LastIndexOf('/');
                    //                            if (slashIndex != -1 && slashIndex < extensionIndex) // some more sanity
                    //                            {
                    //                                string newName = lastCreatedAssetFileNames[i].Substring(slashIndex + 1, extensionIndex - slashIndex - 1) + settings.querier.FileExtension;
                    //#if DEBUG_MESSAGES
                    //                                Debug.Log("Renaming audio clip asset to \"" + newName + "\"");
                    //#endif
                    //                                Debug.Log(AssetDatabase.RenameAsset(lastCreatedAssetFileNames[i], newName)); -- CAN'T, extension isn't renamed
                    //                            }
                    //                        }
                    //                    }

                    settings.data.SetClip(created[i], foundClip, false); // TODO: VERIFY?
                }
                else
                {
                    Debug.LogWarning("EasyVoice expected an AudioClip asset to be generated at \"" + lastCreatedAssetFileNames[i] + "\", but it wasn't found!");
                }
            }
        }
#if DEBUG_MESSAGES
        else
        {
            Debug.Log("Settings not set to link created clips, skipping.");
        }
#endif

        lastCreatedAssetFileNames.Clear();

        return true;
    }

    public static bool IssuePreventsFileMaking(LineIssue issues)
    {
        return 
            (issues & LineIssue.emptyLine) == LineIssue.emptyLine ||
            (issues & LineIssue.badFileName) == LineIssue.badFileName ||
            (issues & LineIssue.invalidSpeaker) == LineIssue.invalidSpeaker;
    }
    
    /// <summary>
    /// This will generate the Unity asset path and full system file path for the given line/clip/filename
    /// </summary>
    public static void GenerateFullFileName(int lineIndex, out string assetFileName, out string fullFileName)
    {
        if (EasyVoiceSettings.instance.data.GetClip(lineIndex) == null)
        {
            assetFileName = "Assets" + EasyVoiceSettings.instance.defaultFolder + EasyVoiceSettings.instance.data.GetFileNameOrDefault(lineIndex) + (EasyVoiceSettings.instance.querier != null ? EasyVoiceSettings.instance.querier.FileExtension : ""); // todo win/mac guess?
            fullFileName = Application.dataPath + assetFileName.Substring(6); // dataPath includes "Assets"
        }
        else
        {
            assetFileName = AssetDatabase.GetAssetPath(EasyVoiceSettings.instance.data.GetClip(lineIndex)); // will include "Assets/" and ".*" extension
            if (assetFileName.StartsWith("Assets"))
            {
                fullFileName = Application.dataPath + assetFileName.Substring(6); // remove "Assets" already in data path, keeping "/"
            }
            else
            {
                fullFileName = Application.dataPath + assetFileName;
                //Debug.LogWarning("Unity returned AssetDatabase.GetAssetPath() not starting with 'Assets\'! This may have unexpected results for \"" + assetFileName + "\"");
            }

        }
        //Debug.Log("fullFileName = \"" + fullFileName + "\", assetFileName = \"" + assetFileName + "\""); -- uber-spammy
    }
}
