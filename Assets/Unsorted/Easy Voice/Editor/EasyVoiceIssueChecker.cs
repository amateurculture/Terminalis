/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EasyVoiceIssueChecker
{
    public static void VerifySpeakerName(int index)
    {
        EasyVoiceSettings.instance.data.SetIssue(
            index,
            LineIssue.invalidSpeaker,
            !EasyVoiceSettings.instance.ValidVoiceName(EasyVoiceSettings.instance.data.GetSpeakerName(index), true)
        );
    }

    public static void VerifySpeechText(int index)
    {
        EasyVoiceSettings.instance.data.SetIssue(
            index, 
            LineIssue.emptyLine, 
            EasyVoiceSettings.instance.data.GetSpeechText(index) == ""
        );
    }

    public static void VerifyFileNameOrClip(int index)
    {
        VerifyFileNameOrClip(index, true);
    }

    public static int verifyCount;

    private static void VerifyFileNameOrClip(int index, bool allowFurtherCalls)
    {
        verifyCount++;

        EasyVoiceDataAsset data = EasyVoiceSettings.instance.data; // readability

        data.SetIssue(index, LineIssue.badFileName, !ValidFileName(data.GetFileName(index)));
        
        // CLear all issues that we can possibly set in the loops below
        data.SetIssue(index, LineIssue.duplicateBaseFileName, false);
        data.SetIssue(index, LineIssue.duplicateAssetFileName, false);
        data.SetIssue(index, LineIssue.clashingExistingAsset, false);
        data.SetIssue(index, LineIssue.duplicateClipReference, false);

        //Debug.Log("Clearing line " + index + " file name or clip issues");

        AudioClip ourClip = data.GetClip(index);

        string ourAssetFileName, ourFullFileName;
        EasyVoiceClipCreator.GenerateFullFileName(index, out ourAssetFileName, out ourFullFileName);

        if (ourClip == null)
        {
            string ourFileName = data.GetFileNameOrDefault(index);

            for (int otherIndex = 0; otherIndex < data.LineCount(); otherIndex++)
            {
                if (otherIndex == index) 
                    continue;

                if (data.GetClip(otherIndex) == null)
                {
                    // Check for duplicate file names -- another line has the same file name as us
                    if (ourFileName == data.GetFileNameOrDefault(otherIndex))
                    {
                        data.SetIssue(index, LineIssue.duplicateBaseFileName, true);
                        if (allowFurtherCalls)
                        {
                            // Recheck the other file as well now, because we probably just clashed it
                            VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                    else
                    {
                        // Recheck existing lines with duplicate issue, in case we were the one causing it
                        if (allowFurtherCalls)
                        {
                            if (data.HasIssue(otherIndex, LineIssue.duplicateBaseFileName))
                                VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                }
                else
                {
                    string otherAssetFileName, otherFullFileName;
                    EasyVoiceClipCreator.GenerateFullFileName(otherIndex, out otherAssetFileName, out otherFullFileName); // TODO: cache?

                    // Check for clashing clip names -- another line has a clip name+path the same as our potential file path
                    if (ourAssetFileName == otherAssetFileName)
                    {
                        data.SetIssue(index, LineIssue.duplicateAssetFileName, true);
                        if (allowFurtherCalls)
                        {
                            // Recheck the other file as well now, because we probably just clashed it
                            VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                    else
                    {
                        // Recheck existing lines with clashing file name issue, in case we were the one causing it
                        if (allowFurtherCalls)
                        {
                            if (data.HasIssue(otherIndex, LineIssue.duplicateAssetFileName))
                                VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                }
            }

            if (!data.HasIssue(index, LineIssue.duplicateAssetFileName)) // below issue is implied if already clashing another line with that asset, performance
            {
                // Check for clashing existing assets -- an asset already exists at the path that "our" asset will potentially be created at
                Object foundAsset = (Object)AssetDatabase.LoadAssetAtPath(ourAssetFileName, typeof(Object));
                if (foundAsset != null)
                {
                    data.SetIssue(index, LineIssue.clashingExistingAsset, true);
                }
            }
        }
        else
        {
            for (int otherIndex = 0; otherIndex < data.LineCount(); otherIndex++)
            {
                if (otherIndex == index)
                    continue;

                if (data.GetClip(otherIndex) != null)
                {
                    // Check for duplicate clip references -- another line has the same clip as we do
                    if (ourClip == data.GetClip(otherIndex))
                    {
                        data.SetIssue(index, LineIssue.duplicateClipReference, true);
                        if (allowFurtherCalls)
                        {
                            // Recheck the other file as well now, because we probably just clashed it
                            VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                    else
                    {
                        // Recheck existing lines with duplicate clip issue, in case we were the one causing it
                        if (allowFurtherCalls)
                        {
                            if (data.HasIssue(otherIndex, LineIssue.duplicateClipReference))
                                VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                }
                else
                {
                    // Check for clashing clip names -- another line has a the same potential file path as our clip name+path

                    string otherAssetFileName, otherFullFileName;
                    EasyVoiceClipCreator.GenerateFullFileName(otherIndex, out otherAssetFileName, out otherFullFileName); // TODO: cache?

                    if (ourAssetFileName == otherAssetFileName)
                    {
                        data.SetIssue(index, LineIssue.duplicateAssetFileName, true);
                        if (allowFurtherCalls)
                        {
                            // Recheck the other file as well now, because we probably just clashed it
                            VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                    else
                    {
                        // Recheck existing lines with clashing file name issue, in case we were the one causing it
                        if (allowFurtherCalls)
                        {
                            if (data.HasIssue(otherIndex, LineIssue.duplicateAssetFileName))
                                VerifyFileNameOrClip(otherIndex, false);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// This assumes proper sanitization was carried out by SetFileName()
    /// </summary>
    private static bool ValidFileName(string fileName)
    {
        if (fileName == EasyVoiceSettings.defaultFileNameString)
            return true;

        if (fileName == "")
            return false;

        return true;
    }

    public static void CheckAllLineIssues()
    {
        for (int index = 0; index < EasyVoiceSettings.instance.data.LineCount(); index++)
        {
            CheckLineIssues(index);
        }
    }

    public static void CheckAllFileNamesOrClips()
    {
        for (int index = 0; index < EasyVoiceSettings.instance.data.LineCount(); index++)
        {
            VerifyFileNameOrClip(index);
        }
    }

    public static void CheckLineIssues(int index)
    {
        VerifySpeakerName(index);
        VerifySpeechText(index);
        VerifyFileNameOrClip(index);
    }

    public static bool AssetExists(int index)
    {
        string assetFileName, fullFileName;
        EasyVoiceClipCreator.GenerateFullFileName(index, out assetFileName, out fullFileName);
        Object foundAsset = (Object)AssetDatabase.LoadAssetAtPath(assetFileName, typeof(Object));
        return foundAsset != null;
    }
}
