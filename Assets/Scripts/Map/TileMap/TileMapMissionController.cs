using System;
using System.Collections;
using UnityEngine;

public class TileMapMissionController : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private Vector2Int mapSize = new(10, 7);
    [SerializeField, Min(1)] private int targetTileCount = 5;
    [SerializeField] private Vector2Int playerStartCell = new(5, 3);
    [SerializeField, Min(0.1f)] private float cellSize = 1f;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveDuration = 0.1f;

    [Header("Colours")]
    [SerializeField] private Color floorColor = new(0.19f, 0.23f, 0.29f);
    [SerializeField] private Color floorAccentColor = new(0.16f, 0.2f, 0.26f);
    [SerializeField] private Color targetColor = new(0.93f, 0.16f, 0.18f);
    [SerializeField] private Color steppedTargetColor = new(0.2f, 0.78f, 0.34f);
    [SerializeField] private Color playerColor = new(1f, 0.83f, 0.22f);
    [SerializeField] private Color lockedDoorColor = new(0.34f, 0.22f, 0.27f);
    [SerializeField] private Color openDoorColor = new(0.28f, 0.86f, 0.8f);

    [Header("Setup")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool configureMainCamera = true;

    public event Action OnAllTargetsStepped;
    public event Action OnStageCompleted;

    private Transform generatedRoot;
    private TileMapBoard board;
    private TileMapExitDoor exitDoor;
    private TileMapPlayerMover player;
    private Sprite squareSprite;
    private Vector2Int playerCell;
    private Vector2Int exitCell;
    private bool isMoving;
    private bool targetsComplete;
    private bool stageComplete;

    public int TargetTileCount => targetTileCount;
    public int SteppedTargetCount => board == null ? 0 : board.SteppedTargetCount;
    public bool TargetsComplete => targetsComplete;
    public bool StageComplete => stageComplete;

    private void Awake()
    {
        ClampSettings();
    }

    private void Start()
    {
        if (buildOnStart)
            BuildMap();
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    [ContextMenu("Build Tile Map Mission")]
    public void BuildMap()
    {
        StopAllCoroutines();
        ClampSettings();
        ClearGeneratedObjects();
        EnsureSquareSprite();

        generatedRoot = new GameObject("Generated Tile Map Mission").transform;
        generatedRoot.SetParent(transform, false);
        playerCell = playerStartCell;
        exitCell = new Vector2Int(mapSize.x - 1, playerStartCell.y);
        isMoving = false;
        targetsComplete = false;
        stageComplete = false;

        CreateBoard();
        CreatePlayer();
        CreateExitDoor();
        ConfigureCamera();
    }
    
    public bool TryMovePlayer(Vector2Int direction)
    {
        if (isMoving || stageComplete || direction == Vector2Int.zero)
            return false;

        Vector2Int nextCell = playerCell + direction;
        if (nextCell == exitCell)
        {
            if (!targetsComplete)
                return false;

            StartCoroutine(ExitStage());
            return true;
        }

        if (!board.IsInsideMap(nextCell))
            return false;

        StartCoroutine(MovePlayerTo(nextCell));
        return true;
    }

    public void ResetMission()
    {
        BuildMap();
    }

    private void CreateBoard()
    {
        GameObject boardObject = new GameObject("Tile Map Board");
        boardObject.transform.SetParent(generatedRoot, false);

        board = boardObject.AddComponent<TileMapBoard>();
        board.Initialize(
            mapSize,
            cellSize,
            targetTileCount,
            playerStartCell,
            exitCell,
            squareSprite,
            floorColor,
            floorAccentColor,
            targetColor,
            steppedTargetColor);
    }

    private void CreatePlayer()
    {
        GameObject playerObject = CreateSpriteObject("Player", board.CellToWorld(playerStartCell), playerColor, 3);
        playerObject.transform.localScale = Vector3.one * (cellSize * 0.66f);

        player = playerObject.AddComponent<TileMapPlayerMover>();
        player.Initialize(this, playerStartCell);
    }

    private void CreateExitDoor()
    {
        GameObject doorObject = new GameObject("Exit Door");
        doorObject.transform.SetParent(generatedRoot, false);

        exitDoor = doorObject.AddComponent<TileMapExitDoor>();
        exitDoor.Initialize(
            squareSprite,
            board.CellToWorld(exitCell),
            cellSize,
            lockedDoorColor,
            openDoorColor);
    }

    private IEnumerator MovePlayerTo(Vector2Int nextCell)
    {
        isMoving = true;
        yield return MoveTransform(player.transform, board.CellToWorld(nextCell), moveDuration);

        playerCell = nextCell;
        player.SetCell(playerCell);
        CheckTargetTile();
        isMoving = false;
    }

    private IEnumerator ExitStage()
    {
        isMoving = true;
        yield return MoveTransform(player.transform, board.CellToWorld(exitCell), moveDuration);

        playerCell = exitCell;
        player.SetCell(playerCell);
        stageComplete = true;
        isMoving = false;
        
        OnStageCompleted?.Invoke();
    }

    private void CheckTargetTile()
    {
        if (!board.TryStepOnTarget(playerCell) || targetsComplete || !board.AreAllTargetsStepped)
            return;

        targetsComplete = true;
        exitDoor.Open();

        OnAllTargetsStepped?.Invoke();
    }

    private IEnumerator MoveTransform(Transform target, Vector3 destination, float duration)
    {
        Vector3 start = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.position = Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        target.position = destination;
    }

    private GameObject CreateSpriteObject(string objectName, Vector3 position, Color color, int sortingOrder)
    {
        GameObject spriteObject = new GameObject(objectName);
        spriteObject.transform.SetParent(generatedRoot, false);
        spriteObject.transform.position = position;

        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return spriteObject;
    }

    private void ConfigureCamera()
    {
        if (!configureMainCamera || Camera.main == null)
            return;

        Camera mainCamera = Camera.main;
        mainCamera.orthographic = true;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.orthographicSize = Mathf.Max(mapSize.y * 0.65f, mapSize.x * 0.4f) + 1f;
        mainCamera.backgroundColor = new Color(0.055f, 0.07f, 0.1f);
    }

    private void EnsureSquareSprite()
    {
        if (squareSprite != null)
            return;

        Texture2D texture = new(1, 1);
        texture.name = "TileMapMissionPixel";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void ClearGeneratedObjects()
    {
        if (generatedRoot == null)
            return;

        if (Application.isPlaying)
            Destroy(generatedRoot.gameObject);
        else
            DestroyImmediate(generatedRoot.gameObject);

        generatedRoot = null;
        board = null;
        exitDoor = null;
        player = null;
    }

    private void ClampSettings()
    {
        mapSize.x = Mathf.Max(2, mapSize.x);
        mapSize.y = Mathf.Max(1, mapSize.y);
        playerStartCell = new Vector2Int(
            Mathf.Clamp(playerStartCell.x, 0, mapSize.x - 1),
            Mathf.Clamp(playerStartCell.y, 0, mapSize.y - 1));
        Vector2Int doorCell = new(mapSize.x - 1, playerStartCell.y);
        int blockedCellCount = playerStartCell == doorCell ? 1 : 2;
        targetTileCount = Mathf.Clamp(targetTileCount, 1, mapSize.x * mapSize.y - blockedCellCount);
    }
}
