using UnityEngine;
using UnityEngine.InputSystem;

public class MapSpawn : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            GameManager.Instance.NextMap();
    }
}
