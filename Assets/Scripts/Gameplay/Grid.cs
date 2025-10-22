using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Grid
{
    public int Width { get; }
    public int Height { get; }

    private readonly PropView[,] _grid;
    private readonly float _cellSize;
    private readonly Vector2 _gridOffset;
    private readonly Vector3 _originPosition;

    public Grid(LevelData levelData, float cellSize, int maxGridHeight, Vector3 originPosition, bool isTutorial = false)
    {
        Height = Mathf.Min(levelData.requiredGroups.Count, maxGridHeight);
        
        if (isTutorial)
        {
            Width = 3;
        }
        else
        {
            Width = 4;
        }
        
        _grid = new PropView[Width, Height];
        _cellSize = cellSize;
        _originPosition = originPosition;
        
        float totalGridWidth = (Width - 1) * _cellSize;
        float totalGridHeight = (Height - 1) * _cellSize;
        _gridOffset = new Vector2(-totalGridWidth / 2f, -totalGridHeight / 2f);
    }

    public PropView GetPropAt(int x, int y) => IsWithinBounds(x, y) ? _grid[x, y] : null;
    public void SetPropAt(int x, int y, PropView prop) { if (IsWithinBounds(x, y)) _grid[x, y] = prop; }
    public void ClearCell(int x, int y) => SetPropAt(x, y, null);
    public bool IsWithinBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    public bool IsGridEmpty() => _grid.Cast<PropView>().All(prop => prop == null);

    public void SwapProps(Vector2Int posA, Vector2Int posB)
    {
        PropView propA = GetPropAt(posA.x, posA.y);
        PropView propB = GetPropAt(posB.x, posB.y);

        SetPropAt(posA.x, posA.y, propB);
        SetPropAt(posB.x, posB.y, propA);

        if (propA != null) propA.GridPosition = posB;
        if (propB != null) propB.GridPosition = posA;
    }

    public List<int> GetCompletedRows()
    {
        var completedRows = new List<int>();
        for (int y = 0; y < Height; y++)
        {
            PropView firstProp = GetPropAt(0, y);
            if (firstProp == null) continue;

            bool isRowComplete = true;
            for (int x = 1; x < Width; x++)
            {
                PropView currentProp = GetPropAt(x, y);
                if (currentProp == null || currentProp.AssignedGroup.groupKey != firstProp.AssignedGroup.groupKey)
                {
                    isRowComplete = false;
                    break;
                }
            }

            if (isRowComplete)
            {
                completedRows.Add(y);
            }
        }
        return completedRows;
    }
    
    public List<PropView> CollapseColumns()
    {
        var movedProps = new List<PropView>();
        for (int x = 0; x < Width; x++)
        {
            int emptySpaces = 0;
            for (int y = 0; y < Height; y++)
            {
                if (GetPropAt(x, y) == null)
                {
                    emptySpaces++;
                }
                else if (emptySpaces > 0)
                {
                    PropView prop = GetPropAt(x, y);
                    Vector2Int newPos = new Vector2Int(x, y - emptySpaces);
                    
                    SetPropAt(x, y, null);
                    SetPropAt(newPos.x, newPos.y, prop);
                    prop.GridPosition = newPos;
                    
                    movedProps.Add(prop);
                }
            }
        }
        return movedProps;
    }
    
    public List<PropView> GetAllProps()
    {
        var props = new List<PropView>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_grid[x, y] != null)
                {
                    props.Add(_grid[x, y]);
                }
            }
        }
        return props;
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return (Vector3)_gridOffset + new Vector3(x * _cellSize, y * _cellSize, 0) + _originPosition;
    }

    public Vector2Int GetGridPositionFromWorld(Vector2 worldPos)
    {
        Vector2 localPos = worldPos - ((Vector2)_originPosition + _gridOffset);
        int x = Mathf.RoundToInt(localPos.x / _cellSize);
        int y = Mathf.RoundToInt(localPos.y / _cellSize);
        return new Vector2Int(x, y);
    }
    
    public PropView GetPropAtWorldPosition(Vector2 worldPos)
    {
        Vector2Int gridPos = GetGridPositionFromWorld(worldPos);
        return GetPropAt(gridPos.x, gridPos.y);
    }
}