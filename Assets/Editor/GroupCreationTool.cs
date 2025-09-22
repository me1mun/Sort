using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GroupCreationWindow : EditorWindow
{
    private string groupName = "";
    private List<ItemData> selectedItems = new List<ItemData>();
    private const string GroupsFolderPath = "Assets/Resources/Groups";

    [MenuItem("Tools/Game/Group Creator")]
    public static void ShowWindow()
    {
        GetWindow<GroupCreationWindow>("Group Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create New Group", EditorStyles.boldLabel);
        
        groupName = EditorGUILayout.TextField("Group Name / Key", groupName);
        
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Selected Items:", $"{selectedItems.Count} item(s)");

        EditorGUILayout.Space();

        GUI.enabled = !string.IsNullOrEmpty(groupName) && selectedItems.Count > 0;

        if (GUILayout.Button("Create Group"))
        {
            CreateGroupAsset();
        }

        GUI.enabled = true;
    }

    private void OnSelectionChange()
    {
        selectedItems = Selection.GetFiltered<ItemData>(SelectionMode.Assets).ToList();
        Repaint();
    }

    private void CreateGroupAsset()
    {
        var newGroup = ScriptableObject.CreateInstance<GroupData>();
        newGroup.groupKey = groupName;
        newGroup.items.AddRange(selectedItems.OrderBy(i => i.name));

        if (!Directory.Exists(GroupsFolderPath))
        {
            Directory.CreateDirectory(GroupsFolderPath);
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GroupsFolderPath, $"{groupName}.asset"));

        AssetDatabase.CreateAsset(newGroup, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newGroup;
        
        this.Close();
    }
}