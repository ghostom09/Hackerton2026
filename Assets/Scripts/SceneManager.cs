using UnityEngine;

public enum SceneName
{
    MainMenu,
    InGame,
    HappyEnding,
    SadEnding,
}

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else Destroy(this);
    }

    public void ChangeScene(SceneName scene)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.ToString());
    }
}
