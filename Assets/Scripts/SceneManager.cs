using UnityEngine;

public enum SceneName
{
    MainMenu,
    InGame,
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
            _ => scene.ToString(),
        };

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
