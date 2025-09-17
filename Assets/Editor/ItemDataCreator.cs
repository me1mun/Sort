using UnityEngine;
using UnityEditor;
using System.IO;

public static class ItemDataCreator
{
    private const string OUTPUT_FOLDER_PATH = "Assets/GameData/Items";

    // ИЗМЕНЕНИЕ: Добавили подменю "Creation"
    [MenuItem("My Tools/Creation/Create ItemData from Selected Icons")]
    private static void CreateItemsFromIcons()
    {
        // ... остальной код без изменений ...
        if (!Directory.Exists(OUTPUT_FOLDER_PATH))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER_PATH);
        }

        var selectedTextures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        if (selectedTextures.Length == 0)
        {
            Debug.LogWarning("Не выбрано ни одного изображения (Texture2D). Выделите иконки в окне проекта.");
            return;
        }
        
        EditorUtility.DisplayProgressBar("Creating Items", "Processing icons...", 0f);

        int createdCount = 0;
        for (int i = 0; i < selectedTextures.Length; i++)
        {
            Texture2D texture = selectedTextures[i];
            string texturePath = AssetDatabase.GetAssetPath(texture);

            EditorUtility.DisplayProgressBar("Creating Items", $"Processing {texture.name}...", (float)i / selectedTextures.Length);
            
            string filename = Path.GetFileNameWithoutExtension(texturePath);
            if (!filename.StartsWith("icon_"))
            {
                Debug.LogWarning($"Файл '{filename}' пропущен, так как его имя не начинается с 'icon_'.");
                continue;
            }

            string itemName = filename.Substring("icon_".Length);
            
            string itemKey = itemName; 
            string assetName = "Item_" + itemName;
            string assetPath = Path.Combine(OUTPUT_FOLDER_PATH, $"{assetName}.asset");

            if (File.Exists(assetPath))
            {
                Debug.Log($"Ассет '{assetName}' уже существует. Пропускаем.");
                continue;
            }

            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (iconSprite == null)
            {
                Debug.LogWarning($"Не удалось загрузить спрайт из '{texturePath}'. Убедитесь, что тип текстуры 'Sprite (2D and UI)'.");
                continue;
            }

            ItemData newItemData = ScriptableObject.CreateInstance<ItemData>();
            newItemData.itemKey = itemKey;
            newItemData.icon = iconSprite;

            AssetDatabase.CreateAsset(newItemData, assetPath);
            createdCount++;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Готово! Создано {createdCount} новых ассетов ItemData в папке '{OUTPUT_FOLDER_PATH}'.");
    }
}