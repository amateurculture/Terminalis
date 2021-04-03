/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

//#define DEBUG_MESSAGES

#if !UNITY_WEBPLAYER
#define USE_CLOSE
#endif

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EasyVoiceQuerierWinOS : EasyVoiceQuerier
{
    public override string Name { get { return "Windows OS built-in text-to-speech"; } }

    public override string FileExtension { get { return ".wav"; } }

    public override bool VerifyApp()
    {
        if (!File.Exists(FileName())) // our win console app must be found
            return false;

        return true;
    }

    public override void QueryForVoiceList(EasyVoiceSettings settings)
    {
        settings.voiceNames = null;
        settings.voiceDescriptions = null;
        settings.voiceGenders = null;
        settings.voiceAges = null;
        
        string fileName = FileName();

        if (!File.Exists(fileName))
            return;

        Process voiceListRequest = new Process();

        //ProcessStartInfo startInfo = new ProcessStartInfo();
        //startInfo.FileName = fileName;
        //startInfo.Arguments = "voiceList";
        //startInfo.CreateNoWindow = true;
        //startInfo.RedirectStandardOutput = true; // we need to capture it
        //startInfo.RedirectStandardError = true; // we need to capture it
        //startInfo.UseShellExecute = false; // must be false for redirect output

        //voiceListRequest.StartInfo = startInfo;


        voiceListRequest.StartInfo.FileName = fileName;
        voiceListRequest.StartInfo.Arguments = "voiceList";
        voiceListRequest.StartInfo.CreateNoWindow = true;
        voiceListRequest.StartInfo.RedirectStandardOutput = true; // we need to capture it
        voiceListRequest.StartInfo.RedirectStandardError = true; // we need to capture it
        voiceListRequest.StartInfo.UseShellExecute = false; // must be false for redirect output



#if DEBUG_MESSAGES
        Debug.Log("Starting query...");
#endif

        voiceListRequest.Start();

        voiceListRequest.WaitForExit();

        if (voiceListRequest.ExitCode == 0)
        {
            using (StreamReader streamReader = voiceListRequest.StandardOutput)
            {
                string reply = streamReader.ReadLine();
                if (reply == "VOICE LIST")
                {
                    int count = int.Parse(streamReader.ReadLine());
                    settings.voiceNames = new List<string>(count);
                    settings.voiceDescriptions = new List<string>(count);
                    settings.voiceGenders = new List<string>(count);
                    settings.voiceAges = new List<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        settings.voiceNames.Add(streamReader.ReadLine());
                        settings.voiceDescriptions.Add(streamReader.ReadLine());
                        settings.voiceGenders.Add(streamReader.ReadLine());
                        settings.voiceAges.Add(streamReader.ReadLine());
#if DEBUG_MESSAGES
                        Debug.Log(settings.voiceNames[settings.voiceNames.Count - 1] + ", " +
                            settings.voiceGenders[settings.voiceNames.Count - 1] + ", " +
                            settings.voiceAges[settings.voiceNames.Count - 1] + " -- " +
                            settings.voiceDescriptions[settings.voiceNames.Count - 1]);
#endif
                    }
                }
                else if (reply == "ERROR")
                {
                    Debug.LogError("EasyVoice Windows console returned an error for " + streamReader.ReadLine() + streamReader.ReadLine());
                }
                else
                {
                    Debug.LogError("Unexpected process output: " + reply + "\r\n" + streamReader.ReadToEnd());
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
        string fileName = FileName();

        if (!File.Exists(fileName))
            return;

        Process makeFileRequest = new Process();


        //ProcessStartInfo startInfo = new ProcessStartInfo();

        //startInfo.FileName = fileName;
        //startInfo.Arguments = 
        //    "makeFile" +
        //    " \"" + speechText.Replace("\"", "\"\"") + "\"" +
        //    " \"" + speakerName.Replace("\"", "\"\"") + "\"" +
        //    " \"" + fullFileName.Replace("\"", "\"\"") + "\"";
        //startInfo.CreateNoWindow = true;
        //startInfo.RedirectStandardOutput = true; // we need to capture it
        //startInfo.RedirectStandardError = true; // we need to capture it
        //startInfo.UseShellExecute = false; // must be false for redirect output

        //makeFileRequest.StartInfo = startInfo;


        makeFileRequest.StartInfo.FileName = fileName;
        makeFileRequest.StartInfo.Arguments = 
            "makeFile" +
            " \"" + speechText.Replace("\"", "\"\"") + "\"" +
            " \"" + speakerName.Replace("\"", "\"\"") + "\"" +
            " \"" + fullFileName.Replace("\"", "\"\"") + "\"";
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
                string reply = streamReader.ReadLine();
                if (reply == "FILE MAKE")
                {
                    if (streamReader.ReadLine() == "OKAY")
                    {
#if DEBUG_MESSAGES
                        Debug.Log("Process said okay");
                        Debug.Log("File name should be: " + streamReader.ReadLine());
#endif
                    }
                }
                else
                {
                    Debug.LogError("Unexpected process output: " + reply + "\r\n" + streamReader.ReadToEnd());
                }
                //string result = streamReader.ReadToEnd();
                //Debug.Log(result);
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

    public static string FileName()
    {
        return Application.dataPath + @"/Easy Voice/Apps/EasyVoiceWinConsole.exe";
    }
}
