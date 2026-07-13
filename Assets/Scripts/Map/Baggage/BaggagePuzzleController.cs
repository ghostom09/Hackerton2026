using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaggagePuzzleController : MonoBehaviour
{
    private const string DefaultMissionText = "\uC218\uD654\uBB3C\uC744 \uC548\uC804\uD55C \uACF3\uC73C\uB85C \uC62E\uAE30\uC138\uC694";

    [Serializable]
    public class BaggagePlacement
    {
        public Vector2Int startCell;
        public Vector2Int targetCell;

        public BaggagePlacement(Vector2Int startCell, Vector2Int targetCell)
        {
            this.startCell = startCell;
            this.targetCell = targetCell;
        }
    }

    [Header("Mission")]
    [SerializeField] private string missionText = "수화물을 안전한 곳으로 옮기세요";
    [SerializeField, Min(1)] private int baggageCount = 3;
    [SerializeField] private Vector2Int boardSize = new Vector2Int(8, 6);
    [SerializeField] private Vector2Int playerStartCell = new Vector2Int(1, 2);
    [SerializeField, HideInInspector] private BaggagePlacement[] baggagePlacements =
    {
        new BaggagePlacement(new Vector2Int(3, 1), new Vector2Int(6, 1)),
        new BaggagePlacement(new Vector2Int(3, 3), new Vector2Int(6, 3)),
        new BaggagePlacement(new Vector2Int(2, 4), new Vector2Int(5, 4))
    };

    [Header("Random Layout")]
    [SerializeField] private Vector2Int[] baggageStartCells =
    {
        new Vector2Int(2, 1),
        new Vector2Int(3, 1),
        new Vector2Int(2, 3),
        new Vector2Int(3, 3),
        new Vector2Int(2, 4),
        new Vector2Int(4, 4)
    };
    [SerializeField] private Vector2Int[] baggageTargetCells =
    {
        new Vector2Int(5, 1),
        new Vector2Int(6, 1),
        new Vector2Int(5, 3),
        new Vector2Int(6, 3),
        new Vector2Int(5, 4),
        new Vector2Int(6, 4)
    };

    [Header("Runtime")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool autoCreateCamera = true;
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField, Min(0.05f)] private float cellSize = 1f;
    [SerializeField, Min(0.01f)] private float moveDuration = 0.12f;

    [Header("Visuals")]
    [SerializeField] private Color floorColor = new Color(0.28f, 0.31f, 0.34f);
    [SerializeField] private Color alternateFloorColor = new Color(0.24f, 0.27f, 0.3f);
    [SerializeField] private Color targetColor = new Color(0.95f, 0.12f, 0.1f);
    [SerializeField] private Color baggageColor = new Color(0.79f, 0.53f, 0.25f);
    [SerializeField] private Color playerColor = new Color(0.16f, 0.58f, 1f);

    [Header("References")]
    [SerializeField] private Transform generatedRoot;
    [SerializeField] private BaggagePuzzlePlayer player;
    [SerializeField] private BaggagePuzzleUI puzzleUI;

    public event Action OnPuzzleCleared;

    private readonly Dictionary<Vector2Int, BaggagePuzzleCrate> baggageByCell = new();
    private readonly Dictionary<BaggagePuzzleCrate, Vector2Int> initialBaggageCells = new();
    private readonly HashSet<Vector2Int> targetCells = new();
    private readonly List<BaggagePuzzleCrate> baggageList = new();

    private Sprite squareSprite;
    private Vector2Int playerCell;
    private Vector2Int initialPlayerCell;
    private bool isMoving;
    private bool isCleared;

    public string MissionText => missionText;
    public int BaggageCount => baggageCount;
    public int SafeBaggageCount => CountSafeBaggage();
    public bool IsCleared => isCleared;

    private void Awake()
    {
        ClampSettings();
        EnsurePlacementCapacity();
        EnsureLayoutCandidates();
    }

    private void Start()
    {
        if (buildOnStart)
            BuildPuzzle();
    }

    private void OnValidate()
    {
        ClampSettings();
        EnsurePlacementCapacity();
        EnsureLayoutCandidates();
    }

    [ContextMenu("Build Baggage Puzzle")]
    public void BuildPuzzle()
    {
        StopAllCoroutines();
        isMoving = false;
        isCleared = false;

        ClearGeneratedObjects();
        EnsureRoot();
        EnsureSprite();

        baggageByCell.Clear();
        initialBaggageCells.Clear();
        baggageList.Clear();
        targetCells.Clear();

        BuildBoard();
        BuildTargets();
        BuildBaggage();
        BuildPlayer();
        SetupCamera();
        SetupUI();
        RefreshUI();
    }

    public void ResetPuzzle()
    {
        BuildPuzzle();
    }

    public bool TryReturnToStart()
    {
        if (isMoving || IsAtInitialPosition())
            return false;

        StartCoroutine(ReturnToInitialPosition());
        return true;
    }

    public bool TryMovePlayer(Vector2Int direction)
    {
        if (isMoving || isCleared || direction == Vector2Int.zero)
            return false;

        Vector2Int nextPlayerCell = playerCell + direction;
        if (!IsInsideBoard(nextPlayerCell))
            return false;

        if (baggageByCell.TryGetValue(nextPlayerCell, out BaggagePuzzleCrate baggage))
        {
            Vector2Int nextBaggageCell = nextPlayerCell + direction;
            if (!CanMoveBaggageTo(nextBaggageCell))
                return false;

            StartCoroutine(MovePlayerAndBaggage(direction, baggage, nextPlayerCell, nextBaggageCell));
            return true;
        }

        StartCoroutine(MovePlayerOnly(nextPlayerCell));
        return true;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        float xOffset = (boardSize.x - 1) * 0.5f;
        float yOffset = (boardSize.y - 1) * 0.5f;
        return new Vector3((cell.x - xOffset) * cellSize, (cell.y - yOffset) * cellSize, 0f);
    }

    private IEnumerator MovePlayerOnly(Vector2Int nextPlayerCell)
    {
        isMoving = true;

        yield return SlideTransform(player.transform, CellToWorld(nextPlayerCell), moveDuration);

        playerCell = nextPlayerCell;
        player.SetCell(playerCell);

        isMoving = false;
        RefreshUI();
    }

    private IEnumerator MovePlayerAndBaggage(
        Vector2Int direction,
        BaggagePuzzleCrate baggage,
        Vector2Int nextPlayerCell,
        Vector2Int nextBaggageCell)
    {
        isMoving = true;

        baggageByCell.Remove(baggage.Cell);
        baggage.SetCell(nextBaggageCell);
        baggageByCell[nextBaggageCell] = baggage;

        player.SetFacingDirection(direction);

        Coroutine baggageMove = StartCoroutine(SlideTransform(baggage.transform, CellToWorld(nextBaggageCell), moveDuration));
        yield return SlideTransform(player.transform, CellToWorld(nextPlayerCell), moveDuration);
        yield return baggageMove;

        playerCell = nextPlayerCell;
        player.SetCell(playerCell);

        isMoving = false;

        RefreshUI();
        CheckClear();
    }

    private IEnumerator ReturnToInitialPosition()
    {
        isMoving = true;
        isCleared = false;

        List<Coroutine> baggageMoves = new();
        baggageByCell.Clear();

        for (int i = 0; i < baggageList.Count; i++)
        {
            BaggagePuzzleCrate baggage = baggageList[i];
            Vector2Int initialCell = initialBaggageCells[baggage];
            baggage.SetCell(initialCell);
            baggageByCell[initialCell] = baggage;
            baggageMoves.Add(StartCoroutine(SlideTransform(baggage.transform, CellToWorld(initialCell), moveDuration)));
        }

        playerCell = initialPlayerCell;
        player.SetCell(playerCell);
        yield return SlideTransform(player.transform, CellToWorld(initialPlayerCell), moveDuration);

        for (int i = 0; i < baggageMoves.Count; i++)
            yield return baggageMoves[i];

        isMoving = false;
        RefreshUI();
    }

    private IEnumerator SlideTransform(Transform target, Vector3 destination, float duration)
    {
        Vector3 start = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }

        target.position = destination;
    }

    private bool CanMoveBaggageTo(Vector2Int cell)
    {
        return IsInsideBoard(cell) && !baggageByCell.ContainsKey(cell);
    }

    private bool IsInsideBoard(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < boardSize.x && cell.y < boardSize.y;
    }

    private void CheckClear()
    {
        if (CountSafeBaggage() < baggageCount)
            return;

        isCleared = true;
        RefreshUI();
        OnPuzzleCleared?.Invoke();
    }

    private int CountSafeBaggage()
    {
        int safeCount = 0;

        for (int i = 0; i < baggageList.Count; i++)
        {
            if (targetCells.Contains(baggageList[i].Cell))
                safeCount++;
        }

        return safeCount;
    }

    private void BuildBoard()
    {
        Transform boardRoot = CreateChildRoot("Board");

        for (int y = 0; y < boardSize.y; y++)
        {
            for (int x = 0; x < boardSize.x; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                GameObject tile = CreateSpriteObject($"Floor_{x}_{y}", boardRoot, cell, (x + y) % 2 == 0 ? floorColor : alternateFloorColor, 0);
                tile.transform.localScale = Vector3.one * (cellSize * 0.96f);
            }
        }
    }

    private void BuildTargets()
    {
        Transform targetRoot = CreateChildRoot("Safe Targets");
        HashSet<Vector2Int> usedTargets = new();
        List<Vector2Int> targetSelection = PickUniqueRandomCells(baggageTargetCells, baggageCount, null);

        for (int i = 0; i < baggageCount; i++)
        {
            Vector2Int targetCell = FindAvailableCell(targetSelection[i], usedTargets);
            usedTargets.Add(targetCell);
            targetCells.Add(targetCell);

            GameObject target = CreateSpriteObject($"Safe_Target_{i + 1}", targetRoot, targetCell, targetColor, 1);
            target.transform.localScale = Vector3.one * (cellSize * 0.72f);
        }
    }

    private void BuildBaggage()
    {
        Transform baggageRoot = CreateChildRoot("Baggage");
        HashSet<Vector2Int> blockedStartCells = new(targetCells);
        blockedStartCells.Add(ClampToBoard(playerStartCell));

        List<Vector2Int> startSelection = PickUniqueRandomCells(baggageStartCells, baggageCount, blockedStartCells);
        HashSet<Vector2Int> usedCells = new(blockedStartCells);

        for (int i = 0; i < baggageCount; i++)
        {
            Vector2Int cell = FindAvailableCell(startSelection[i], usedCells);
            usedCells.Add(cell);

            GameObject baggageObject = CreateSpriteObject($"Baggage_{i + 1}", baggageRoot, cell, baggageColor, 3);
            baggageObject.transform.localScale = Vector3.one * (cellSize * 0.82f);

            BaggagePuzzleCrate baggage = baggageObject.AddComponent<BaggagePuzzleCrate>();
            baggage.Init(cell);
            baggageList.Add(baggage);
            initialBaggageCells[baggage] = cell;
            baggageByCell[cell] = baggage;
        }
    }

    private void BuildPlayer()
    {
        Vector2Int safePlayerCell = FindAvailableCell(playerStartCell, new HashSet<Vector2Int>(baggageByCell.Keys));
        playerCell = safePlayerCell;
        initialPlayerCell = safePlayerCell;

        GameObject playerObject = CreateSpriteObject("Player", generatedRoot, playerCell, playerColor, 4);
        playerObject.transform.localScale = Vector3.one * (cellSize * 0.76f);

        player = playerObject.AddComponent<BaggagePuzzlePlayer>();
        player.Init(this, playerCell);
    }

    private void SetupUI()
    {
        if (!autoCreateUI)
            return;

        if (puzzleUI == null)
        {
            GameObject uiObject = new GameObject("Baggage Puzzle UI");
            puzzleUI = uiObject.AddComponent<BaggagePuzzleUI>();
        }

        puzzleUI.BuildDefaultLayout();
        puzzleUI.Bind(this);
    }

    private void RefreshUI()
    {
        if (puzzleUI == null)
            return;

        puzzleUI.SetMission(missionText);
        puzzleUI.SetProgress(CountSafeBaggage(), baggageCount, isCleared);
        puzzleUI.SetCanUndo(!IsAtInitialPosition());
    }

    private bool IsAtInitialPosition()
    {
        if (playerCell != initialPlayerCell)
            return false;

        for (int i = 0; i < baggageList.Count; i++)
        {
            BaggagePuzzleCrate baggage = baggageList[i];
            if (!initialBaggageCells.TryGetValue(baggage, out Vector2Int initialCell) || baggage.Cell != initialCell)
                return false;
        }

        return true;
    }

    private void SetupCamera()
    {
        if (!autoCreateCamera)
            return;

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographicSize = Mathf.Max(boardSize.x, boardSize.y) * 0.68f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
    }

    private GameObject CreateSpriteObject(string objectName, Transform parent, Vector2Int cell, Color color, int sortingOrder)
    {
        GameObject spriteObject = new GameObject(objectName);
        spriteObject.transform.SetParent(parent, false);
        spriteObject.transform.position = CellToWorld(cell);

        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        return spriteObject;
    }

    private Transform CreateChildRoot(string objectName)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(generatedRoot, false);
        return root.transform;
    }

    private Vector2Int FindAvailableCell(Vector2Int preferredCell, HashSet<Vector2Int> usedCells)
    {
        Vector2Int clampedCell = ClampToBoard(preferredCell);
        if (!usedCells.Contains(clampedCell))
            return clampedCell;

        for (int y = 0; y < boardSize.y; y++)
        {
            for (int x = 0; x < boardSize.x; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);
                if (!usedCells.Contains(candidate))
                    return candidate;
            }
        }

        return clampedCell;
    }

    private List<Vector2Int> PickUniqueRandomCells(Vector2Int[] candidates, int count, HashSet<Vector2Int> blockedCells)
    {
        List<Vector2Int> availableCells = new();
        HashSet<Vector2Int> seenCells = new();

        if (candidates != null)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2Int cell = ClampToBoard(candidates[i]);
                if ((blockedCells != null && blockedCells.Contains(cell)) || !seenCells.Add(cell))
                    continue;

                availableCells.Add(cell);
            }
        }

        Shuffle(availableCells);

        List<Vector2Int> selectedCells = new();
        for (int i = 0; i < availableCells.Count && selectedCells.Count < count; i++)
            selectedCells.Add(availableCells[i]);

        for (int y = 0; y < boardSize.y && selectedCells.Count < count; y++)
        {
            for (int x = 0; x < boardSize.x && selectedCells.Count < count; x++)
            {
                Vector2Int fallbackCell = new Vector2Int(x, y);
                if ((blockedCells != null && blockedCells.Contains(fallbackCell)) || seenCells.Contains(fallbackCell))
                    continue;

                selectedCells.Add(fallbackCell);
                seenCells.Add(fallbackCell);
            }
        }

        return selectedCells;
    }

    private void Shuffle(List<Vector2Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (cells[i], cells[randomIndex]) = (cells[randomIndex], cells[i]);
        }
    }

    private Vector2Int ClampToBoard(Vector2Int cell)
    {
        return new Vector2Int(
            Mathf.Clamp(cell.x, 0, boardSize.x - 1),
            Mathf.Clamp(cell.y, 0, boardSize.y - 1));
    }

    private void ClampSettings()
    {
        if (string.IsNullOrWhiteSpace(missionText) || missionText.Contains("?"))
            missionText = DefaultMissionText;

        boardSize.x = Mathf.Max(2, boardSize.x);
        boardSize.y = Mathf.Max(2, boardSize.y);

        int maxBaggageCount = Mathf.Max(1, (boardSize.x * boardSize.y - 1) / 2);
        baggageCount = Mathf.Clamp(baggageCount, 1, maxBaggageCount);
    }

    private void EnsureRoot()
    {
        if (generatedRoot != null)
            return;

        GameObject root = new GameObject("Generated Baggage Puzzle");
        root.transform.SetParent(transform, false);
        generatedRoot = root.transform;
    }

    private void EnsureSprite()
    {
        if (squareSprite != null)
            return;

        Texture2D texture = new Texture2D(1, 1);
        texture.name = "BaggagePuzzlePixel";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void ClearGeneratedObjects()
    {
        if (generatedRoot != null)
        {
            if (Application.isPlaying)
                Destroy(generatedRoot.gameObject);
            else
                DestroyImmediate(generatedRoot.gameObject);
        }

        generatedRoot = null;
        player = null;
    }

    private void EnsurePlacementCapacity()
    {
        if (baggagePlacements == null)
            baggagePlacements = Array.Empty<BaggagePlacement>();

        if (baggagePlacements.Length == baggageCount)
            return;

        BaggagePlacement[] nextPlacements = new BaggagePlacement[baggageCount];
        int copyLength = Mathf.Min(baggagePlacements.Length, nextPlacements.Length);

        for (int i = 0; i < copyLength; i++)
            nextPlacements[i] = baggagePlacements[i];

        for (int i = copyLength; i < nextPlacements.Length; i++)
        {
            int row = i % Mathf.Max(1, boardSize.y - 2);
            Vector2Int start = new Vector2Int(Mathf.Clamp(2 + i, 0, boardSize.x - 2), 1 + row);
            Vector2Int target = new Vector2Int(boardSize.x - 2, 1 + row);
            nextPlacements[i] = new BaggagePlacement(start, target);
        }

        baggagePlacements = nextPlacements;
    }

    private void EnsureLayoutCandidates()
    {
        if (baggageStartCells == null || baggageStartCells.Length == 0)
        {
            baggageStartCells = new[]
            {
                new Vector2Int(2, 1),
                new Vector2Int(3, 1),
                new Vector2Int(2, 3),
                new Vector2Int(3, 3),
                new Vector2Int(2, 4),
                new Vector2Int(4, 4)
            };
        }

        if (baggageTargetCells == null || baggageTargetCells.Length == 0)
        {
            baggageTargetCells = new[]
            {
                new Vector2Int(boardSize.x - 3, 1),
                new Vector2Int(boardSize.x - 2, 1),
                new Vector2Int(boardSize.x - 3, 3),
                new Vector2Int(boardSize.x - 2, 3),
                new Vector2Int(boardSize.x - 3, 4),
                new Vector2Int(boardSize.x - 2, 4)
            };
        }
    }
}
