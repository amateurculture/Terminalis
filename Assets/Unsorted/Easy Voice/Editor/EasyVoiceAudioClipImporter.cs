/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

//#define DEBUG_MESSAGES

using UnityEditor;
using UnityEngine;

public class EasyVoiceAudioClipImporter : AssetPostprocessor
{
    public void OnPreprocessAudio()
    {
        if (EasyVoiceSettings.instance == null)
            return;

        if (EasyVoiceSettings.instance.data == null)
            return;

        if (EasyVoiceClipCreator.lastCreatedAssetFileNames == null)
            return;

        // If user hasn't specified we need to apply custom settings, then don't bother touching the asset
        if (!EasyVoiceSettings.instance.audioImportSettings.applyCustomSettings)
            return;

        string ourAssetPath = assetPath;

        if (!ourAssetPath.StartsWith("Assets/"))
            return;

        //ourAssetPath = ourAssetPath.Substring(6);

#if DEBUG_MESSAGES
        Debug.Log("Importing audio asset at " + ourAssetPath);
#endif

        if (EasyVoiceClipCreator.lastCreatedAssetFileNames.Contains(ourAssetPath))
        {
#if DEBUG_MESSAGES
            Debug.Log("Matched a created asset!");
#endif
            AudioImporter audioImporter = (AudioImporter)assetImporter;

            // Setup generic importer settings

            audioImporter.forceToMono = EasyVoiceSettings.instance.audioImportSettings.forceToMono;

            audioImporter.loadInBackground = EasyVoiceSettings.instance.audioImportSettings.loadInBackground;

            audioImporter.preloadAudioData = EasyVoiceSettings.instance.audioImportSettings.preloadAudioData;

            // Setup new (default) setting for the importer and convert from out stored values

            AudioImporterSampleSettings settings = new AudioImporterSampleSettings();

            settings.loadType = LoadType(EasyVoiceSettings.instance.audioImportSettings.loadType);

            settings.compressionFormat = CompressionFormat(EasyVoiceSettings.instance.audioImportSettings.compressionFormat);

            settings.quality = EasyVoiceSettings.instance.audioImportSettings.quality;

            settings.sampleRateSetting = SampleRate(EasyVoiceSettings.instance.audioImportSettings.sampleRateSetting);

            settings.sampleRateOverride = EasyVoiceSettings.instance.audioImportSettings.overrideSampleRate;

            // Apply the new (default) settings to the importer

            audioImporter.defaultSampleSettings = settings;


            // We used to do this:
            // audioImporter.threeD = EasyVoiceSettings.instance.importClips3D;
            // audioImporter.format = AudioImporterFormatValue(EasyVoiceSettings.instance.importClipsFormat);
            // audioImporter.loadType = AudioImporterLoadTypeValue(EasyVoiceSettings.instance.importClipsLoadType);
            

            //EasyVoiceSettings.instance.data.lastCreatedAssetFileNames.Remove(ourAssetPath); -- CreateFiles() will do this, we are forcing immediate import anyway
        }
#if DEBUG_MESSAGES
        else
        {
            foreach (string lastCreatedAssetFileName in EasyVoiceClipCreator.lastCreatedAssetFileNames)
            {
                Debug.Log("Didn't match " + lastCreatedAssetFileName);
            }
        }
#endif
    }

    private static AudioClipLoadType LoadType(EasyVoiceSettings.AudioImportSettings.LoadType loadType)
    {
        switch (loadType)
        {
            case EasyVoiceSettings.AudioImportSettings.LoadType.DecompressOnLoad:
                return AudioClipLoadType.DecompressOnLoad;
            case EasyVoiceSettings.AudioImportSettings.LoadType.CompressedInMemory:
                return AudioClipLoadType.CompressedInMemory;
            case EasyVoiceSettings.AudioImportSettings.LoadType.Streaming:
                return AudioClipLoadType.Streaming;
            default:
                return AudioClipLoadType.DecompressOnLoad; // Do we have corrupted data?
        }
    }

    //public static EasyVoiceSettings.AudioImportSettings.LoadType LoadType(AudioClipLoadType loadType)
    //{
    //    switch (loadType)
    //    {
    //        case AudioClipLoadType.DecompressOnLoad:
    //            return EasyVoiceSettings.AudioImportSettings.LoadType.DecompressOnLoad;
    //        case AudioClipLoadType.CompressedInMemory:
    //            return EasyVoiceSettings.AudioImportSettings.LoadType.CompressedInMemory;
    //        case AudioClipLoadType.Streaming:
    //            return EasyVoiceSettings.AudioImportSettings.LoadType.Streaming;
    //        default:
    //            return EasyVoiceSettings.AudioImportSettings.LoadType.DecompressOnLoad; // Unity has a new value?
    //    }
    //}

