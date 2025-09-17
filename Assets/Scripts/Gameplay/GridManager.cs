using System.Collections.Generic;
using UnityEngine;

public class GridManager
{
    private readonly PropView[,] _grid;
    private readonly int _width;
    private readonly int _height;

    public GridManager(int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new PropView[width, height];
    }

    public PropView GetPropAt(int x, int y)
    {
        if (!IsWithinBounds(x, y)) return null;
        return _grid[x, y];
    }

    public void SetPropAt(int x, int y, PropView prop)
    {
        if (!IsWithinBounds(x, y)) return;
        _grid[x, y] = prop;
    }

    public void ClearCell(int x, int y)
    {
        if (!IsWithinBounds(x, y)) return;
        _grid[x, y] = null;
    }

    public void SwapProps(Vector2Int posA, Vector2Int posB)
    {
        PropView propA = _grid[posA.x, posA.y];
        PropView propB = _grid[posB.x, posB.y];
        
        _grid[posA.x, posA.y] = propB;
        _grid[posB.x, posB.y] = propA;

        if (propA != null) propA.GridPosition = posB;
        if (propB != null) propB.GridPosition = posA;
    }
    
    public List<int> GetCompletedRows()
    {
        var completedRows = new List<int>();
        for (int y = 0; y < _height; y++)
        {
            if (IsRowComplete(y))
            {
                completedRows.Add(y);
            }
        }
        return completedRows;
    }

    private bool IsRowComplete(int y)
    {
        PropView firstProp = GetPropAt(0, y);
        if (firstProp == null) return false;
            
        string firstGroupKey = firstProp.AssignedGroup.groupKey;
        for (int x = 1; x < _width; x++)
        {
            PropView currentProp = GetPropAt(x, y);
            if (currentProp == null || currentProp.AssignedGroup.groupKey != firstGroupKey)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public bool IsGridEmpty()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_grid[x, y] != null) return false;
            }
        }
        return true;
    }
    
    public bool IsGridIdle()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_grid[x, y] != null && _grid[x, y].IsAnimating) return false;
            }
        }
        return true;
    }
}