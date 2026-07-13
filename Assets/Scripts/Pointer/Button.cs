using UnityEngine;

public class Button : MonoBehaviour, IPointerHover, IPointerHoverExit, IPointerClick
{
    [SerializeField] private Sprite exitHoverSprite;
    [SerializeField] private Sprite onHoverSprite;
    [SerializeField] private SpriteRenderer sr;
    public bool interactable;

    public void OnPointerHover()
    {
        sr.sprite = onHoverSprite;
    }

    public void OnPointerHoverExit()
    {
        sr.sprite = exitHoverSprite;
    }

    public void OnPointerClick()
    {
        GameManager.Instance.RequestNextMap();
        Debug.Log($"[Button] Clicked: {name}", this);
    }
}
