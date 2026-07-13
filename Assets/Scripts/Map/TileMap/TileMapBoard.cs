using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapBoard : MonoBehaviour
{
    private readonly HashSet<Vector3Int> targetCells = new();
    private readonly HashSet<Vector3Int> steppedTargetCells = new();

    private Vector2Int mapSize;
    private float cellSize;
    private Tilemap floorTilemap;

    public int SteppedTargetCount => steppedTargetCells.Count;
    public bool AreAllTargetsStepped => targetCells.Count > 0 && steppedTargetCells.Count == targetCells.Count;

    public void Initialize(
        Vector2Int newMapSize,
        float newCellSize,
        int targetTileCount,
        Vector2Int playerStartCell,
        Vector2Int exitCell,
        Sprite tileSprite,
        Color floorColor,
        Color floorAccentColor,
        Color targetColor,
        Color steppedTargetColor)
    {
        mapSize = newMapSize;
        cellSize = newCellSize;

        CreateTilemap(tileSprite);
        PaintFloor(floorColor, floorAccentColor);
        PlaceRandomTargets(targetTileCount, playerStartCell, exitCell, targetColor);
        this.steppedTargetColor = steppedTargetColor;
    }

    public bool IsInsideMap(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < mapSize.x && cell.y < mapSize.y;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            (cell.x - (mapSize.x - 1) * 0.5f) * cellSize,
            (cell.y - (mapSize.y - 1) * 0.5f) * cellSize,
            -0.1f);
    }
    
    public bool TryStepOnTarget(Vector2Int cell)
    {
        Vector3Int tileCell = new(cell.x, cell.y, 0);
        if (!targetCells.Contains(tileCell) || !steppedTargetCells.Add(tileCell))
            return false;

        floorTilemap.SetColor(tileCell, steppedTargetColor);
        return true;
    }

    private Color steppedTargetColor;

    private void CreateTilemap(Sprite tileSprite)
    {
        GameObject gridObject = new GameObject("Grid");
        gridObject.transform.SetParent(transform, false);

        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one * cellSize;
        gridObject.transform.position = new Vector3(-mapSize.x * cellSize * 0.5f, -mapSize.y * cellSize * 0.5f, 0f);

        GameObject tilemapObject = new GameObject("Floor Tilemap");
        tilemapObject.transform.SetParent(gridObject.transform, false);
        floorTilemap = tilemapObject.AddComponent<Tilemap>();
        floorTilemap.gameObject.AddComponent<TilemapRenderer>().sortingOrder = 0;

        Tile floorTile = ScriptableObject.CreateInstance<Tile>();
        floorTile.sprite = tileSprite;
        floorTile.flags = TileFlags.None;

        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                Vector3Int cell = new(x, y, 0);
                floorTilemap.SetTile(cell, floorTile);
                floorTilemap.SetTileFlags(cell, TileFlags.None);
            }
        }
    }

    private void PaintFloor(Color floorColor, Color floorAccentColor)
    {
        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                floorTilemap.SetColor(
                    new Vector3Int(x, y, 0),
                    (x + y) % 2 == 0 ? floorColor : floorAccentColor);
            }
        }
    }

    private void PlaceRandomTargets(
        int targetTileCount,
        Vector2Int playerStartCell,
        Vector2Int exitCell,
        Color targetColor)
    {
        List<Vector3Int> candidates = new();
        Vector3Int playerCell = new(playerStartCell.x, playerStartCell.y, 0);
        Vector3Int doorCell = new(exitCell.x, exitCell.y, 0);

        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                Vector3Int cell = new(x, y, 0);
                if (cell != playerCell && cell != doorCell)
                    candidates.Add(cell);
            }
        }

        Shuffle(candidates);
        for (int i = 0; i < targetTileCount; i++)
        {
            Vector3Int targetCell = candidates[i];
            targetCells.Add(targetCell);
            floorTilemap.SetColor(targetCell, targetColor);
        }
    }

    private static void Shuffle(List<Vector3Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (cells[i], cells[randomIndex]) = (cells[randomIndex], cells[i]);
        }
    }
}
