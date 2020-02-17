using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleporter : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Brain.instance.LoadScene("Scene2", true);
    }

    /*
    public string sceneName;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Brain.instance.currentScene = SceneManager.GetActiveScene();
        Brain.instance.player.transform.position = GameObject.Find("Start").transform.position;
    }

    void OnSceneUnloaded(Scene scene)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    private void OnTriggerEnter(Collider other)
    {
        SceneManager.UnloadSceneAsync(Brain.instance.sceneName);
    }
    */
}
