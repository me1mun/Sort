using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GroupManagerWindow : EditorWindow
{
    private const string ITEMS_DATA_FOLDER = "Assets/GameData/Items";
    private const string GROUPS_FOLDER = "Assets/GameData/Groups";
    private const int DESIRED_ITEM_COUNT = 4;
    private const float ITEM_BLOCK_WIDTH = 64f; 
    private const float ITEM_BLOCK_HEIGHT = 50f;

    private List<GroupData> _allGroups = new List<GroupData>();
    private List<ItemData> _allItems = new List<ItemData>();
    private Dictionary<GroupData, bool> _groupFoldouts = new Dictionary<GroupData, bool>();
    
    private Vector2 _scrollPosition;
    private Vector2 _groupScrollPosition;
    private Vector2 _itemPoolScrollPosition;
    
    private string _groupSearch = "";
    private string _itemSearch = "";
    private string _newGroupName = "";

    [MenuItem("Game Tools/Group Manager")]
    public static void ShowWindow()
    {
        GetWindow<GroupManagerWindow>("Group Manager").minSize = new Vector2(400, 600);
    }

    private void OnEnable() => ReloadAllData();
    
    private void ReloadAllData()
    {
        _allGroups = LoadAssets<GroupData>(new[] { GROUPS_FOLDER }).OrderBy(g => g.name).ToList();
        _allItems = LoadAssets<ItemData>(new[] { ITEMS_DATA_FOLDER }).OrderBy(i => i.name).ToList();
        
        foreach (var group in _allGroups.Where(group => !_groupFoldouts.ContainsKey(group)))
        {
            _groupFoldouts[group] = false;
        }
    }

    private List<T> LoadAssets<T>(string[] searchInFolders) where T : Object =>
        AssetDatabase.FindAssets($"t:{typeof(T).Name}", searchInFolders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(a => a != null)
            .ToList();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Group & Item Manager", new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        EditorGUILayout.Space(10);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
        DrawSection("1. Group Manager & Creator", DrawGroupManager, GUILayout.ExpandHeight(true)); 
        DrawSection("2. Item Pool (Search to view)", DrawItemPool, GUILayout.MinHeight(250)); 

        EditorGUILayout.EndScrollView();

        if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform) Repaint();
        if (GUI.changed) AssetDatabase.SaveAssets();
    }

    private void DrawSection(string title, System.Action content, params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, options);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Separator();
        content?.Invoke();
        EditorGUILayout.Space(5);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }
    
    private void DrawSearchToolbar(ref string searchString)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchString = EditorGUILayout.TextField(GUIContent.none, searchString, EditorStyles.toolbarSearchField);
        if (GUILayout.Button(new GUIContent("X", "Clear Search"), EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            searchString = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawGroupManager()
    {
        DrawSearchToolbar(ref _groupSearch);

        if (!_allGroups.Any() && string.IsNullOrEmpty(_groupSearch))
        {
            EditorGUILayout.LabelField("No groups found. Create one below.");
        }

        _groupScrollPosition = EditorGUILayout.BeginScrollView(_groupScrollPosition, "box", GUILayout.ExpandHeight(true));
        
        GroupData groupToRemove = null;
        string s = _groupSearch.ToLower();

        foreach (var group in _allGroups.Where(g => g != null && 
            (string.IsNullOrEmpty(s) || g.name.ToLower().Contains(s) || (g.groupKey != null && g.groupKey.ToLower().Contains(s)))))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            bool fold = _groupFoldouts.GetValueOrDefault(group, false);

            Color originalBgColor = GUI.backgroundColor;
            if (group.items.Count(i => i != null) != DESIRED_ITEM_COUNT)
            {
                GUI.backgroundColor = Color.yellow; 
            }

            string headerContent = GetNameWithoutPrefix(group.name, "group_");

            fold = EditorGUILayout.Foldout(fold, new GUIContent(headerContent), true, EditorStyles.foldoutHeader);
            _groupFoldouts[group] = fold;
            
            GUI.backgroundColor = originalBgColor;

            if (GUILayout.Button(new GUIContent("🗑️", "Delete Group"), EditorStyles.miniButton, GUILayout.Width(35), GUILayout.Height(22)) &&
                EditorUtility.DisplayDialog("Confirm Deletion", $"Are you sure you want to delete group '{group.name}'?", "Yes", "No"))
            {
                groupToRemove = group;
            }
            
            EditorGUILayout.EndHorizontal();

            if (_groupFoldouts[group])
            {
                EditorGUI.indentLevel++;
                
                string newGroupKey = EditorGUILayout.TextField("Group Key:", group.groupKey);
                if (newGroupKey != group.groupKey)
                {
                    Undo.RecordObject(group, "Rename Group Key");
                    group.groupKey = newGroupKey;
                    EditorUtility.SetDirty(group);
                }
                
                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField("Items:", EditorStyles.miniBoldLabel);
                DrawGroupItemsInRow(group);
                
                CheckForIntersections(group);
            }
            
            EditorGUILayout.EndVertical();

            HandleGroupDrop(GUILayoutUtility.GetLastRect(), group); 
        }

        EditorGUILayout.Space(10);
        DrawNewGroupCreationArea();
        
        EditorGUILayout.EndScrollView();

        if (groupToRemove != null)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(groupToRemove));
            ReloadAllData();
        }
    }
    
    private void DrawGroupItemsInRow(GroupData group)
    {
        // 2. Window with group items has minimum height
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(80)); 
        
        List<ItemData> itemsToRemove = new List<ItemData>();
        
        // 3. Wrapping logic
        float currentX = 0;
        const float SPACING = 5f; 
        const float ROW_SPACING = 8f; // Вертикальный отступ между строками

        EditorGUILayout.BeginHorizontal(); 
        GUILayout.FlexibleSpace(); 

        float viewWidth = EditorGUIUtility.currentViewWidth - 60; 
        int itemsInCurrentRow = 0;
        
        for (int i = 0; i < group.items.Count; i++)
        {
            var item = group.items[i];
            if (item == null) continue;
            
            float requiredWidth = ITEM_BLOCK_WIDTH + SPACING; 
            
            if (currentX + requiredWidth > viewWidth && itemsInCurrentRow > 0)
            {
                GUILayout.FlexibleSpace(); 
                EditorGUILayout.EndHorizontal();
                
                // Добавление вертикального отступа между строками
                GUILayout.Space(ROW_SPACING); 
                
                EditorGUILayout.BeginHorizontal(); 
                GUILayout.FlexibleSpace(); 
                currentX = 0; 
                itemsInCurrentRow = 0;
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(ITEM_BLOCK_WIDTH));
            DrawItemPreview(item, ITEM_BLOCK_WIDTH, ITEM_BLOCK_HEIGHT);

            if (GUILayout.Button("➖", EditorStyles.miniButton, GUILayout.Height(15)))
            {
                itemsToRemove.Add(item);
            }
            EditorGUILayout.EndVertical();
            
            currentX += requiredWidth;
            itemsInCurrentRow++;
            
            if (i < group.items.Count - 1 && currentX + requiredWidth <= viewWidth)
            {
                 GUILayout.Space(SPACING);
            }
        }
        
        GUILayout.FlexibleSpace(); 
        EditorGUILayout.EndHorizontal(); 
        EditorGUILayout.EndVertical(); 
        
        if (itemsToRemove.Any())
        {
            Undo.RecordObject(group, "Remove Item from Group");
            foreach (var item in itemsToRemove) group.items.Remove(item);
            EditorUtility.SetDirty(group);
        }
    }
    
    private void DrawItemPreview(ItemData item, float width, float height)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));

        Rect rect = GUILayoutUtility.GetRect(width, height);
        
        Rect iconRect = new Rect(rect.x + (width - 32) / 2, rect.y, 32f, 32f); 
        
        if (item.icon != null && item.icon.texture != null)
        {
            Texture2D tex = item.icon.texture;
            Rect texRect = item.icon.textureRect;
            Rect texCoords = new Rect(texRect.x / tex.width, texRect.y / tex.height, texRect.width / tex.width, texRect.height / tex.height);
            
            float aspect = texRect.width / texRect.height;
            float drawW = 32f;
            float drawH = 32f;
            if (aspect > 1f) drawH = 32f / aspect; else drawW = 32f * aspect;
            float offsetX = (32f - drawW) / 2f;
            float offsetY = (32f - drawH) / 2f;
            
            GUI.DrawTextureWithTexCoords(new Rect(iconRect.x + offsetX, iconRect.y + offsetY, drawW, drawH), tex, texCoords);
        }
        else
        {
            EditorGUI.LabelField(iconRect, "❓", new GUIStyle(EditorStyles.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter });
        }
        
        string itemName = GetNameWithoutPrefix(item.name, "Item_");
        Rect labelRect = new Rect(rect.x, rect.y + 32, width, 18);
        EditorGUI.LabelField(labelRect, itemName, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true });
        
        Event currentEvent = Event.current;
        if (rect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDrag)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new Object[] { item };
            DragAndDrop.StartDrag($"Dragging {item.name}");
            currentEvent.Use();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void HandleGroupDrop(Rect dropRect, GroupData targetGroup)
    {
        Event currentEvent = Event.current;
        if (!dropRect.Contains(currentEvent.mousePosition)) return;
        
        if (currentEvent.type == EventType.DragUpdated)
        {
            if (DragAndDrop.objectReferences.Any(obj => obj is ItemData))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                currentEvent.Use();
            }
        }
        else if (currentEvent.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            bool wasModified = false;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is ItemData draggedItem && !targetGroup.items.Contains(draggedItem))
                {
                    Undo.RecordObject(targetGroup, "Add Item to Group");
                    targetGroup.items.Add(draggedItem);
                    wasModified = true;
                }
            }
            if (wasModified)
            {
                EditorUtility.SetDirty(targetGroup);
                ReloadAllData(); 
            }
            currentEvent.Use();
        }
    }

    private void DrawItemPool()
    {
        DrawSearchToolbar(ref _itemSearch);

        if (string.IsNullOrEmpty(_itemSearch))
        {
            EditorGUILayout.HelpBox("Введите название предмета в поле поиска выше, чтобы отобразить пул для перетаскивания.", MessageType.Info);
            return;
        }

        _itemPoolScrollPosition = EditorGUILayout.BeginScrollView(_itemPoolScrollPosition, "box", GUILayout.ExpandHeight(true));

        string s = _itemSearch.ToLower();
        var filteredItems = _allItems.Where(item => item != null && 
                                                    (item.name.ToLower().Contains(s) || 
                                                     (item.itemKey != null && item.itemKey.ToLower().Contains(s)))).ToList();

        if (!filteredItems.Any())
        {
            EditorGUILayout.LabelField("No items found matching criteria.");
        }
        else
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            foreach (var item in filteredItems)
            {
                DrawItemPreview(item, ITEM_BLOCK_WIDTH, ITEM_BLOCK_HEIGHT); 
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
    
    private void DrawNewGroupCreationArea()
    {
        EditorGUILayout.LabelField("Create New Group", EditorStyles.boldLabel);
        
        _newGroupName = EditorGUILayout.TextField("New Group Key", _newGroupName);
        EditorGUILayout.Space(5);
        
        bool canCreate = !string.IsNullOrEmpty(_newGroupName) && 
                         !_allGroups.Any(g => g.groupKey == _newGroupName || g.name == $"group_{_newGroupName}");

        DrawGenerateButton("Create Group Asset (Empty)", canCreate, CreateGroupAsset);
    }

    private void DrawGenerateButton(string text, bool enabled, System.Action onClick)
    {
        GUI.enabled = enabled;
        if (GUILayout.Button(text, GUILayout.Height(30))) onClick();
        GUI.enabled = true;
    }
    
    private string GetNameWithoutPrefix(string name, string prefix) =>
        name.StartsWith(prefix) ? name.Substring(prefix.Length) : name;

    private void CreateGroupAsset()
    {
        EnsureFolderExists(GROUPS_FOLDER);
        // Предполагается, что GroupData является ScriptableObject
        var newGroup = CreateInstance<GroupData>(); 
        newGroup.groupKey = _newGroupName;

        string path = Path.Combine(GROUPS_FOLDER, $"group_{_newGroupName}.asset");
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(newGroup, uniquePath);
        
        // Финализация
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newGroup;
        Debug.Log($"Создана пустая группа '{_newGroupName}'.");

        _newGroupName = "";
        ReloadAllData();
    }

    private void CheckForIntersections(GroupData group)
    {
        if (group.items.Any(i => i == null))
        {
            EditorGUILayout.HelpBox("Группа содержит пустые (null) ссылки на предметы.", MessageType.Warning);
        }
    }

    private void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}