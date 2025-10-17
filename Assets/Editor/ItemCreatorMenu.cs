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

            // Загружаем спрайт
            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (iconSprite == null) continue;

            try
            {
                var newItem = ScriptableObject.CreateInstance("ItemData"); 

                AssetDatabase.CreateAsset(newItem, itemAssetPath);
                createdCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create ItemData for {filename}. Ensure ItemData ScriptableObject class is defined and accessible: {e.Message}");
            }
        }
        
        FinalizeAssetCreation($"Создано: {createdCount} новых предметов.");
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