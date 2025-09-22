using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AssetUsageAnalyzerWindow : EditorWindow
{
    private Dictionary<int, List<ItemData>> _analysisResult;
    private Vector2 _scrollPosition;
    private Dictionary<int, bool> _foldoutStates = new Dictionary<int, bool>();
    private int _totalItemsAnalyzed;
    private int _totalGroupsAnalyzed;

    [MenuItem("Game Tools/Asset Usage Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<AssetUsageAnalyzerWindow>("Asset Usage Analyzer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Asset Usage Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Analyzes how many times each 'ItemData' is used across all 'GroupData' assets in the project.", MessageType.Info);

        if (GUILayout.Button("Analyze Asset Usage", GUILayout.Height(30)))
        {
            AnalyzeAssets();
        }

        EditorGUILayout.Separator();

        if (_analysisResult != null)
        {
            EditorGUILayout.LabelField($"Analyzed: {_totalItemsAnalyzed} Items and {_totalGroupsAnalyzed} Groups.", EditorStyles.miniLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var sortedKeys = _analysisResult.Keys.OrderBy(k => k).ToList();

            foreach (int usageCount in sortedKeys)
            {
                var items = _analysisResult[usageCount];
                string foldoutLabel = $"Used {usageCount} time(s) ({items.Count} items)";
                
                if (!_foldoutStates.ContainsKey(usageCount))
                {
                    _foldoutStates[usageCount] = false;
                }
                
                GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
                if (usageCount == 0 && items.Count > 0)
                {
                    foldoutStyle.fontStyle = FontStyle.Bold;
                    foldoutStyle.normal.textColor = new Color(1f, 0.8f, 0.4f); // Orange-yellow
                    foldoutLabel = $"⚠️ {foldoutLabel}";
                }

                _foldoutStates[usageCount] = EditorGUILayout.Foldout(_foldoutStates[usageCount], foldoutLabel, true, foldoutStyle);

                if (_foldoutStates[usageCount])
                {
                    EditorGUI.indentLevel++;
                    
                    // Формируем и выводим текстовый список
                    var formattedNames = items.Select(item => FormatItemName(item.name));
                    string commaSeparatedList = string.Join(", ", formattedNames);
                    
                    EditorGUILayout.LabelField("Copyable Text List:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.SelectableLabel(commaSeparatedList, EditorStyles.textArea, GUILayout.MinHeight(40), GUILayout.MaxHeight(120));
                    EditorGUILayout.Space(5);

                    // Выводим кликабельные поля объектов
                    EditorGUILayout.LabelField("Interactive Object List:", EditorStyles.miniBoldLabel);
                    foreach (var item in items)
                    {
                        EditorGUILayout.ObjectField(item, typeof(ItemData), false);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private string FormatItemName(string originalName)
    {
        string name = originalName;
        if (name.StartsWith("Item_"))
        {
            name = name.Substring("Item_".Length);
        }
        return name.Replace('_', ' ');
    }

    private void AnalyzeAssets()
    {
        var allItems = LoadAllAssets<ItemData>();
        var allGroups = LoadAllAssets<GroupData>();

        _totalItemsAnalyzed = allItems.Count;
        _totalGroupsAnalyzed = allGroups.Count;
        _foldoutStates.Clear();

        if (allItems.Count == 0)
        {
            _analysisResult = null;
            ShowNotification(new GUIContent("No ItemData assets found to analyze."));
            return;
        }

        var usageCount = new Dictionary<ItemData, int>();
        foreach (var item in allItems)
        {
            usageCount[item] = 0;
        }

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
        
        _analysisResult = new Dictionary<int, List<ItemData>>();
        foreach (var pair in usageCount)
        {
            int count = pair.Value;
            ItemData item = pair.Key;

            if (!_analysisResult.ContainsKey(count))
            {
                _analysisResult[count] = new List<ItemData>();
            }
            _analysisResult[count].Add(item);
        }
    }

    private static List<T> LoadAllAssets<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Select(path => AssetDatabase.LoadAssetAtPath<T>(path))
            .Where(asset => asset != null)
            .ToList();
    }
}