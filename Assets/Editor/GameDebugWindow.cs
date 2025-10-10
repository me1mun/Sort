using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameDebugWindow : EditorWindow
{
    private LanguageConfig _languageConfig;
    private string[] _languageCodes;
    private string[] _languageNames;
    private int _selectedLanguageIndex = 0;
    
    private int _levelToLoad = 1;

    [MenuItem("Tools/Game Debug Window")]
    public static void ShowWindow()
    {
        GetWindow<GameDebugWindow>("Game Debug");
    }

    private void OnEnable()
    {
        FindAndLoadLanguageConfig();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Обновляем выбранный язык, когда входим в Play Mode
            UpdateSelectedLanguageFromGame();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Game Debug & Test Tool", EditorStyles.boldLabel);
        
        DrawLanguageSection();
        EditorGUILayout.Separator();
        DrawLevelSelectionSection();
    }

    private void DrawLanguageSection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Dynamic Language Change", EditorStyles.boldLabel);

        if (_languageConfig == null)
        {
            EditorGUILayout.HelpBox("LanguageConfig not found.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }
        
        // Кнопки и список активны только в режиме игры
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        
        EditorGUI.BeginChangeCheck();
        _selectedLanguageIndex = EditorGUILayout.Popup("Select Language", _selectedLanguageIndex, _languageNames);
        if (EditorGUI.EndChangeCheck())
        {
            // Если игра запущена, сразу меняем язык
            if (EditorApplication.isPlaying && LocalizationManager.Instance != null)
            {
                string selectedCode = _languageCodes[_selectedLanguageIndex];
                LocalizationManager.Instance.SetLanguage(selectedCode);
                Debug.Log($"[Debug] Language changed dynamically to: {selectedCode}.");
            }
        }
        
        EditorGUI.EndDisabledGroup();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to change the language.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }
    
    private void DrawLevelSelectionSection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Level Selection (In Play Mode)", EditorStyles.boldLabel);
        
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        _levelToLoad = EditorGUILayout.IntField("Level to Load", _levelToLoad);
        if (GUILayout.Button($"Load Level {_levelToLoad}"))
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.Progress.predefinedLevelIndex = _levelToLoad - 1;
                DataManager.Instance.Progress.randomLevelCount = 0;
                DataManager.Instance.SaveProgress(); // Сохраняем прогресс, чтобы он применился при перезапуске
                
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                Debug.Log($"[Debug] Loading level {_levelToLoad}...");
            }
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndVertical();
    }

    private void FindAndLoadLanguageConfig()
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(LanguageConfig).Name}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _languageConfig = AssetDatabase.LoadAssetAtPath<LanguageConfig>(path);
            if (_languageConfig != null)
            {
                _languageCodes = _languageConfig.Languages.Select(l => l.LanguageCode).ToArray();
                _languageNames = _languageConfig.Languages.Select(l => l.LanguageName).ToArray();
                _selectedLanguageIndex = 0;
            }
        }
    }

    private void UpdateSelectedLanguageFromGame()
    {
        if (LocalizationManager.Instance != null && _languageCodes != null)
        {
            string currentLangCode = LocalizationManager.Instance.CurrentLanguage;
            _selectedLanguageIndex = System.Array.IndexOf(_languageCodes, currentLangCode);
            if (_selectedLanguageIndex == -1) _selectedLanguageIndex = 0;
            Repaint(); // Обновляем окно, чтобы показать правильный язык
        }
    }
}