    private static AudioCompressionFormat CompressionFormat(EasyVoiceSettings.AudioImportSettings.CompressionFormat format)
    {
        switch (format)
        {
            case EasyVoiceSettings.AudioImportSettings.CompressionFormat.PCM:
                return AudioCompressionFormat.PCM;
            case EasyVoiceSettings.AudioImportSettings.CompressionFormat.Vorbis:
                return AudioCompressionFormat.Vorbis;
            case EasyVoiceSettings.AudioImportSettings.CompressionFormat.ADPCM:
                return AudioCompressionFormat.ADPCM;
            //case EasyVoiceSettings.AudioImportSettings.CompressionFormat.MP3: -- Unity doesn't support this for overall/default settings
            //    return AudioCompressionFormat.MP3;
            //case EasyVoiceSettings.AudioImportSettings.CompressionFormat.VAG: -- Unity doesn't support this for overall/default settings
            //    return AudioCompressionFormat.VAG;
            //case EasyVoiceSettings.AudioImportSettings.CompressionFormat.HEVAG: -- Unity doesn't support this for overall/default settings
            //    return AudioCompressionFormat.HEVAG;
            default:
                return AudioCompressionFormat.Vorbis; // Do we have corrupted data?
        }
    }

    //public static EasyVoiceSettings.AudioImportSettings.CompressionFormat CompressionFormat(AudioCompressionFormat format)
    //{
    //    switch (format)
    //    {
    //        case AudioCompressionFormat.PCM:
    //            return EasyVoiceSettings.AudioImportSettings.CompressionFormat.PCM;
    //        case AudioCompressionFormat.Vorbis:
    //            return EasyVoiceSettings.AudioImportSettings.CompressionFormat.Vorbis;
    //        case AudioCompressionFormat.ADPCM:
    //            return EasyVoiceSettings.AudioImportSettings.CompressionFormat.ADPCM;
    //        //case AudioCompressionFormat.MP3:
    //        //    return EasyVoiceSettings.AudioImportSettings.CompressionFormat.MP3; -- Unity doesn't support this for overall/default settings
    //        //case AudioCompressionFormat.VAG:
    //        //    return EasyVoiceSettings.AudioImportSettings.CompressionFormat.VAG; -- Unity doesn't support this for overall/default settings
    //        //case AudioCompressionFormat.HEVAG:
    //        //    return EasyVoiceSettings.AudioImportSettings.CompressionFormat.HEVAG; -- Unity doesn't support this for overall/default settings
    //        default:
    //            return EasyVoiceSettings.AudioImportSettings.CompressionFormat.Vorbis; // Unity has a new value?
    //    }
    //}

    private static AudioSampleRateSetting SampleRate(EasyVoiceSettings.AudioImportSettings.SampleRate sampleRate)
    {
        switch (sampleRate)
        {
            case EasyVoiceSettings.AudioImportSettings.SampleRate.PreserveSampleRate:
                return AudioSampleRateSetting.PreserveSampleRate;
            case EasyVoiceSettings.AudioImportSettings.SampleRate.OptimizeSampleRate:
                return AudioSampleRateSetting.OptimizeSampleRate;
            case EasyVoiceSettings.AudioImportSettings.SampleRate.OverrideSampleRate:
                return AudioSampleRateSetting.OverrideSampleRate;
            default:
                return AudioSampleRateSetting.PreserveSampleRate; // Do we have corrupted data?
        }
    }

    //public static EasyVoiceSettings.AudioImportSettings.SampleRate SampleRate(AudioSampleRateSetting sampleRate)
    //{
    //    switch (sampleRate)
    //    {
    //        case AudioSampleRateSetting.PreserveSampleRate:
    //            return EasyVoiceSettings.AudioImportSettings.SampleRate.PreserveSampleRate;
    //        case AudioSampleRateSetting.OptimizeSampleRate:
    //            return EasyVoiceSettings.AudioImportSettings.SampleRate.OptimizeSampleRate;
    //        case AudioSampleRateSetting.OverrideSampleRate:
    //            return EasyVoiceSettings.AudioImportSettings.SampleRate.OverrideSampleRate;
    //        default:
    //            return EasyVoiceSettings.AudioImportSettings.SampleRate.PreserveSampleRate; // Unity has a new value?
    //    }
    //}
    
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        // This will make sure that if any changed assets match our lines, then these lines will have their clip/file name re-checked for issues (or no more issues)

        if (EasyVoiceSettings.instance != null && EasyVoiceSettings.instance.data != null) // we are not yet initialized?
        {
            EasyVoiceDataAsset data = EasyVoiceSettings.instance.data;
            for (int lineIndex = 0; lineIndex < data.LineCount(); lineIndex++)
            {
                //string assetFileName, fullFileName;
                //EasyVoiceClipCreator.GenerateFullFileName(lineIndex, out assetFileName, out fullFileName);
                //if ((deletedAssets != null && deletedAssets.Contains(assetFileName)) || 
                //    (movedAssets != null && movedAssets.Contains(assetFileName)) || 
                //    (importedAssets != null && importedAssets.Contains(assetFileName)) || 
                //    (movedFromAssetPaths != null && movedFromAssetPaths.Contains(assetFileName)))
                //{ -- WE CAN'T RELY ON THIS, IT DOESN'T TELL US WHAT THE FILE NAME *WAS* (and keeping this cached is an overkill imo)
                EasyVoiceIssueChecker.VerifyFileNameOrClip(lineIndex);
                //    break;
                //}
            }
        }
    }
}


