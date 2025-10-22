using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class GridSpawner
{
    private class ItemSpawnInfo
    {
        public GroupData Group { get; }
        public ItemData Item { get; }
        public ItemSpawnInfo(GroupData g, ItemData i) { Group = g; Item = i; }
    }
    
    private const int MaxGridHeight = 5;

    private readonly Queue<ItemSpawnInfo> _spawnQueue;
    private readonly PropPool _propPool;
    private readonly Grid _grid;

    public GridSpawner(LevelData levelData, int gridWidth, PropPool propPool, Grid grid)
    {
        _propPool = propPool;
        _grid = grid;
        _spawnQueue = GenerateSpawnQueue(levelData, grid.Width);
    }
    
    public bool HasItemsToSpawn() => _spawnQueue.Any();

    public PropView CreatePropAt(int x, int y, bool spawnAtTop = false)
    {
        if (!HasItemsToSpawn()) return null;
        
        var itemInfo = _spawnQueue.Dequeue();
        Vector3 startPosition = spawnAtTop 
            ? _grid.GetWorldPosition(x, _grid.Height)
            : _grid.GetWorldPosition(x, y);

        PropView prop = _propPool.Get();
        prop.transform.position = startPosition;
        prop.Initialize(itemInfo.Group, itemInfo.Item, new Vector2Int(x, y));
        prop.OnSpawn();
        
        _grid.SetPropAt(x, y, prop);
        return prop;
    }
    
    public List<PropView> RefillGrid()
    {
        var newProps = new List<PropView>();
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                if (_grid.GetPropAt(x, y) == null && HasItemsToSpawn())
                {
                    var newProp = CreatePropAt(x, y, true);
                    if(newProp != null) newProps.Add(newProp);
                }
            }
        }
        return newProps;
    }

    private Queue<ItemSpawnInfo> GenerateSpawnQueue(LevelData levelData, int gridWidth)
    {
        if (!levelData.requiredGroups.Any() || gridWidth <= 0)
        {
            return new Queue<ItemSpawnInfo>();
        }

        var masterItemPool = new List<ItemSpawnInfo>();
        foreach (var group in levelData.requiredGroups)
        {
            if (!group.items.Any()) continue;
            var normalizedItems = new List<ItemData>(group.items);

            if (normalizedItems.Count < gridWidth)
            {
                int originalCount = group.items.Count;
                while (normalizedItems.Count < gridWidth)
                {
                    normalizedItems.Add(group.items[normalizedItems.Count % originalCount]);
                }
            }
            else if (normalizedItems.Count > gridWidth)
            {
                normalizedItems = normalizedItems.OrderBy(x => Random.value).Take(gridWidth).ToList();
            }

            foreach (var item in normalizedItems)
            {
                masterItemPool.Add(new ItemSpawnInfo(group, item));
            }
        }

        List<ItemSpawnInfo> finalOrderedList;
        const int maxAttempts = 20; 
        int attempts = 0;

        int gridHeight = Mathf.Min(levelData.requiredGroups.Count, MaxGridHeight);

        do
        {
            if (levelData.requiredGroups.Count < 2)
            {
                finalOrderedList = new List<ItemSpawnInfo>(masterItemPool.OrderBy(x => Random.value));
            }
            else
            {
                var itemsForStartingField = new List<ItemSpawnInfo>();
                var remainingItems = new List<ItemSpawnInfo>(masterItemPool);
        
                var shuffledGroups = levelData.requiredGroups.OrderBy(x => Random.value).ToList();
                var guaranteedGroups = shuffledGroups.Take(2).ToList();
                var otherGroups = shuffledGroups.Skip(2).ToList();

                foreach (var group in guaranteedGroups)
                {
                    var itemsOfGroup = remainingItems.Where(info => info.Group == group).ToList();
                    itemsForStartingField.AddRange(itemsOfGroup);
                    remainingItems.RemoveAll(info => info.Group == group);
                }

                foreach (var group in otherGroups)
                {
                    var representative = remainingItems.FirstOrDefault(info => info.Group == group);
                    if (representative != null)
                    {
                        itemsForStartingField.Add(representative);
                        remainingItems.Remove(representative);
                    }
                }
        
                finalOrderedList = new List<ItemSpawnInfo>();
                finalOrderedList.AddRange(itemsForStartingField.OrderBy(x => Random.value));
                finalOrderedList.AddRange(remainingItems.OrderBy(x => Random.value));
            }

            if (++attempts > maxAttempts)
            {
                Debug.LogWarning("Could not generate a valid grid without a pre-completed row. Proceeding anyway.");
                break;
            }
            
        } while (HasPrecompletedRow(finalOrderedList, gridWidth, gridHeight));
        
        return new Queue<ItemSpawnInfo>(finalOrderedList);
    }

    private bool HasPrecompletedRow(List<ItemSpawnInfo> items, int gridWidth, int gridHeight)
    {
        int initialItemCount = gridWidth * gridHeight;
        if (items.Count < initialItemCount)
        {
            return false;
        }

        for (int y = 0; y < gridHeight; y++)
        {
            int rowIndex = y * gridWidth;
            
            var firstItemGroup = items[rowIndex].Group;
            bool allSameGroup = true;
            for (int x = 1; x < gridWidth; x++)
            {
                if (items[rowIndex + x].Group != firstItemGroup)
                {
                    allSameGroup = false;
                    break;
                }
            }

            if (allSameGroup)
            {
                return true;
            }
        }
        
        return false;
    }
}