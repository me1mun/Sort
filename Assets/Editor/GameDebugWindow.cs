// File: GameDebugWindow.cs
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO; // Добавляем для работы с файлами

public class GameDebugWindow : EditorWindow
{
    private LanguageConfig _languageConfig;
    private string[] _languageCodes;
    private string[] _languageNames;
    private int _selectedLanguageIndex = 0;
    
    // Поля для уровней
    private int _predefinedLevelIndexToSet = 0; // Начинаем с 0, т.к. это индекс
    private int _randomLevelCountToSet = 0;

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
            // Обновляем поля уровней из текущего прогресса игры
            UpdateLevelFieldsFromGameProgress(); 
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Game Debug & Test Tool", EditorStyles.boldLabel);
        
        DrawLanguageSection();
        EditorGUILayout.Separator();
        DrawLevelSelectionSection();
        EditorGUILayout.Separator();
        DrawUtilitySection(); // Новый раздел для утилит
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
        EditorGUILayout.LabelField("Level Progress Control", EditorStyles.boldLabel);
        
        // Поля ввода всегда активны, чтобы можно было установить прогресс до запуска игры
        
        // 1. Поле для заготовленных уровней (индекс)
        // Отображаем как 'Level N', но сохраняем как индекс N-1.
        int predefinedLevelDisplay = _predefinedLevelIndexToSet + 1;
        
        // Гарантируем, что введенное значение не меньше 1
        predefinedLevelDisplay = EditorGUILayout.IntField("Predefined Level (1+)", predefinedLevelDisplay);
        _predefinedLevelIndexToSet = Mathf.Max(0, predefinedLevelDisplay - 1);
        
        // 2. Поле для случайных уровней
        // Гарантируем, что введенное значение не меньше 0
        _randomLevelCountToSet = EditorGUILayout.IntField("Random Levels Count (0+)", _randomLevelCountToSet);
        _randomLevelCountToSet = Mathf.Max(0, _randomLevelCountToSet);

        int totalDisplayLevel = _predefinedLevelIndexToSet + _randomLevelCountToSet + 1;
        EditorGUILayout.LabelField("Total Displayed Level:", totalDisplayLevel.ToString());
        
        
        // --- ПУНКТ 1: Загрузка уровня немедленно (в Play Mode) ---
        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying || DataManager.Instance == null);
        if (GUILayout.Button($"1. Load Level Now (Reload Scene)"))
        {
            SetProgressAndReloadScene();
        }
        EditorGUI.EndDisabledGroup();
        
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to load the level immediately.", MessageType.Info);
        }

        // --- ПУНКТ 2: Запись прогресса для следующего запуска ---
        if (GUILayout.Button($"2. Set Progress for Next Launch (Save Only)"))
        {
            SetProgressAndSaveOnly();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawUtilitySection()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Data Utilities", EditorStyles.boldLabel);
        
        // --- ПУНКТ 3: Сброс всех сохранений ---
        if (GUILayout.Button("!!! Reset All Saved Data (Progress & Settings) !!!"))
        {
            if (EditorUtility.DisplayDialog("Confirm Reset", 
                                            "Are you sure you want to delete ALL save files (progress.json and settings.json)? This action cannot be undone.", 
                                            "Yes, Delete", 
                                            "Cancel"))
            {
                ResetAllSavedData();
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // --- Методы логики ---
    
    private void SetProgressAndReloadScene()
    {
        if (DataManager.Instance != null)
        {
            // 1. Устанавливаем прогресс в DataManager
            DataManager.Instance.Progress.predefinedLevelIndex = _predefinedLevelIndexToSet;
            DataManager.Instance.Progress.randomLevelCount = _randomLevelCountToSet;
            
            // 2. Сохраняем, чтобы изменения вступили в силу при перезагрузке
            DataManager.Instance.SaveProgress(); 
            
            // 3. Перезагружаем текущую сцену
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Debug.Log($"[Debug] Progress set and scene reloaded: Predefined Index = {_predefinedLevelIndexToSet}, Random Count = {_randomLevelCountToSet}.");
        }
    }
    
    private void SetProgressAndSaveOnly()
    {
        // Создаем временный DataManager, если его нет (т.е. мы не в Play Mode), чтобы иметь возможность записать сохранение.
        // DataManager.Instance будет null, если мы не в Play Mode.
        if (DataManager.Instance == null)
        {
            // Создаем временный экземпляр ProgressData и сохраняем его напрямую,
            // т.к. DataManager.Instance недоступен.
            ProgressData tempProgress = new ProgressData
            {
                predefinedLevelIndex = _predefinedLevelIndexToSet,
                randomLevelCount = _randomLevelCountToSet
            };
            
            // Получаем путь сохранения, как это делает DataManager
            string savePath = Path.Combine(Application.persistentDataPath, "progress.json");
            string json = JsonUtility.ToJson(tempProgress, true);
            File.WriteAllText(savePath, json);
            
            Debug.Log($"[Debug] Progress saved successfully for next launch (via direct file write): Predefined Index = {_predefinedLevelIndexToSet}, Random Count = {_randomLevelCountToSet}.");
        }
        else
        {
            // DataManager.Instance доступен (мы в Play Mode), используем его
            DataManager.Instance.Progress.predefinedLevelIndex = _predefinedLevelIndexToSet;
            DataManager.Instance.Progress.randomLevelCount = _randomLevelCountToSet;
            DataManager.Instance.SaveProgress();
            
            Debug.Log($"[Debug] Progress saved successfully for next launch (via DataManager): Predefined Index = {_predefinedLevelIndexToSet}, Random Count = {_randomLevelCountToSet}.");
        }
    }

    private void ResetAllSavedData()
    {
        string progressPath = Path.Combine(Application.persistentDataPath, "progress.json");
        string settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
        
        bool deletedAny = false;

        if (File.Exists(progressPath))
        {
            File.Delete(progressPath);
            Debug.Log($"[Debug] Deleted: {progressPath}");
            deletedAny = true;
        }
        
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
            Debug.Log($"[Debug] Deleted: {settingsPath}");
            deletedAny = true;
        }

        if (deletedAny)
        {
            Debug.Log("[Debug] All save files have been reset.");
            // Если DataManager существует (в Play Mode), принудительно перезагружаем данные
            if (DataManager.Instance != null)
            {
                DataManager.Instance.LoadAllData();
                UpdateLevelFieldsFromGameProgress(); // Обновляем поля, чтобы отразить сброс
            }
        }
        else
        {
            Debug.Log("[Debug] No save files were found to delete.");
        }
    }

    // --- Методы инициализации ---

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
            Repaint(); // Обновляем окно
        }
    }
    
    private void UpdateLevelFieldsFromGameProgress()
    {
        // Этот метод вызывается только в Play Mode, когда DataManager.Instance уже загрузил прогресс.
        if (DataManager.Instance != null)
        {
            _predefinedLevelIndexToSet = DataManager.Instance.Progress.predefinedLevelIndex;
            _randomLevelCountToSet = DataManager.Instance.Progress.randomLevelCount;
            Repaint();
        }
    }
}