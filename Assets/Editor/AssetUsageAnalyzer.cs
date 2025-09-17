using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class AssetUsageAnalyzer
{
    [MenuItem("My Tools/Analysis/Count Item Usage in Groups")]
    private static void CountItemUsage()
    {
        // 1. Находим и загружаем все ассеты Items и Groups
        var allItems = LoadAllAssets<ItemData>();
        var allGroups = LoadAllAssets<GroupData>();

        if (allItems.Count == 0 || allGroups.Count == 0)
        {
            Debug.LogWarning("Не найдены ассеты ItemData или GroupData для анализа.");
            return;
        }

        // 2. Создаем словарь для подсчета использований
        Dictionary<ItemData, int> usageCount = new Dictionary<ItemData, int>();
        foreach (var item in allItems)
        {
            usageCount[item] = 0;
        }

        // 3. Проходим по всем группам и считаем предметы
        foreach (var group in allGroups)
        {
            if (group.items == null) continue;
            foreach (var item in group.items)
            {
                if (item != null && usageCount.ContainsKey(item))
                {
                    usageCount[item]++;
                }
            }
        }

        // 4. Формируем красивый отчет для вывода в консоль
        var sortedUsage = usageCount.OrderByDescending(pair => pair.Value).ToList();
        
        StringBuilder report = new StringBuilder();
        report.AppendLine("--- Отчет об использовании предметов в группах ---");
        report.AppendLine($"Всего проанализировано: {allItems.Count} предметов и {allGroups.Count} групп.");
        report.AppendLine();

        foreach (var pair in sortedUsage)
        {
            report.AppendLine($"'{pair.Key.name}' используется в {pair.Value} группах.");
        }
        
        // Дополнительно выводим предметы, которые не используются ни разу
        var unusedItems = usageCount.Where(pair => pair.Value == 0).ToList();
        if (unusedItems.Count > 0)
        {
            report.AppendLine("\n--- ⚠️ Неиспользуемые предметы ---");
            foreach (var pair in unusedItems)
            {
                report.AppendLine(pair.Key.name);
            }
        }

        Debug.Log(report.ToString());
    }

    // Вспомогательный метод для загрузки всех ассетов указанного типа
    private static List<T> LoadAllAssets<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        List<T> assets = new List<T>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            assets.Add(AssetDatabase.LoadAssetAtPath<T>(path));
        }
        return assets;
    }
}