using UnityEngine;

public enum SceneName
{
    MainMenu,
    Prologue,
    InGameScene,
    
    HappyEnding,
    BadEnding,
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
