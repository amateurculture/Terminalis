/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

//#define DEBUG_MESSAGES

#if !UNITY_WEBPLAYER
#define USE_CLOSE
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions; // seems fine with mono
using Debug = UnityEngine.Debug;

public class EasyVoiceQuerierMacOS : EasyVoiceQuerier
{
    public override string Name { get { return "Mac OS X built-in text-to-speech"; } }

    public override string FileExtension { get { return ".aiff"; } }

    public override bool VerifyApp()
    {
        return true; // all should be good
    }

    public override void QueryForVoiceList(EasyVoiceSettings settings)
    {
        settings.voiceNames = null;
        settings.voiceDescriptions = null;
        settings.voiceGenders = null;
        settings.voiceAges = null;

        Process voiceListRequest = new Process();


        //ProcessStartInfo startInfo = new ProcessStartInfo();

        ////startInfo.FileName = Application.dataPath + @"/../EasyVoiceApps/EasyVoiceMacTerminalSpoof/bin/Debug/EasyVoiceMacTerminalSpoof.exe";
        ////startInfo.Arguments = "-v '?' -break";
        //startInfo.FileName = @"/usr/bin/say";
        //startInfo.Arguments = "-v '?'";
        //startInfo.CreateNoWindow = true;
        //startInfo.RedirectStandardOutput = true; // we need to capture it
        //startInfo.RedirectStandardError = true; // we need to capture it
        //startInfo.UseShellExecute = false; // must be false for redirect output

        //voiceListRequest.StartInfo = startInfo;


        //voiceListRequest.StartInfo.FileName = Application.dataPath + @"/../EasyVoiceApps/EasyVoiceMacTerminalSpoof/bin/Debug/EasyVoiceMacTerminalSpoof.exe";
        //voiceListRequest.StartInfo.Arguments = "-v '?' -break";
        voiceListRequest.StartInfo.FileName = @"/usr/bin/say";
        voiceListRequest.StartInfo.Arguments = "-v '?'";
        voiceListRequest.StartInfo.CreateNoWindow = true;
        voiceListRequest.StartInfo.RedirectStandardOutput = true; // we need to capture it
        voiceListRequest.StartInfo.RedirectStandardError = true; // we need to capture it
        voiceListRequest.StartInfo.UseShellExecute = false; // must be false for redirect output


#if DEBUG_MESSAGES
        Debug.Log("Starting query...");
#endif
        try
        {
            voiceListRequest.Start();

            voiceListRequest.WaitForExit();
        }
        catch (Exception exception)
        {
#if USE_CLOSE
            voiceListRequest.Close();
#endif
            Debug.LogError("Process start/run error: " + exception);
            return;
        }

        if (voiceListRequest.ExitCode == 0)
        {
            using (StreamReader streamReader = voiceListRequest.StandardOutput)
            {
                settings.voiceNames = new List<string>();
                settings.voiceDescriptions = new List<string>();
                settings.voiceGenders = new List<string>();
                settings.voiceAges = new List<string>();

                while (!streamReader.EndOfStream)
                {
                    string reply = streamReader.ReadLine();

                    // Skip empty strings
                    if (string.IsNullOrEmpty(reply))
                        continue;

                    Match match = Regex.Match(reply, @"^([^#]+?)\s*([^ ]+)\s*# (.*?)$");

                    if (match.Success)
                    {
                        settings.voiceNames.Add(match.Groups[1].ToString());
                        settings.voiceDescriptions.Add(match.Groups[3].ToString());
                        settings.voiceGenders.Add("");
                        settings.voiceAges.Add("");
#if DEBUG_MESSAGES
                        Debug.Log(settings.voiceNames[settings.voiceNames.Count - 1] + " -- " +
                                  settings.voiceDescriptions[settings.voiceNames.Count - 1]);
#endif
                    }
                    else
                    {
#if DEBUG_MESSAGES // Apparently custom voices can insert extra lines into the terminal output, so we cannot print this in production
                        Debug.LogError("Process returned unexpected line: " + reply);
#endif
                    }
                }
                //string result = streamReader.ReadToEnd();
                //Debug.Log(result);
            }
        }
        else
        {
            using (StreamReader streamReader = voiceListRequest.StandardError)
            {
                string result = streamReader.ReadToEnd();

                // If "say" reports there is no '?' voice, then it doesn't support -v '?' syntax and we need to do the ls speech folder
                if (result.StartsWith("Voice `?' not found"))
                {
#if USE_CLOSE
                    voiceListRequest.Close();
#endif
                    QueryForVoiceListLs(settings);
                    return;
                }

                Debug.LogError("Process error: " + result);
            }
        }

#if USE_CLOSE
        voiceListRequest.Close();
#endif
    }

