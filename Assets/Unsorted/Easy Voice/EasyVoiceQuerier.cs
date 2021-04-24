/******************************************************************************
 * Copyright (c) 2014 Game Loop
 * All Rights reserved.
 *****************************************************************************/

public abstract class EasyVoiceQuerier
{
    public abstract string Name { get; }

    /// <summary>
    /// Includes '.'
    /// </summary>
    public abstract string FileExtension { get; }

    public abstract bool VerifyApp();

    public abstract void QueryForVoiceList(EasyVoiceSettings settings);
    
    public abstract void AskToMakeFile(string speechText, string speakerName, string fullFileName, string fileFormat);
}
