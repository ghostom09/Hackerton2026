using UnityEngine;

public enum SceneName
{
    MainMenu,
    InGame,
    HappyEnding,
    SadEnding,
    Ending,
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
        string sceneName = scene switch
        {
            SceneName.InGame => "InGameScene",
            SceneName.SadEnding => "BadEnding",
            SceneName.Ending => "Ending",
            _ => scene.ToString(),
        };

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
