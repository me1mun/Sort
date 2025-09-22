using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

public static class LevelCreationTool
{
    private const string LevelsFolderPath = "Assets/GameData/Levels";

    [MenuItem("Assets/Create/Game/Level From Selected Groups")]
    private static void CreateLevelFromGroups()
    {
        var selectedGroups = Selection.GetFiltered<GroupData>(SelectionMode.Assets);

        var newLevel = ScriptableObject.CreateInstance<LevelData>();
        newLevel.requiredGroups.AddRange(selectedGroups.OrderBy(g => g.name));

        if (!Directory.Exists(LevelsFolderPath))
        {
            Directory.CreateDirectory(LevelsFolderPath);
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(LevelsFolderPath, "NewLevel.asset"));
        
        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newLevel;
    }

    [MenuItem("Assets/Create/Game/Level From Selected Groups", true)]
    private static bool ValidateCreateLevelFromGroups()
    {
        return Selection.GetFiltered<GroupData>(SelectionMode.Assets).Length > 0;
    }
}