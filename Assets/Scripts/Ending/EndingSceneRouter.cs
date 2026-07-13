using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingSceneRouter : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForEndingScene()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Ending" && FindFirstObjectByType<EndingSceneRouter>() == null)
            new GameObject(nameof(EndingSceneRouter)).AddComponent<EndingSceneRouter>();
    }

    private void Start()
    {
        var result = GameResultManager.Instance != null ? GameResultManager.Instance.CurrentResult : new GameResultData();
        if (result.endingType == EndingType.Happy)
            FindFirstObjectByType<HappyEndingController>()?.BeginHappyEnding();
        else
            FindFirstObjectByType<EndingResultUI>()?.gameObject.SetActive(true);
    }
}
