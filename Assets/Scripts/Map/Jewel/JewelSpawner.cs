using System.Collections.Generic;
using UnityEngine;

/// <summary>맵에 쥬얼을 생성하고, 모두 획득되면 GameManager에 다음 맵을 요청합니다.</summary>
[DisallowMultipleComponent]
public class JewelSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private JewelPickup jewelPrefab;
    [SerializeField, Min(1)] private int jewelCount = 8;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Vector2 randomAreaSize = new(12f, 7f);

    private readonly List<JewelPickup> jewels = new();
    private bool isComplete;

    private void Start()
    {
        SpawnJewels();
    }

    private void OnDestroy()
    {
        foreach (JewelPickup jewel in jewels)
        {
            if (jewel != null)
            {
                jewel.PickedUp -= OnJewelPickedUp;
            }
        }
    }

    public void SpawnJewels()
    {
        isComplete = false;
        jewels.Clear();

        for (int i = 0; i < jewelCount; i++)
        {
            Vector3 position = spawnPoints != null && i < spawnPoints.Length && spawnPoints[i] != null
                ? spawnPoints[i].position
                : GetRandomPosition();

            JewelPickup jewel = CreateJewel(position);
            jewel.PickedUp += OnJewelPickedUp;
            jewels.Add(jewel);
        }
    }

    private JewelPickup CreateJewel(Vector3 position)
    {
        if (jewelPrefab != null)
        {
            return Instantiate(jewelPrefab, position, Quaternion.identity, transform);
        }

        GameObject jewelObject = new("Jewel");
        jewelObject.transform.SetParent(transform);
        jewelObject.transform.position = position;
        jewelObject.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        jewelObject.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        SpriteRenderer renderer = jewelObject.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        renderer.color = Color.cyan;

        CircleCollider2D collider = jewelObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        return jewelObject.AddComponent<JewelPickup>();
    }

    private void OnJewelPickedUp(JewelPickup jewel)
    {
        jewel.PickedUp -= OnJewelPickedUp;
        jewels.Remove(jewel);

        if (jewels.Count == 0)
        {
            CompleteMap();
        }
    }

    private void CompleteMap()
    {
        if (isComplete)
        {
            return;
        }

        isComplete = true;

        // Cheat/MagazineGrabChecker와 같은 방식으로 다음 맵을 GameManager에 요청합니다.
        if (GameManager.Instance != null)
        {
            // GameManager.Instance.RandomMap();
        }
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 halfSize = randomAreaSize * 0.5f;
        Vector3 localPosition = new(
            Random.Range(-halfSize.x, halfSize.x),
            Random.Range(-halfSize.y, halfSize.y),
            0f);
        return transform.TransformPoint(localPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(randomAreaSize.x, randomAreaSize.y, 0f));
    }
}
