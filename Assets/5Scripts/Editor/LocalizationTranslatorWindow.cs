using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class LocalizationTranslatorWindow : EditorWindow
{
    private string targetLanguage = "tr";
    private DefaultAsset jsonFile;
    private ScriptableObject scriptableObjectAsset;
    private GameObject gameObjectAsset;

    // Helper classes for JSON mapping to match test.json
    [System.Serializable]
    public class JSONReplicaFile {
        public List<JSONReplica> replicas;
        public List<JSONChoice> choices;
    }
    [System.Serializable]
    public class JSONReplica {
        public string characterId;
        public List<JSONPhrase> phrases;
    }
    [System.Serializable]
    public class JSONPhrase {
        public LocalizedLine text;
        public string iconName;
        public string voiceClipName;
    }
    [System.Serializable]
    public class JSONChoice {
        public LocalizedLine buttonText;
        public string nextDialogueName;
    }

    [MenuItem("Tools/Auto Translator")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationTranslatorWindow>("Auto Translator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Translate Dialogues (Google Translate)", EditorStyles.boldLabel);
        
        targetLanguage = EditorGUILayout.TextField("Target Language Code (tr, ru, es)", targetLanguage);
        
        GUILayout.Space(10);
        GUILayout.Label("1. Translate a ScriptableObject", EditorStyles.boldLabel);
        scriptableObjectAsset = (ScriptableObject)EditorGUILayout.ObjectField("Dialogue Asset", scriptableObjectAsset, typeof(ScriptableObject), false);
        
        if (GUILayout.Button("Translate ScriptableObject"))
        {
            if (scriptableObjectAsset != null)
                EditorCoroutineHelper.StartCoroutine(TranslateSO(scriptableObjectAsset));
            else
                Debug.LogWarning("Select a ScriptableObject first!");
        }

        GUILayout.Space(10);
        GUILayout.Label("2. Translate JSON File (e.g. test.json)", EditorStyles.boldLabel);
        jsonFile = (DefaultAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(DefaultAsset), false);

        if (GUILayout.Button("Translate JSON"))
        {
            if (jsonFile != null)
            {
                string path = AssetDatabase.GetAssetPath(jsonFile);
                if (path.EndsWith(".json"))
                    EditorCoroutineHelper.StartCoroutine(TranslateJsonFile(path));
                else
                    Debug.LogWarning("Selected file is not a JSON!");
            }
            else
            {
                Debug.LogWarning("Select a JSON file first!");
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("3. Translate GameObject (Scene/Prefab)", EditorStyles.boldLabel);
        gameObjectAsset = (GameObject)EditorGUILayout.ObjectField("GameObject", gameObjectAsset, typeof(GameObject), true);

        if (GUILayout.Button("Translate GameObject"))
        {
            if (gameObjectAsset != null)
                EditorCoroutineHelper.StartCoroutine(TranslateGameObject(gameObjectAsset));
            else
                Debug.LogWarning("Select a GameObject first!");
        }
    }

    private IEnumerator TranslateSO(ScriptableObject so)
    {
        SerializedObject serializedObj = new SerializedObject(so);
        string targetPropName = GetTargetPropertyName(targetLanguage);
        SerializedProperty iterator = serializedObj.GetIterator();
        bool enterChildren = true;
        
        List<System.Action> applyTranslations = new List<System.Action>();
        
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = true;
            if (iterator.name == "englishText" && iterator.propertyType == SerializedPropertyType.String)
            {
                string originalText = iterator.stringValue;
                if (!string.IsNullOrEmpty(originalText))
                {
                    string targetPath = iterator.propertyPath.Replace("englishText", targetPropName);
                    SerializedProperty targetProp = serializedObj.FindProperty(targetPath);
                    
                    if (targetProp != null && targetProp.propertyType == SerializedPropertyType.String)
                    {
                        if (string.IsNullOrEmpty(targetProp.stringValue))
                        {
                            yield return TranslateText(originalText, targetLanguage, (translated) => {
                                applyTranslations.Add(() => {
                                    SerializedObject sObj = new SerializedObject(so);
                                    SerializedProperty tProp = sObj.FindProperty(targetPath);
                                    if (tProp != null) {
                                        tProp.stringValue = translated;
                                        sObj.ApplyModifiedProperties();
                                    }
                                });
                            });
                        }
                    }
                }
            }
        }
        
        foreach (var action in applyTranslations) action();
            
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        Debug.Log("ScriptableObject " + so.name + " Translated!");
    }

    private IEnumerator TranslateGameObject(GameObject go)
    {
        MonoBehaviour[] components = go.GetComponentsInChildren<MonoBehaviour>(true);
        int totalTranslated = 0;

        foreach (var comp in components)
        {
            if (comp == null) continue;
            
            SerializedObject serializedObj = new SerializedObject(comp);
            string targetPropName = GetTargetPropertyName(targetLanguage);
            SerializedProperty iterator = serializedObj.GetIterator();
            bool enterChildren = true;
            
            List<System.Action> applyTranslations = new List<System.Action>();
            
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.name == "englishText" && iterator.propertyType == SerializedPropertyType.String)
                {
                    string originalText = iterator.stringValue;
                    if (!string.IsNullOrEmpty(originalText))
                    {
                        string targetPath = iterator.propertyPath.Replace("englishText", targetPropName);
                        SerializedProperty targetProp = serializedObj.FindProperty(targetPath);
                        
                        if (targetProp != null && targetProp.propertyType == SerializedPropertyType.String)
                        {
                            if (string.IsNullOrEmpty(targetProp.stringValue))
                            {
                                yield return TranslateText(originalText, targetLanguage, (translated) => {
                                    applyTranslations.Add(() => {
                                        SerializedObject sObj = new SerializedObject(comp);
                                        SerializedProperty tProp = sObj.FindProperty(targetPath);
                                        if (tProp != null) {
                                            tProp.stringValue = translated;
                                            sObj.ApplyModifiedProperties();
                                        }
                                    });
                                });
                            }
                        }
                    }
                }
            }
            
            foreach (var action in applyTranslations) action();
            
            if (applyTranslations.Count > 0)
            {
                totalTranslated += applyTranslations.Count;
                EditorUtility.SetDirty(comp);
            }
        }
        
        Debug.Log($"GameObject {go.name} Translated! ({totalTranslated} fields updated)");
    }

    private IEnumerator TranslateJsonFile(string path)
    {
        string json = File.ReadAllText(path);
        JSONReplicaFile parsed = JsonUtility.FromJson<JSONReplicaFile>(json);
        bool modified = false;

        if (parsed.replicas != null)
        {
            foreach (var rep in parsed.replicas)
            {
                if (rep.phrases != null)
                {
                    foreach (var phrase in rep.phrases)
                    {
                        if (phrase.text != null && !string.IsNullOrEmpty(phrase.text.englishText))
                        {
                            string currentTarget = GetTargetText(phrase.text, targetLanguage);
                            if (string.IsNullOrEmpty(currentTarget))
                            {
                                string translated = "";
                                yield return TranslateText(phrase.text.englishText, targetLanguage, res => translated = res);
                                SetTargetText(phrase.text, targetLanguage, translated);
                                modified = true;
                            }
                        }
                    }
                }
            }
        }

        if (parsed.choices != null)
        {
            foreach (var choice in parsed.choices)
            {
                if (choice.buttonText != null && !string.IsNullOrEmpty(choice.buttonText.englishText))
                {
                    string currentTarget = GetTargetText(choice.buttonText, targetLanguage);
                    if (string.IsNullOrEmpty(currentTarget))
                    {
                        string translated = "";
                        yield return TranslateText(choice.buttonText.englishText, targetLanguage, res => translated = res);
                        SetTargetText(choice.buttonText, targetLanguage, translated);
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            string newJson = JsonUtility.ToJson(parsed, true);
            File.WriteAllText(path, newJson);
            AssetDatabase.Refresh();
            Debug.Log("JSON File Translated: " + path);
        }
        else
        {
            Debug.Log("No new strings to translate in JSON.");
        }
    }

    private string GetTargetPropertyName(string lang)
    {
        if (lang == "tr") return "turkishText";
        if (lang == "ru") return "russianText";
        if (lang == "es") return "spanishText";
        return lang + "Text";
    }

    private string GetTargetText(LocalizedLine line, string lang)
    {
        if (lang == "tr") return line.turkishText;
        if (lang == "ru") return line.russianText;
        if (lang == "es") return line.spanishText;
        return "";
    }

    private void SetTargetText(LocalizedLine line, string lang, string text)
    {
        if (lang == "tr") line.turkishText = text;
        else if (lang == "ru") line.russianText = text;
        else if (lang == "es") line.spanishText = text;
    }

    private IEnumerator TranslateText(string sourceText, string targetLang, System.Action<string> onResult)
    {
        string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl={targetLang}&dt=t&q={UnityWebRequest.EscapeURL(sourceText)}";
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = req.downloadHandler.text;
                    string translated = "";
                    
                    bool inString = false;
                    bool escapeNext = false;
                    int arrayDepth = 0;
                    int itemIndex = 0;
                    System.Text.StringBuilder currentString = new System.Text.StringBuilder();
                    
                    for (int i = 0; i < jsonResponse.Length; i++)
                    {
                        char c = jsonResponse[i];
                        
                        if (escapeNext)
                        {
                            currentString.Append(c);
                            escapeNext = false;
                            continue;
                        }
                        
                        if (c == '\\')
                        {
                            escapeNext = true;
                            currentString.Append(c);
                            continue;
                        }
                        
                        if (c == '"')
                        {
                            inString = !inString;
                            if (!inString && arrayDepth == 3 && itemIndex == 0)
                            {
                                string s = currentString.ToString();
                                s = System.Text.RegularExpressions.Regex.Unescape(s);
                                translated += s;
                            }
                            if (!inString) 
                                currentString.Length = 0;
                            continue;
                        }
                        
                        if (!inString)
                        {
                            if (c == '[') {
                                arrayDepth++;
                                if (arrayDepth == 3) itemIndex = 0;
                            }
                            else if (c == ']') {
                                arrayDepth--;
                                if (arrayDepth < 2) break;
                            }
                            else if (c == ',') {
                                if (arrayDepth == 3) itemIndex++;
                            }
                        }
                        else
                        {
                            if (arrayDepth == 3 && itemIndex == 0)
                            {
                                currentString.Append(c);
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(translated))
                    {
                        Debug.Log($"Translated '{sourceText.Length}' chars -> '{translated.Length}' chars");
                        onResult?.Invoke(translated);
                    }
                    else
                    {
                        Debug.LogError("Translation parse result was empty: " + jsonResponse);
                        onResult?.Invoke("ERROR_PARSING_EMPTY");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Translation parse error: " + e.Message);
                    onResult?.Invoke("ERROR");
                }
            }
            else
            {
                Debug.LogError("Translation request error: " + req.error);
                onResult?.Invoke("NETWORK_ERROR");
            }
        }
    }
}

// Simple Coroutine helper to run IEnumerator in Editor without needing MonoBehaviour
public static class EditorCoroutineHelper
{
    public static void StartCoroutine(IEnumerator routine)
    {
        Stack<IEnumerator> stack = new Stack<IEnumerator>();
        stack.Push(routine);

        EditorApplication.CallbackFunction updateCallback = null;
        updateCallback = () =>
        {
            if (stack.Count > 0)
            {
                if (stack.Peek().Current is AsyncOperation op && !op.isDone) return;

                IEnumerator current = stack.Peek();
                if (current.MoveNext())
                {
                    if (current.Current is IEnumerator nested)
                    {
                        stack.Push(nested);
                    }
                }
                else
                {
                    stack.Pop();
                }
            }

            if (stack.Count == 0)
            {
                EditorApplication.update -= updateCallback;
            }
        };
        EditorApplication.update += updateCallback;
    }
}
