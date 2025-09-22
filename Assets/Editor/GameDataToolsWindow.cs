using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class GameDataToolsWindow : EditorWindow
{
    private const string ROOT_FOLDER = "Assets/GameData";
    private const string ITEMS_FOLDER = "Assets/GameData/Items";
    private const string GROUPS_FOLDER = "Assets/GameData/Groups";
    private const string LEVELS_FOLDER = "Assets/GameData/Levels";

    private List<Texture2D> _texturesForItems = new List<Texture2D>();
    private List<ItemData> _itemsForGroup = new List<ItemData>();
    private List<GroupData> _groupsForLevel = new List<GroupData>();

    private string _groupName = "";
    private Vector2 _scrollPosition;

    [MenuItem("Game Tools/Data Creator Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<GameDataToolsWindow>("Data Creator");
        window.minSize = new Vector2(350, 400);
    }

    private void OnEnable()
    {
        // OnEnable теперь пуст, так как мы не зависим от выделения
    }
    
    private void OnDisable()
    {
        // OnDisable теперь пуст
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Game Data Creator", new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        EditorGUILayout.Space(10);
        
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
        DrawSection("1. Item Creator", DrawItemsContent);
        DrawSection("2. Group Creator", DrawGroupsContent);
        DrawSection("3. Level Creator", DrawLevelsContent);

        EditorGUILayout.EndScrollView();
        
        // Постоянно перерисовываем окно, если идет процесс перетаскивания
        if (DragAndDrop.objectReferences.Length > 0)
        {
            Repaint();
        }
    }
    
    #region GUI Sections

    private void DrawSection(string title, System.Action content)
    {
        EditorGUILayout.BeginVertical("HelpBox");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Separator();
        content?.Invoke();
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawItemsContent()
    {
        DrawDropArea<Texture2D>("Drag Textures (with 'icon_' prefix) Here", _texturesForItems, FilterAndAddTextures);
        DrawAssetList(_texturesForItems, () => _texturesForItems.Clear());
        DrawGenerateButton("Generate Items", _texturesForItems.Count > 0, CreateItemsFromTextures);
    }

    private void DrawGroupsContent()
    {
        DrawDropArea<ItemData>("Drag ItemData Assets Here", _itemsForGroup);
        DrawAssetList(_itemsForGroup, () => _itemsForGroup.Clear());

        _groupName = EditorGUILayout.TextField("Group Name / Key", _groupName);
        EditorGUILayout.Space(5);
        DrawGenerateButton("Generate Group", _itemsForGroup.Count > 0 && !string.IsNullOrEmpty(_groupName), CreateGroupAsset);
    }

    private void DrawLevelsContent()
    {
        DrawDropArea<GroupData>("Drag GroupData Assets Here", _groupsForLevel);
        DrawAssetList(_groupsForLevel, () => _groupsForLevel.Clear());
        DrawGenerateButton("Generate Level", _groupsForLevel.Count > 0, CreateLevelFromGroups);
    }

    #endregion

    #region Drag & Drop and UI Helpers

    private void DrawDropArea<T>(string text, List<T> targetList, System.Action<IEnumerable<T>> customAddAction = null) where T : Object
    {
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        
        Color originalColor = GUI.backgroundColor;
        bool isDraggingValidAsset = DragAndDrop.objectReferences.Any(obj => obj is T);

        if (isDraggingValidAsset)
        {
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // Green for potential target
        }

        GUI.Box(dropArea, text, new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic });
        GUI.backgroundColor = originalColor;

        Event currentEvent = Event.current;
        if (!dropArea.Contains(currentEvent.mousePosition)) return;

        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                var draggedObjects = DragAndDrop.objectReferences.OfType<T>();
                
                if (customAddAction != null) customAddAction(draggedObjects);
                else targetList.AddRange(draggedObjects.Where(item => !targetList.Contains(item)));
            }
            currentEvent.Use();
        }
    }

    private void DrawAssetList<T>(List<T> assetList, System.Action onClear) where T : Object
    {
        if (assetList.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Assets to process:", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Clear", GUILayout.Width(50))) onClear();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            for (int i = 0; i < assetList.Count; i++)
            {
                assetList[i] = (T)EditorGUILayout.ObjectField(assetList[i], typeof(T), false);
            }
            assetList.RemoveAll(item => item == null);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
    }
    
    private void DrawGenerateButton(string text, bool enabled, System.Action onClick)
    {
        Color originalColor = GUI.backgroundColor;
        GUI.enabled = enabled;
        GUI.backgroundColor = enabled ? new Color(0.7f, 1f, 0.7f) : originalColor;

        if (GUILayout.Button(text, GUILayout.Height(30)))
        {
            onClick();
        }
        
        GUI.backgroundColor = originalColor;
        GUI.enabled = true;
    }

    private void FilterAndAddTextures(IEnumerable<Texture2D> textures)
    {
        foreach (var texture in textures)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            string filename = Path.GetFileNameWithoutExtension(path);

            if (!filename.StartsWith("icon_")) continue;
            
            string itemName = filename.Substring("icon_".Length);
            string assetPath = Path.Combine(ITEMS_FOLDER, $"Item_{itemName}.asset");

            if (File.Exists(assetPath) || _texturesForItems.Contains(texture)) continue;
            
            _texturesForItems.Add(texture);
        }
    }
    
    #endregion
    
    #region Asset Creation Logic
    private void CreateItemsFromTextures()
    {
        EnsureFolderExists(ITEMS_FOLDER);
        int createdCount = 0;
        foreach (var texture in _texturesForItems)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            string filename = Path.GetFileNameWithoutExtension(path);
            string itemName = filename.Substring("icon_".Length);
            string assetPath = Path.Combine(ITEMS_FOLDER, $"Item_{itemName}.asset");
            
            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (iconSprite == null) continue;

            ItemData newItem = CreateInstance<ItemData>();
            newItem.itemKey = itemName;
            newItem.icon = iconSprite;
            AssetDatabase.CreateAsset(newItem, assetPath);
            createdCount++;
        }
        FinalizeAssetCreation($"Created {createdCount} new ItemData assets.");
        _texturesForItems.Clear();
    }
    private void CreateGroupAsset()
    {
        EnsureFolderExists(GROUPS_FOLDER);
        var newGroup = CreateInstance<GroupData>();
        newGroup.groupKey = _groupName;
        newGroup.items.AddRange(_itemsForGroup.OrderBy(i => i.name));
        
        string path = Path.Combine(GROUPS_FOLDER, $"{_groupName}.asset");
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
        
        AssetDatabase.CreateAsset(newGroup, uniquePath);
        FinalizeAssetCreation($"Created group '{_groupName}'.", newGroup);
        _groupName = "";
        _itemsForGroup.Clear();
    }
    private void CreateLevelFromGroups()
    {
        EnsureFolderExists(LEVELS_FOLDER);
        int nextLevelNumber = GetNextLevelNumber();
        
        var newLevel = CreateInstance<LevelData>();
        newLevel.requiredGroups.AddRange(_groupsForLevel.OrderBy(g => g.name));

        string path = Path.Combine(LEVELS_FOLDER, $"Level_{nextLevelNumber}.asset");
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(newLevel, uniquePath);
        FinalizeAssetCreation($"Created level 'Level_{nextLevelNumber}'.", newLevel);
        _groupsForLevel.Clear();
    }
    
    #endregion
    
    #region Helpers
    private int GetNextLevelNumber()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LEVELS_FOLDER });
        int maxNumber = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = Path.GetFileNameWithoutExtension(path);
            Match match = Regex.Match(filename, @"Level_(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                if (number > maxNumber) maxNumber = number;
            }
        }
        return maxNumber + 1;
    }
    private void FinalizeAssetCreation(string logMessage, Object newAsset = null)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        if (newAsset != null) Selection.activeObject = newAsset;
        Debug.Log(logMessage);
    }
    private void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
    #endregion
}