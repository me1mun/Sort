using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class LocalizationEditorWindow : EditorWindow
{
    private class TranslationNode
    {
        public string Name;
        public string FullKey;
        public Dictionary<string, TranslationNode> Children = new Dictionary<string, TranslationNode>();
        public bool IsExpanded = true;
    }

    private const float ToolbarButtonWidth = 30f;
    private const float KeyColumnMinWidth = 250f;
    private const float LangColumnMinWidth = 150f;
    private const float ActionButtonWidth = 25f;

    private LanguageConfig _config;
    private Vector2 _scrollPosition;
    
    private Dictionary<string, Dictionary<string, string>> _languageData;
    private TranslationNode _rootNode;
    private List<string> _masterKeyList = new List<string>();

    private string _newKey = "group.new_key";
    private string _searchText = "";
    private string _statusMessage = "Ready";
    private bool _isDirty = false;

    private GUIContent _reloadIcon;
    private GUIContent _syncIcon;
    private GUIContent _generateIcon;
    private GUIContent _saveIcon;

    [MenuItem("Tools/Localization Editor")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationEditorWindow>("Localization Editor");
    }

    private void OnEnable()
    {
        _reloadIcon = EditorGUIUtility.IconContent("d_Refresh", "Reload all data from files.");
        _saveIcon = EditorGUIUtility.IconContent("d_SaveAs", "Save all changes to files.");
        _syncIcon = EditorGUIUtility.IconContent("d_Mirror", "Synchronize Keys|Add missing keys to all languages.");
        _generateIcon = EditorGUIUtility.IconContent("d_ScriptableObject Icon", "Generate Keys|Generate keys from GroupData assets.");
    }

    private void OnGUI()
    {
        var newConfig = (LanguageConfig)EditorGUILayout.ObjectField("Language Config", _config, typeof(LanguageConfig), false);
        if (newConfig != _config)
        {
            _config = newConfig;
            LoadLocalizationData();
        }

        if (_config == null)
        {
            EditorGUILayout.HelpBox("Please assign a Language Config to begin editing.", MessageType.Info);
            return;
        }

        DrawToolbar();
        
        if (_languageData == null || _rootNode == null) return;

        DrawMainContent();
        DrawStatusBar();
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button(_reloadIcon, EditorStyles.toolbarButton, GUILayout.Width(ToolbarButtonWidth))) LoadLocalizationData();
        
        EditorGUI.BeginDisabledGroup(!_isDirty);
        if (GUILayout.Button(_saveIcon, EditorStyles.toolbarButton, GUILayout.Width(ToolbarButtonWidth))) PersistChangesToFiles();
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);
        
        if (GUILayout.Button(_syncIcon, EditorStyles.toolbarButton, GUILayout.Width(ToolbarButtonWidth))) SynchronizeKeys();
        if (GUILayout.Button(_generateIcon, EditorStyles.toolbarButton, GUILayout.Width(ToolbarButtonWidth))) GenerateGroupKeys();

        GUILayout.FlexibleSpace();
        
        EditorGUI.BeginChangeCheck();
        _searchText = EditorGUILayout.TextField(_searchText, GUI.skin.FindStyle("ToolbarSearchTextField"), GUILayout.MaxWidth(250));
        if (EditorGUI.EndChangeCheck())
        {
            ApplyFilter();
        }
        if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton")))
        {
            _searchText = "";
            ApplyFilter();
            GUI.FocusControl(null);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMainContent()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
        DrawTranslationTable();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        DrawAddNewKeySection();
        EditorGUILayout.EndVertical();
    }

    private void DrawTranslationTable()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Localization Key", EditorStyles.boldLabel, GUILayout.MinWidth(KeyColumnMinWidth));
        foreach (var lang in _config.Languages)
        {
            EditorGUILayout.LabelField(lang.LanguageName, EditorStyles.boldLabel, GUILayout.MinWidth(LangColumnMinWidth));
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
        if(_rootNode?.Children != null && _rootNode.Children.Count > 0)
        {
            foreach (var node in _rootNode.Children.Values.OrderBy(n => n.Name))
            {
                DrawNode(node);
            }
        }

        EditorGUILayout.EndScrollView();
    }
    
    private void DrawNode(TranslationNode node)
    {
        if (node.FullKey != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(node.Name, GUILayout.Height(20), GUILayout.MinWidth(KeyColumnMinWidth - 10));

            foreach (var lang in _config.Languages)
            {
                var langDict = _languageData[lang.LanguageCode];
                string currentValue;
                langDict.TryGetValue(node.FullKey, out currentValue);
                currentValue = currentValue ?? "";
                
                GUI.backgroundColor = string.IsNullOrEmpty(currentValue) ? new Color(1, 0.7f, 0.7f, 0.5f) : Color.white;
                
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUILayout.TextField(currentValue, GUILayout.MinWidth(LangColumnMinWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    langDict[node.FullKey] = newValue;
                    _isDirty = true;
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(ActionButtonWidth)))
            {
                if (EditorUtility.DisplayDialog("Delete Key?", $"Delete the key '{node.FullKey}' from all languages?", "Yes", "No"))
                {
                    DeleteKey(node.FullKey);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, node.Name, true, EditorStyles.foldoutHeader);
            if (node.IsExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var childNode in node.Children.Values.OrderBy(n => n.Name))
                {
                    DrawNode(childNode);
                }
                EditorGUI.indentLevel--;
            }
        }
    }

    private void DrawAddNewKeySection()
    {
        EditorGUILayout.BeginHorizontal();
        _newKey = EditorGUILayout.TextField("New Key", _newKey);
        if (GUILayout.Button("Add", GUILayout.Width(50)))
        {
            if (!string.IsNullOrWhiteSpace(_newKey) && !_masterKeyList.Contains(_newKey))
            {
                AddNewKey(_newKey);
                _newKey = "";
                GUI.FocusControl(null);
            }
            else ShowNotification(new GUIContent("Key cannot be empty or already exist."));
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField(_statusMessage, EditorStyles.miniLabel);
        if (_isDirty)
        {
            EditorGUILayout.LabelField("Unsaved changes", EditorStyles.miniBoldLabel, GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrEmpty(_searchText))
        {
            BuildTreeFromKeys(_masterKeyList);
        }
        else
        {
            var filteredKeys = _masterKeyList.Where(k => k.ToLower().Contains(_searchText.ToLower())).ToList();
            BuildTreeFromKeys(filteredKeys);
        }
        Repaint();
    }

    private void BuildTreeFromKeys(List<string> keys)
    {
        _rootNode = new TranslationNode { Name = "Root" };
        foreach (string key in keys)
        {
            var parts = key.Split('.');
            var currentNode = _rootNode;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (!currentNode.Children.ContainsKey(part))
                {
                    currentNode.Children[part] = new TranslationNode { Name = part };
                }
                currentNode = currentNode.Children[part];
                
                if (i == parts.Length - 1)
                {
                    currentNode.FullKey = key;
                }
            }
        }
    }

    private void LoadLocalizationData()
    {
        if (_config == null) { _languageData = null; _rootNode = null; return; }

        _languageData = new Dictionary<string, Dictionary<string, string>>();
        var allKeys = new HashSet<string>();

        foreach (var langDef in _config.Languages)
        {
            var langDict = new Dictionary<string, string>();
            if (langDef.TranslationFile != null)
            {
                var data = JsonUtility.FromJson<LocalizationData>(langDef.TranslationFile.text);
                if (data?.items != null)
                {
                    foreach (var item in data.items)
                    {
                        if (!string.IsNullOrEmpty(item.key))
                        {
                            langDict[item.key] = item.value;
                            allKeys.Add(item.key);
                        }
                    }
                }
            }
            _languageData[langDef.LanguageCode] = langDict;
        }
        
        _masterKeyList = allKeys.OrderBy(k => k).ToList();
        ApplyFilter();
        _isDirty = false;
        SetStatus($"Loaded {_masterKeyList.Count} keys for {_languageData.Count} languages.");
    }

    private void PersistChangesToFiles()
    {
        if (_config == null || _languageData == null) return;
        
        foreach (var langDef in _config.Languages)
        {
            if (langDef.TranslationFile == null) continue;

            string path = AssetDatabase.GetAssetPath(langDef.TranslationFile);
            if (string.IsNullOrEmpty(path)) continue;

            var langDict = _languageData[langDef.LanguageCode];
            var newData = new LocalizationData
            {
                items = _masterKeyList
                    .Select(k => 
                    {
                        langDict.TryGetValue(k, out string value);
                        return new LocalizationItem { key = k, value = value ?? "" };
                    })
                    .OrderBy(i => i.key)
                    .ToArray()
            };

            string json = JsonUtility.ToJson(newData, true);
            File.WriteAllText(path, json);
        }
        
        EditorUtility.SetDirty(_config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _isDirty = false;
        SetStatus("All changes saved successfully!");
    }
    
    private void SynchronizeKeys()
    {
        if (_config == null || _languageData == null) return;
        
        int keysAddedCount = 0;
        foreach (var langDict in _languageData.Values)
        {
            foreach (string masterKey in _masterKeyList)
            {
                if (!langDict.ContainsKey(masterKey))
                {
                    langDict.Add(masterKey, "");
                    keysAddedCount++;
                }
            }
        }

        if (keysAddedCount > 0)
        {
            _isDirty = true;
            SetStatus($"Synchronization complete. {keysAddedCount} missing key(s) added.");
        }
        else SetStatus("All languages are already synchronized.");
    }
    
    private void GenerateGroupKeys()
    {
        if (_config == null) return;

        var allGroups = LoadAllAssets<GroupData>();
        if (allGroups.Count == 0) { SetStatus("No GroupData assets found in project."); return; }

        int keysAdded = 0;
        foreach (var group in allGroups)
        {
            if (string.IsNullOrEmpty(group.groupKey)) continue;

            string key = $"groups.{group.groupKey}";
            if (!_masterKeyList.Contains(key))
            {
                AddNewKey(key);
                keysAdded++;
            }
        }

        if (keysAdded > 0) SetStatus($"Successfully added {keysAdded} new group key(s).");
        else SetStatus("All group keys are already up to date.");
    }

    private void AddNewKey(string key)
    {
        _masterKeyList.Add(key);
        _masterKeyList.Sort();
        
        foreach (var langDict in _languageData.Values)
        {
            langDict.Add(key, "");
        }
        ApplyFilter();
        _isDirty = true;
    }
    
    private void DeleteKey(string key)
    {
        _masterKeyList.Remove(key);
        
        foreach (var langDict in _languageData.Values)
        {
            langDict.Remove(key);
        }
        ApplyFilter();
        _isDirty = true;
    }
    
    private void SetStatus(string message)
    {
        _statusMessage = message;
        Repaint();
    }
    
    private static List<T> LoadAllAssets<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .ToList();
    }
}