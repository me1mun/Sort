// File: ItemCreatorMenu.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ItemCreatorMenu
{
    private const string ITEMS_ART_FOLDER = "Assets/Art/Items";
    private const string ITEMS_DATA_FOLDER = "Assets/GameData/Items";

    [MenuItem("Game Tools/Item Creator/Create Missing Items from Icons", false, 10)]
    public static void CreateMissingItemsFromIcons()
    {
        ProcessItemIconsFromFolder();
    }

    private static void ProcessItemIconsFromFolder()
    {
        EnsureFolderExists(ITEMS_DATA_FOLDER);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ITEMS_ART_FOLDER });
        int createdCount = 0;
        
        foreach (string guid in guids)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(guid);
            string filename = Path.GetFileNameWithoutExtension(texturePath);
            if (!filename.StartsWith("icon_")) continue;

            string itemName = filename.Substring("icon_".Length);
            string itemAssetPath = Path.Combine(ITEMS_DATA_FOLDER, $"Item_{itemName}.asset");
            
            // Если ассет уже существует, пропускаем
            if (File.Exists(itemAssetPath)) continue; 

            // Загружаем все ассеты из файла текстуры, чтобы найти спрайт.
            // Это важно, если текстура имеет тип Texture Type: Sprite (2D and UI).
            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            
            // Если не удалось загрузить как спрайт напрямую, ищем все вложенные объекты.
            if (iconSprite == null)
            {
                // Загружаем все объекты из файла и ищем первый Sprite
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
                foreach (Object asset in assets)
                {
                    if (asset is Sprite sprite)
                    {
                        iconSprite = sprite;
                        break;
                    }
                }
            }

            if (iconSprite == null)
            {
                Debug.LogWarning($"Skipping {filename}: Could not find a valid Sprite asset at path {texturePath}. Ensure Texture Type is set to 'Sprite (2D and UI)'.");
                continue;
            }

            try
            {
                // Инициализируем ItemData явно
                ItemData newItem = ScriptableObject.CreateInstance<ItemData>(); 
                
                // --- УСТАНОВКА ПОЛЕЙ ---
                // Устанавливаем itemKey: имя иконки без префикса "icon_"
                newItem.itemKey = itemName; 
                
                // Устанавливаем icon: загруженный спрайт
                newItem.icon = iconSprite;
                // --- КОНЕЦ УСТАНОВКИ ПОЛЕЙ ---

                AssetDatabase.CreateAsset(newItem, itemAssetPath);
                createdCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create ItemData for {filename}. Ensure ItemData ScriptableObject class is defined and accessible: {e.Message}");
            }
        }
        
        FinalizeAssetCreation($"Создано: {createdCount} новых предметов. ({guids.Length} текстур проверено)");
    }

    private static void FinalizeAssetCreation(string logMessage)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Debug.Log(logMessage);
    }
    
    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}