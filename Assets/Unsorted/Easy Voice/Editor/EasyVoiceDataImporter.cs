/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class EasyVoiceDataImporter
{
    private const string csvHeader = "\"ID\",\"Group\",\"Status\",\"Speaker\",\"Text\",\"File name\"";

    public static void ExportData(string fileName)
    {
        try
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter streamWriter = new StreamWriter(fileStream, EasyVoiceSettings.instance.exportCSVFileEncodingUTF8 ? Encoding.UTF8 : Encoding.ASCII);

            streamWriter.WriteLine(csvHeader);

            EasyVoiceDataAsset data = EasyVoiceSettings.instance.data; // readability

            for (int i = 0; i < EasyVoiceSettings.instance.data.LineCount(); i++)
            {
                string speechText = data.GetSpeechText(i); // cache, using thrice
                streamWriter.WriteLine(
                    "" + data.GetId(i) + "," +
                    "\"" + data.GetGroup(i).Replace("\"", "\"\"") + "\"," +
                    "" + data.GetStatus(i) + "," +
                    "\"" + data.GetSpeakerName(i).Replace("\"", "\"\"") + "\"," +
                    (NeedCSVEscaping(speechText) ? "\"" + speechText.Replace("\"", "\"\"") + "\"" : speechText.Replace("\"", "\"\"")) + "," +
                    "\"" + data.GetFileName(i).Replace("\"", "\"\"") + "\""
                );
            }

            streamWriter.Close();
            fileStream.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("EasyVoice.ExportData encountered an error: " + e);
        }
    }

    private static bool NeedCSVEscaping(string text)
    {
        return text.IndexOf(' ') != -1 || text.IndexOf(',') != -1 || text.IndexOf('"') != -1;
    }

    private static int lastImportStartingLineCount; // We remember how many lines there were before importing, so we only link the matching clips of the NEW appended lines, not old ones that could possibly have matches

    public enum ImportResult { fail, okay, matchingClipsFound }

    public static ImportResult ImportData(string fileName, EasyVoiceSettings settings, bool append)
    {
        try
        {
            if (!File.Exists(fileName))
            {
                Debug.LogWarning("EasyVoice.ImportData tried to open the file returned by Unity's dialog, but such file doesn't appear to exist: \"" + fileName + "\"!");
                return ImportResult.fail;
            }

            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream, settings.exportCSVFileEncodingUTF8 ? Encoding.Unicode : Encoding.ASCII);

            string header = streamReader.ReadLine();
            List<string> headerSplit = SplitLine(header);
            if (headerSplit == null || headerSplit.Count != 6 || headerSplit[0] != "ID" || headerSplit[1] != "Group" || headerSplit[2] != "Status" || headerSplit[3] != "Speaker" || headerSplit[4] != "Text" || headerSplit[5] != "File name")
            {
                Debug.LogWarning("EasyVoice.ImportData opened the CSV the file, but the header syntax did not match the expected format!");
                return ImportResult.fail;
            }

            List<int> tempIds = new List<int>();
            List<string> tempGroups = new List<string>();
            List<byte> tempStatuses = new List<byte>();
            List<string> tempSpeakerNames = new List<string>();
            List<string> tempSpeechTexts = new List<string>();
            List<string> tempFileNames = new List<string>();
            List<LineIssue> tempIssues = new List<LineIssue>();

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                List<string> splitLine = SplitLine(line);
                //Debug.Log(splitLine);
                if (splitLine != null && splitLine.Count == 6)
                {
                    int id;
                    if (!int.TryParse(splitLine[0], out id))
                    {
                        Debug.LogWarning("EasyVoice.ImportData opened the CSV the file, but an encountered id field syntax did not match the expected format!");
                        return ImportResult.fail;
                    }
                    tempIds.Add(id);
                    tempGroups.Add(splitLine[3]);
                    byte status;
                    if (!byte.TryParse(splitLine[2], out status))
                    {
                        Debug.LogWarning("EasyVoice.ImportData opened the CSV the file, but an encountered status field syntax did not match the expected format!");
                        return ImportResult.fail;
                    }
                    tempStatuses.Add(status);
                    tempSpeakerNames.Add(splitLine[3]);
                    tempSpeechTexts.Add(splitLine[4]);
                    tempFileNames.Add(splitLine[5]);
                    tempIssues.Add(0);
                }
                else
                {
                    Debug.LogWarning("EasyVoice.ImportData opened the CSV the file, but an encountered line syntax did not match the expected format!");
                    return ImportResult.fail;
                }
            }

            streamReader.Close();
            fileStream.Close();

            if (!append)
                settings.data.DeleteAllLines();

            lastImportStartingLineCount = settings.data.LineCount();

            bool defaultFolderValid = settings.IsDefaultFolderValid(); // so we don't check this too many times
            bool clipFound = false;

            for (int i = 0; i < tempSpeakerNames.Count; i++)
            {
                settings.data.AddNewLine(tempIds[i], tempGroups[i], tempStatuses[i], tempSpeakerNames[i], tempSpeechTexts[i], tempFileNames[i], tempIssues[i]);

                // If we need to find a clip (haven't found one yet) and also settings are valid to do this check
                if (!clipFound && defaultFolderValid)
                {
                    string assetFileName, fullFileName;
                    EasyVoiceClipCreator.GenerateFullFileName(i, out assetFileName, out fullFileName);
                    AudioClip foundClip = (AudioClip)AssetDatabase.LoadAssetAtPath(assetFileName, typeof(AudioClip));
                    if (foundClip != null)
                        clipFound = true;
                }
            }

            EasyVoiceIssueChecker.CheckAllLineIssues(); // this will get called again after linking clips, but we don't know if user will decide to do this yet

            return clipFound ? ImportResult.matchingClipsFound : ImportResult.okay;
        }
        catch (Exception e)
        {
            Debug.LogError("EasyVoice.ImportData encountered an error: " + e);
            return ImportResult.fail;
        }
    }

    public static void LinkClipsAfterImport()
    {
        for (int i = lastImportStartingLineCount; i < EasyVoiceSettings.instance.data.LineCount(); i++)
        {
            string assetFileName, fullFileName;
            EasyVoiceClipCreator.GenerateFullFileName(i, out assetFileName, out fullFileName);
            AudioClip foundClip = (AudioClip)AssetDatabase.LoadAssetAtPath(assetFileName, typeof(AudioClip));
            if (foundClip != null)
                EasyVoiceSettings.instance.data.SetClip(i, foundClip); // don't mark changed though
        }

        EasyVoiceIssueChecker.CheckAllLineIssues();
    }

    private static List<string> SplitLine(string line)
    {
        if (line.Length == 0) // "," is valid :)
            return null;

        List<string> splits = new List<string>();
        int startIndex = 0;

        int currentState = 1;
        // 0 - waiting for ,
        // 1 - waiting for start '"' or other symbol
        // 2 - reading escaped value, waiting for end '"'
        // 3 - reading plain value, waiting for end ','

        //Debug.Log("Splitting line " + line);

        for (int i = 0; i < line.Length; i++)
        {
            //Debug.Log("Reading symbol #" + i + " " + line[i] + ", state = " + currentState + ", starting index = " + startIndex);

            switch (currentState)
            {
                case 0:
                    if (line[i] == ',')
                    {
                        currentState = 1;
                    }
                    else if (line[i] == ' ')
                        break; // I guess the weird case where after end " there is space before next , even though there can't be any after ,
                    else
                        return null;
                    break;

                case 1:
                    if (line[i] == '"')
                    {
                        // Next value is enclosed in quotes
                        startIndex = i + 1;
                        currentState = 2;
                    }
                    else if (line[i] == ',') // comma followed by comma basically ...,,....
                    {
                        splits.Add("");
                        // stay in current state
                    }
                    else
                    {
                        // Next value is not enclosed in quotes
                        startIndex = i;
                        currentState = 3;
                    }
                    break;

                case 2:
                    if (line[i] == '"')
                    {
                        if (i < line.Length - 1 && line[i + 1] == '"')
                        {
                            // Ignore the second double-quote as an escape character and skip next character
                            i++;
                        }
                        else
                        {
                            splits.Add(line.Substring(startIndex, i - startIndex).Replace("\"\"", "\""));
                            currentState = 0;
                        }
                    }
                    break;

                case 3:
                    if (line[i] == ',')
                    {
                        splits.Add(line.Substring(startIndex, i - startIndex).Replace("\"\"", "\""));
                        currentState = 1;
                    }
                    else if (i < line.Length - 1 && line[i + 1] == '"') // although this shouldn't really happen, the entire value would be in quotes (case 2)
                    {
                        // Ignore the second double-quote as an escape character and skip next character
                        i++;
                    }
                    break;
            }

        }

        if (currentState == 3)
        {
            // line ended while reading an unescaped line, so save it
            splits.Add(line.Substring(startIndex, line.Length - startIndex).Replace("\"\"", "\""));
        }
        else if (currentState != 0) // anything else besides expecting another field is automatic syntax fail (unfinished line)
            return null;

        return splits;
    }
}
