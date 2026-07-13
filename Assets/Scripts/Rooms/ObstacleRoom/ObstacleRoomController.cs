using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleRoomController : MonoBehaviour
{
    public event Action OnRoomFailed;
    public event Action OnRoomCleared;

    [Header("연결")]
    [SerializeField] private VerticalDodgePlayer player;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform scrollRoot;

    [Header("스크롤")]
    [SerializeField] private float scrollSpeed = 5f;

    private Vector3 originalScrollPosition;
    private bool isRunning;
    private bool isFinished;

    private void Awake()
    {
        originalScrollPosition = scrollRoot.localPosition;
    }

    private void Start()
    {
        player.OnObstacleHit += FailRoom;
        StartRoom();
    }

    private void Update()
    {
        if (isRunning)
        {
            ScrollStage();
        }

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartRoom();
        }
    }

    private void ScrollStage()
    {
        scrollRoot.Translate(Vector3.left * scrollSpeed * Time.deltaTime);
    }

    public void StartRoom()
    {
        isRunning = true;
        isFinished = false;

        scrollRoot.gameObject.SetActive(true);
        scrollRoot.localPosition = originalScrollPosition;

        Vector3 spawnPosition = playerSpawnPoint != null
            ? playerSpawnPoint.position
            : player.transform.position;

        player.ResetPlayer(spawnPosition);

        Debug.Log("장애물 피하기 시작!");
    }

    private void FailRoom()
    {
        if (isFinished)
            return;

        Debug.Log("장애물 충돌! 스폰포인트로 돌아갑니다.");

        OnRoomFailed?.Invoke();

        // 플레이어와 스크롤을 처음 상태로 되돌린다.
        StartRoom();
    }

    public void ClearRoom()
    {
        if (isFinished)
            return;

        isRunning = false;
        isFinished = true;

        player.StopMovement();

        Debug.Log("장애물 방 클리어!");

        OnRoomCleared?.Invoke();

        // 임시 확인용
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnObstacleHit -= FailRoom;
        }
    }
}