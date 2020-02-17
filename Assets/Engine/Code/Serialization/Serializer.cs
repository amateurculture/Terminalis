using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class Serializer
{
    public static List<Game> savedGames = new List<Game>();

    public static void Save()
    {
        Game game = new Game();
        game.player = new S_Agent(Brain.instance.player);
        //game.camera = new S_Camera(Camera.main.transform.parent.parent.GetComponent<BzFreeLookCam>());
        game.SerializeAgentList(Brain.instance.automataList);
        game.sceneName = Brain.instance.sceneName;
        game.isSavedGame = true;
        Serializer.savedGames.Add(game);
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/savedGames.gd");
        bf.Serialize(file, Serializer.savedGames);
        file.Close();
    }

    public static string Load()
    {
        if (File.Exists(Application.persistentDataPath + "/savedGames.gd"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/savedGames.gd", FileMode.Open);
            Serializer.savedGames = (List<Game>)bf.Deserialize(file);
            file.Close();

            var game = Serializer.savedGames[Serializer.savedGames.Count-1];
            game.player.Deserialize(Brain.instance.player);
            //game.camera.Deserialize(Camera.main.transform.parent.parent.GetComponent<BzFreeLookCam>());
       
            return game.sceneName;
        }
        return "";
    }
}
