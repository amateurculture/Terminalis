using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

public class SerializeManager : MonoBehaviour
{
    public GameObject dynamic;
    List<S_SaveItem> saveItems;

    [InspectorButton("OnSaveGameClicked")]
    public bool saveGame;

    [InspectorButton("OnLoadGameClicked")]

    public bool loadGame;
    string RemoveBetween(string s, char begin, char end)
    {
        Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", begin, end));
        return regex.Replace(s, string.Empty);
    }

    private void OnSaveGameClicked()
    {
        saveItems = new List<S_SaveItem>();
        foreach (Transform obj in transform)
        {
            S_SaveItem item = new S_SaveItem();
            item.prefabName = RemoveBetween(obj.name, '(', ')');
            item.position = new S_Vector3(obj.position);
            item.rotation = new S_Quaternion(obj.rotation);
            item.scale = new S_Vector3(obj.localScale);
            saveItems.Add(item);
        }

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/savedGames.gd");
        bf.Serialize(file, saveItems);
        file.Close();
    }

    private void OnLoadGameClicked()
    {
        foreach (Transform obj in transform)
        {
            Destroy(obj);
        }

        if (File.Exists(Application.persistentDataPath + "/savedGames.gd"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/savedGames.gd", FileMode.Open);
            saveItems = (List<S_SaveItem>)bf.Deserialize(file);
            file.Close();

            foreach (S_SaveItem saveItem in saveItems)
            {
                GameObject prefab = Resources.Load(saveItem.prefabName) as GameObject;

                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.transform.position = new Vector3(saveItem.position.x, saveItem.position.y, saveItem.position.z);
                    obj.transform.rotation = new Quaternion(saveItem.rotation.x, saveItem.rotation.y, saveItem.rotation.z, saveItem.rotation.w);
                    obj.transform.localScale = new Vector3(saveItem.position.x, saveItem.position.y, saveItem.position.z); 
                    obj.transform.parent = dynamic.transform;
                }
            }
        }
    }
}