    private void QueryForVoiceListLs(EasyVoiceSettings settings)
    {
        Process voiceListRequest = new Process();

        
        //ProcessStartInfo startInfo = new ProcessStartInfo();

        ////startInfo.FileName = Application.dataPath + @"/../EasyVoiceApps/EasyVoiceMacTerminalSpoof/bin/Debug/EasyVoiceMacTerminalSpoof.exe";
        ////startInfo.Arguments = "-ls";
        //startInfo.FileName = @"ls";
        //startInfo.Arguments = "/System/Library/Speech/Voices";
        //startInfo.CreateNoWindow = true;
        //startInfo.RedirectStandardOutput = true; // we need to capture it
        //startInfo.RedirectStandardError = true; // we need to capture it
        //startInfo.UseShellExecute = false; // must be false for redirect output

        //voiceListRequest.StartInfo = startInfo;


        //voiceListRequest.StartInfo.FileName = Application.dataPath + @"/../EasyVoiceApps/EasyVoiceMacTerminalSpoof/bin/Debug/EasyVoiceMacTerminalSpoof.exe";
        //voiceListRequest.StartInfo.Arguments = "-ls";
        voiceListRequest.StartInfo.FileName = @"ls";
        voiceListRequest.StartInfo.Arguments = "/System/Library/Speech/Voices";
        voiceListRequest.StartInfo.CreateNoWindow = true;
        voiceListRequest.StartInfo.RedirectStandardOutput = true; // we need to capture it
        voiceListRequest.StartInfo.RedirectStandardError = true; // we need to capture it
        voiceListRequest.StartInfo.UseShellExecute = false; // must be false for redirect output

#if DEBUG_MESSAGES
        Debug.Log("Starting query...");
#endif
        try
        {
            voiceListRequest.Start();

            voiceListRequest.WaitForExit();
        }
        catch (Exception exception)
        {
#if USE_CLOSE
            voiceListRequest.Close();
#endif
            Debug.LogError("Process start/run error: " + exception);
            return;
        }

        if (voiceListRequest.ExitCode == 0)
        {
            using (StreamReader streamReader = voiceListRequest.StandardOutput)
            {
                settings.voiceNames = new List<string>();
                settings.voiceDescriptions = new List<string>();
                settings.voiceGenders = new List<string>();
                settings.voiceAges = new List<string>();

                while (!streamReader.EndOfStream)
                {
                    string reply = streamReader.ReadLine();

                    MatchCollection matches = Regex.Matches(reply, @"([^\s]+?)\.SpeechVoice");

                    foreach (Match match in matches)
                    {
                        settings.voiceNames.Add(match.Groups[1].ToString());
                        settings.voiceDescriptions.Add("");
                        settings.voiceGenders.Add("");
                        settings.voiceAges.Add("");
#if DEBUG_MESSAGES
                        Debug.Log(settings.voiceNames[settings.voiceNames.Count - 1] + " -- " +
                                  settings.voiceDescriptions[settings.voiceNames.Count - 1]);
#endif
                    }
                }

                if (settings.voiceNames.Count == 0)
                {
                    Debug.LogError("Process couldn't parse any speech names");
                    return;
                }
                //string result = streamReader.ReadToEnd();
                //Debug.Log(result);
            }
        }
        else
        {
            using (StreamReader streamReader = voiceListRequest.StandardError)
            {
                string result = streamReader.ReadToEnd();
                Debug.LogError("Process error: " + result);
            }
        }

#if USE_CLOSE
        voiceListRequest.Close();
#endif
    }

    public override void AskToMakeFile(string speechText, string speakerName, string fullFileName, string fileFormat)
    {
        Process makeFileRequest = new Process();


        // Unity Webplayer Mono doesn't support this:
        // Process.StartInfo is read-only

        //ProcessStartInfo startInfo = new ProcessStartInfo();

        ////if (Application.platform == RuntimePlatform.WindowsPlayer)
        ////    startInfo.FileName = Application.dataPath + @"/Easy Voice/Apps/EasyVoiceMacTerminalSpoof.exe";
        ////else
        //    startInfo.FileName = @"/usr/bin/say";
        //startInfo.Arguments =
        //    (speakerName != "" ? " -v \"" + speakerName.Replace("\"", "\"\"") + "\"" : "") +
        //    " -o \"" + fullFileName.Replace("\"", "\"\"") + "\"" +
        //    (fileFormat != "" ? " --file-format=" + fileFormat : "") + // AIFF explicit, not via .aiff, only one that works
        //    " \"" + speechText.Replace("\"", "\"\"") + "\"";
        //startInfo.CreateNoWindow = true;
        //startInfo.RedirectStandardOutput = true; // we need to capture it
        //startInfo.RedirectStandardError = true; // we need to capture it
        //startInfo.UseShellExecute = false; // must be false for redirect output

        //makeFileRequest.StartInfo = startInfo;


        //if (Application.platform == RuntimePlatform.WindowsPlayer)
        //    startInfo.FileName = Application.dataPath + @"/Easy Voice/Apps/EasyVoiceMacTerminalSpoof.exe";
        //else
            makeFileRequest.StartInfo.FileName = @"/usr/bin/say";
        makeFileRequest.StartInfo.Arguments =
            (speakerName != "" ? " -v \"" + speakerName.Replace("\"", "\"\"") + "\"" : "") +
            " -o \"" + fullFileName.Replace("\"", "\"\"") + "\"" +
            (fileFormat != "" ? " --file-format=" + fileFormat : "") + // AIFF explicit, not via .aiff, only one that works
            " \"" + speechText.Replace("\"", "\"\"") + "\"";
        makeFileRequest.StartInfo.CreateNoWindow = true;
        makeFileRequest.StartInfo.RedirectStandardOutput = true; // we need to capture it
        makeFileRequest.StartInfo.RedirectStandardError = true; // we need to capture it
        makeFileRequest.StartInfo.UseShellExecute = false; // must be false for redirect output

#if DEBUG_MESSAGES
        Debug.Log("Starting query...");
#endif

        makeFileRequest.Start();

        makeFileRequest.WaitForExit();

        if (makeFileRequest.ExitCode == 0)
        {
            using (StreamReader streamReader = makeFileRequest.StandardOutput)
            {
            }
        }
        else
        {
            using (StreamReader streamReader = makeFileRequest.StandardError)
            {
                string result = streamReader.ReadToEnd();
                Debug.LogError("Process error: " + result);
            }
        }

#if USE_CLOSE
        makeFileRequest.Close();
#endif
    }
}
