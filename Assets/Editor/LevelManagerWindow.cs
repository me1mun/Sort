using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class LevelManagerWindow : EditorWindow
{
    private const string LEVELS_FOLDER = "Assets/GameData/Levels";
    private const string GROUPS_FOLDER = "Assets/GameData/Groups";
    private List<LevelData> _allLevels = new List<LevelData>();
    private List<GroupData> _allGroups = new List<GroupData>();
    private List<GroupData> _unusedGroups = new List<GroupData>();

    private Dictionary<LevelData, bool> _levelFoldouts = new Dictionary<LevelData, bool>();
    private Dictionary<LevelData, bool> _levelFilters = new Dictionary<LevelData, bool>();
    private Vector2 _levelsScrollPos;
    private Vector2 _groupsScrollPos;

    private string _groupSearch = "";

    [MenuItem("Game Tools/Level Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelManagerWindow>("Level Manager");
        window.minSize = new Vector2(900, 500);
    }

    private void OnEnable()
    {
        ReloadAllData();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Level & Group Manager", new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.60f));
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);
        if (GUILayout.Button("Reset Filters", EditorStyles.miniButton, GUILayout.Width(100))) 
        { 
            foreach (var k in _levelFilters.Keys.ToList()) _levelFilters[k] = false; 
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        _levelsScrollPos = EditorGUILayout.BeginScrollView(_levelsScrollPos, "box");
        DrawLevels();
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("Create New Level", GUILayout.Height(30))) CreateNewLevel();
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        
        EditorGUILayout.LabelField("Groups Pool (Drag to Level Header)", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _groupSearch = EditorGUILayout.TextField(GUIContent.none, _groupSearch, EditorStyles.toolbarSearchField);
        
        if (GUILayout.Button(new GUIContent("X", "Clear Search"), EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            _groupSearch = "";
            GUI.FocusControl(null);
        }

        if (GUILayout.Button(new GUIContent("↻", "Reload Data"), EditorStyles.toolbarButton, GUILayout.Width(30))) ReloadAllData();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        _groupsScrollPos = EditorGUILayout.BeginScrollView(_groupsScrollPos, "box");
        DrawUnusedGroupsFiltered();
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void ReloadAllData()
    {
        _allLevels = LoadAssets<LevelData>(new[] { LEVELS_FOLDER }).OrderBy(l => l.name).ToList();
        _allGroups = LoadAssets<GroupData>(new[] { GROUPS_FOLDER }).OrderBy(g => g.name).ToList();
        foreach (var level in _allLevels)
        {
            if (!_levelFoldouts.ContainsKey(level)) _levelFoldouts[level] = true;
            if (!_levelFilters.ContainsKey(level)) _levelFilters[level] = false;
        }
        var usedGroups = new HashSet<GroupData>(_allLevels.SelectMany(l => l.requiredGroups).Where(g => g != null));
        _unusedGroups = _allGroups.Where(g => !usedGroups.Contains(g)).OrderBy(g => g.name).ToList();
    }

    private List<T> LoadAssets<T>(string[] searchInFolders) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", searchInFolders);
        return guids.Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).Where(a => a != null).ToList();
    }

    private void DrawLevels()
    {
        if (_allLevels.Count == 0) { EditorGUILayout.LabelField("No levels found. Create one!"); return; }

        LevelData levelToRemove = null;
        bool needsReload = false;

        for (int i = 0; i < _allLevels.Count; i++)
        {
            var level = _allLevels[i];
            if (level == null) { needsReload = true; break; }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Rect headerRect = GUILayoutUtility.GetRect(16, 24, GUILayout.ExpandWidth(true));
            
            bool fold = _levelFoldouts[level];
            
            int groupCount = level.requiredGroups.Count(g => g != null);
            string headerContent = $"{level.name} [{groupCount} groups]";

            fold = EditorGUI.Foldout(new Rect(headerRect.x, headerRect.y, headerRect.width - 200, headerRect.height), fold, new GUIContent(headerContent, "Drag Group assets here"), true, EditorStyles.foldoutHeader);
            _levelFoldouts[level] = fold;
            
            Rect headerRight = new Rect(headerRect.xMax - 180, headerRect.y, 180, headerRect.height);

            Rect filterRect = new Rect(headerRight.x + 10, headerRight.y + 2, 80, headerRight.height - 4);
            
            Color originalColor = GUI.backgroundColor;
            if (_levelFilters[level])
            {
                GUI.backgroundColor = Color.cyan;
            }
            
            if (GUI.Button(filterRect, new GUIContent("Filter", "Toggle item intersection filter for this level"), EditorStyles.miniButton))
            {
                _levelFilters[level] = !_levelFilters[level];
            }
            
            GUI.backgroundColor = originalColor;

            if (GUI.Button(new Rect(headerRight.x + 130, headerRight.y + 2, 35, headerRight.height - 4), new GUIContent("🗑️", "Delete Level"), EditorStyles.miniButton))
            {
                if (EditorUtility.DisplayDialog("Confirm Deletion", $"Are you sure you want to delete Level '{level.name}'?", "Yes", "No"))
                {
                    levelToRemove = level;
                }
            }

            HandleHeaderDrop(headerRect, level);

            if (_levelFoldouts[level])
            {
                EditorGUI.indentLevel++;
                
                GroupData groupToRemove = null;
                
                for (int j = 0; j < level.requiredGroups.Count; j++)
                {
                    var grp = level.requiredGroups[j];
                    if (grp == null)
                    {
                        Undo.RecordObject(level, "Remove Null Group from Level");
                        level.requiredGroups.RemoveAt(j);
                        EditorUtility.SetDirty(level);
                        needsReload = true;
                        break;
                    }
                    
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button(new GUIContent("➖", "Remove Group"), EditorStyles.miniButton, GUILayout.Width(25)))
                    {
                        groupToRemove = grp;
                    }
                    
                    DrawGroupPreviewLine(grp, GUILayout.ExpandWidth(true)); 
                    
                    EditorGUILayout.EndHorizontal();
                }

                if (groupToRemove != null)
                {
                    Undo.RecordObject(level, "Remove Group from Level");
                    level.requiredGroups.Remove(groupToRemove);
                    EditorUtility.SetDirty(level);
                    needsReload = true;
                }

                CheckForIntersections(level);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        if (levelToRemove != null)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(levelToRemove));
            needsReload = true;
        }

        if (needsReload)
        {
            ReloadAllData();
        }
        
        if (GUI.changed) AssetDatabase.SaveAssets();
    }

    private void HandleHeaderDrop(Rect headerRect, LevelData targetLevel)
    {
        Event currentEvent = Event.current;
        if (!headerRect.Contains(currentEvent.mousePosition)) return;
        
        if (currentEvent.type == EventType.DragUpdated)
        {
            if (DragAndDrop.objectReferences.Any(obj => obj is GroupData))
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
                if (obj is GroupData draggedGroup && !targetLevel.requiredGroups.Contains(draggedGroup))
                {
                    Undo.RecordObject(targetLevel, "Add Group to Level");
                    targetLevel.requiredGroups.Add(draggedGroup);
                    wasModified = true;
                }
            }
            if (wasModified)
            {
                EditorUtility.SetDirty(targetLevel);
                ReloadAllData();
            }
            currentEvent.Use();
        }
    }

    private void DrawUnusedGroupsFiltered()
    {
        IEnumerable<GroupData> pool = _unusedGroups;
        if (!string.IsNullOrEmpty(_groupSearch))
        {
            string s = _groupSearch.ToLower();
            pool = pool.Where(g => g != null && (g.name.ToLower().Contains(s) || 
                                   (g.groupKey != null && g.groupKey.ToLower().Contains(s)) ||
                                   AssetDatabase.GetAssetPath(g).ToLower().Contains(s)));
        }
        
        var activeFilters = _levelFilters.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        if (activeFilters.Count > 0)
        {
            pool = pool.Where(g => g != null && activeFilters.All(level => !HasItemIntersection(level, g)));
        }
        
        if (!pool.Any())
        {
            EditorGUILayout.LabelField("No unused groups found matching criteria.");
            return;
        }

        foreach (var group in pool.ToList())
        {
            if (group == null) continue;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            DrawGroupPreviewLine(group);
            
            EditorGUILayout.EndHorizontal();
            
            Rect objRect = GUILayoutUtility.GetLastRect();
            Event currentEvent = Event.current;
            if (objRect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { group };
                DragAndDrop.StartDrag($"Dragging {group.name}");
                currentEvent.Use();
            }
        }
    }

    private void DrawGroupPreviewLine(GroupData group, params GUILayoutOption[] options)
    {
        Rect lineRect = GUILayoutUtility.GetRect(0, 22, options.Any() ? options : new[] { GUILayout.ExpandWidth(true) }); 
        
        float iconAreaWidth = 100f;
        Rect itemIconsRect = new Rect(lineRect.x + 4, lineRect.y + 3, iconAreaWidth, 16);
        
        if (group.items != null)
        {
            float x = itemIconsRect.x;
            int maxIcons = 5;
            int iconCount = 0;
            
            foreach (var item in group.items)
            {
                if (iconCount >= maxIcons) break;
                
                if (item != null && item.icon != null && item.icon.texture != null)
                {
                    Texture2D tex = item.icon.texture;
                    Rect texRect = item.icon.textureRect;
                    Rect texCoords = new Rect(texRect.x / tex.width, texRect.y / tex.height, texRect.width / tex.width, texRect.height / tex.height);
                    
                    float aspect = texRect.width / texRect.height;
                    float drawW = 16f;
                    float drawH = 16f;
                    if (aspect > 1f) drawH = 16f / aspect; else drawW = 16f * aspect;
                    float offsetX = (16f - drawW) / 2f;
                    float offsetY = (16f - drawH) / 2f;
                    
                    Rect adj = new Rect(x + offsetX, itemIconsRect.y + offsetY, drawW, drawH);
                    GUI.DrawTextureWithTexCoords(adj, tex, texCoords);
                }
                else
                {
                    EditorGUI.LabelField(new Rect(x, itemIconsRect.y, 16, 16), "❓", EditorStyles.miniLabel);
                }
                
                x += 20;
                iconCount++;
            }
            
            if (group.items.Count > maxIcons)
            {
                 EditorGUI.LabelField(new Rect(x, itemIconsRect.y, 16, 16), new GUIContent("...", "More items..."), EditorStyles.miniLabel);
            }
        }

        string displayName = group.name;
        if (displayName.StartsWith("group_"))
        {
            displayName = displayName.Substring("group_".Length);
        }
        
        float nameAreaX = lineRect.x + iconAreaWidth + 10;
        float nameAreaWidth = lineRect.width - iconAreaWidth - 10;
        
        Rect labelRect = new Rect(nameAreaX, lineRect.y, nameAreaWidth, lineRect.height);
        EditorGUI.LabelField(labelRect, displayName, EditorStyles.label);
    }

    private bool HasItemIntersection(LevelData level, GroupData candidate)
    {
        var levelItems = new HashSet<ItemData>(level.requiredGroups.Where(g => g != null).SelectMany(g => g.items).Where(i => i != null));
        if (candidate.items == null) return false;
        foreach (var item in candidate.items)
        {
            if (item != null && levelItems.Contains(item)) return true;
        }
        return false;
    }

    private void CheckForIntersections(LevelData level)
    {
        var itemUsage = new Dictionary<ItemData, List<string>>();
        foreach (var group in level.requiredGroups)
        {
            if (group == null || group.items == null) continue;
            foreach (var item in group.items)
            {
                if (item == null) continue;
                if (!itemUsage.ContainsKey(item)) itemUsage[item] = new List<string>();
                itemUsage[item].Add(group.groupKey);
            }
        }
        var intersects = itemUsage.Where(kv => kv.Value.Count > 1).ToList();
        if (intersects.Any())
        {
            string txt = "Intersection Found!\n";
            foreach (var it in intersects) txt += $"Item '{it.Key.itemKey}' used in: {string.Join(", ", it.Value)}\n";
            EditorGUILayout.HelpBox(txt, MessageType.Warning);
        }
    }

    private void CreateNewLevel()
    {
        EnsureFolderExists(LEVELS_FOLDER);
        int nextNumber = GetNextLevelNumber();
        var newLevel = CreateInstance<LevelData>();
        string path = Path.Combine(LEVELS_FOLDER, $"Level_{nextNumber}.asset");
        string unique = AssetDatabase.GenerateUniqueAssetPath(path);
        AssetDatabase.CreateAsset(newLevel, unique);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newLevel;
        ReloadAllData();
    }

    private int GetNextLevelNumber()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LEVELS_FOLDER });
        int max = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fn = Path.GetFileNameWithoutExtension(path);
            Match m = Regex.Match(fn, @"Level_(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int n)) if (n > max) max = n;
        }
        return max + 1;
    }

    private void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}