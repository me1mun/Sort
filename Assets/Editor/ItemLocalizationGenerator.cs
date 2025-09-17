using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class ItemLocalizationGenerator
{
    private const string OUTPUT_PATH = "Assets/Resources/Localization/en_items.json";

    [MenuItem("My Tools/Localization/Generate Item Localization File (English)")]
    private static void GenerateItemLocalization()
    {
        // 1. Загружаем существующие данные из JSON файла, если он есть
        LocalizationData localizationData;
        HashSet<string> existingKeys = new HashSet<string>();

        if (File.Exists(OUTPUT_PATH))
        {
            string currentJson = File.ReadAllText(OUTPUT_PATH);
            localizationData = JsonUtility.FromJson<LocalizationData>(currentJson);
            if (localizationData.items == null) localizationData.items = new List<LocalizationItem>();
            
            // Запоминаем все существующие ключи для быстрой проверки
            foreach(var item in localizationData.items)
            {
                existingKeys.Add(item.key);
            }
        }
        else
        {
            localizationData = new LocalizationData { items = new List<LocalizationItem>() };
        }
        
        // 2. Находим все ассеты ItemData в проекте
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        int newKeysAdded = 0;

        // 3. Проходим по ассетам и добавляем только те, которых нет в файле
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (itemData == null || string.IsNullOrEmpty(itemData.itemKey)) continue;

            // Если ключ НЕ существует, добавляем его
            if (!existingKeys.Contains(itemData.itemKey))
            {
                localizationData.items.Add(new LocalizationItem
                {
                    key = itemData.itemKey,
                    value = itemData.itemKey 
                });
                existingKeys.Add(itemData.itemKey); // Добавляем в сет, чтобы избежать дубликатов в одной сессии
                newKeysAdded++;
            }
        }
        
        // 4. Сохраняем файл, только если были добавлены новые ключи
        if (newKeysAdded > 0)
        {
            // Сортируем список по ключу для порядка в файле
            localizationData.items = localizationData.items.OrderBy(item => item.key).ToList();

            string json = JsonUtility.ToJson(localizationData, true);
            File.WriteAllText(OUTPUT_PATH, json);
            AssetDatabase.Refresh();
            Debug.Log($"Готово! Файл локализации '{OUTPUT_PATH}' обновлен. Добавлено {newKeysAdded} новых ключей.");
        }
        else
        {
            Debug.Log($"Новых ключей не найдено. Файл '{OUTPUT_PATH}' не был изменен.");
        }
    }
}