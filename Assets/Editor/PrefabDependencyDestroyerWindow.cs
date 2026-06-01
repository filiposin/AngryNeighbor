#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PrefabDependencyDestroyerWindow : EditorWindow
{
    [Serializable]
    private class DependencyEntry
    {
        public bool selected = true;
        public string path;
        public string name;
        public string typeName;
        public string extension;
        public UnityEngine.Object asset;
        public long fileSizeBytes;
        public int otherUsageCount = -1; // -1 = не проверялось
        public bool usedElsewhere => otherUsageCount > 0;
    }

    private GameObject _prefabAsset;
    private string _prefabPath;
    private bool _deletePrefabAsset = true;
    private bool _moveToTrash = true;
    private bool _showOnlySelected = false;
    private bool _showOnlyUsedElsewhere = false;
    private string _search = "";
    private Vector2 _scroll;

    private int _ignoredNonProjectDependencies;
    private List<DependencyEntry> _entries = new List<DependencyEntry>();

    [MenuItem("Tools/Prefab Dependency Destroyer")]
    public static void Open()
    {
        GetWindow<PrefabDependencyDestroyerWindow>("Prefab Destroyer");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Выбираешь prefab -> Scan Dependencies -> видишь всё, что на нём висит по сериализованным ссылкам -> отмечаешь галками -> удаляешь.\n\n" +
            "Важно: это не поймает ресурсы, которые грузятся строками через Resources.Load, Addressables по ключам, AssetBundle-ключи и прочую магию из кода.",
            MessageType.Warning);

        DrawPrefabSelection();
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_prefabAsset == null))
        {
            DrawTopOptions();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Dependencies", GUILayout.Height(28)))
                {
                    ScanDependencies();
                }

                using (new EditorGUI.DisabledScope(_entries.Count == 0))
                {
                    if (GUILayout.Button("Scan Other Usages In Project", GUILayout.Height(28)))
                    {
                        ScanOtherUsages();
                    }
                }
            }
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(_prefabPath))
        {
            EditorGUILayout.LabelField("Prefab Path", _prefabPath);
        }

        if (_ignoredNonProjectDependencies > 0)
        {
            EditorGUILayout.HelpBox(
                $"Скрыто зависимостей вне папки Assets: {_ignoredNonProjectDependencies} (Packages/ built-in stuff и т.п.). Их это окно не удаляет.",
                MessageType.Info);
        }

        if (_entries.Count > 0)
        {
            DrawToolbar();
            DrawDependencyList();
            DrawDeleteButtons();
        }
    }

    private void DrawPrefabSelection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            _prefabAsset = (GameObject)EditorGUILayout.ObjectField(
                "Prefab",
                _prefabAsset,
                typeof(GameObject),
                false);

            if (GUILayout.Button("Use Selection", GUILayout.Width(110)))
            {
                if (Selection.activeObject is GameObject go)
                    _prefabAsset = go;
                else
                    _prefabAsset = null;
            }
        }

        if (_prefabAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(_prefabAsset);
            bool isValidPrefab =
                !string.IsNullOrEmpty(path) &&
                path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

            if (!isValidPrefab)
            {
                EditorGUILayout.HelpBox("Выбранный объект не является prefab asset из Project view.", MessageType.Error);
            }
        }
    }

    private void DrawTopOptions()
    {
        _deletePrefabAsset = EditorGUILayout.ToggleLeft("Delete prefab asset itself", _deletePrefabAsset);
        _moveToTrash = EditorGUILayout.ToggleLeft("Move to Trash instead of permanent delete", _moveToTrash);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("All", GUILayout.Width(60)))
                SetAllSelected(true);

            if (GUILayout.Button("None", GUILayout.Width(60)))
                SetAllSelected(false);

            if (GUILayout.Button("Invert", GUILayout.Width(70)))
                InvertSelection();

            if (GUILayout.Button("Only Scripts", GUILayout.Width(100)))
                SelectByPredicate(IsScriptAsset);

            if (GUILayout.Button("Only Art/Content", GUILayout.Width(120)))
                SelectByPredicate(IsArtOrContentAsset);

            if (GUILayout.Button("Uncheck Used Elsewhere", GUILayout.Width(170)))
                UncheckUsedElsewhere();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _showOnlySelected = EditorGUILayout.ToggleLeft("Show only selected", _showOnlySelected, GUILayout.Width(150));
            _showOnlyUsedElsewhere = EditorGUILayout.ToggleLeft("Show only used elsewhere", _showOnlyUsedElsewhere, GUILayout.Width(180));
            _search = EditorGUILayout.TextField("Search", _search);
        }

        EditorGUILayout.Space();

        int selectedCount = _entries.Count(e => e.selected);
        int usedElsewhereCount = _entries.Count(e => e.usedElsewhere);
        long selectedSize = _entries.Where(e => e.selected).Sum(e => e.fileSizeBytes);

        EditorGUILayout.LabelField(
            $"Dependencies: {_entries.Count} | Selected: {selectedCount} | Used elsewhere: {usedElsewhereCount} | Selected size: {FormatBytes(selectedSize)}");
    }

    private void DrawDependencyList()
    {
        EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var entry in GetFilteredEntries())
        {
            DrawEntry(entry);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(DependencyEntry entry)
    {
        var oldColor = GUI.color;

        if (IsScriptAsset(entry))
            GUI.color = new Color(1f, 0.85f, 0.85f);
        else if (entry.usedElsewhere)
            GUI.color = new Color(1f, 0.95f, 0.75f);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(18));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(entry.asset, typeof(UnityEngine.Object), false, GUILayout.Width(220));
                }

                GUILayout.Label(entry.typeName, GUILayout.Width(130));
                GUILayout.Label(FormatBytes(entry.fileSizeBytes), GUILayout.Width(80));

                string usageText = entry.otherUsageCount < 0
                    ? "usage: ?"
                    : $"usage: {entry.otherUsageCount}";
                GUILayout.Label(usageText, GUILayout.Width(75));

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(entry.asset);
                    Selection.activeObject = entry.asset;
                }
            }

            EditorGUILayout.LabelField(entry.path, EditorStyles.miniLabel);

            if (IsScriptAsset(entry))
            {
                EditorGUILayout.HelpBox("Это script asset. Если удалишь — он сдохнет во всём проекте, не только на этом prefab.", MessageType.Warning);
            }
            else if (entry.usedElsewhere)
            {
                EditorGUILayout.HelpBox("Похоже, asset ещё где-то используется в проекте.", MessageType.Info);
            }
        }

        GUI.color = oldColor;
    }

    private void DrawDeleteButtons()
    {
        EditorGUILayout.Space();

        int selectedCount = _entries.Count(e => e.selected);
        int totalDeleteCount = selectedCount + (_deletePrefabAsset ? 1 : 0);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Delete", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Will delete: {totalDeleteCount} asset(s)");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.25f);

                if (GUILayout.Button(_moveToTrash ? "Delete Selected (Move To Trash)" : "Delete Selected (Permanent)", GUILayout.Height(34)))
                {
                    DeleteSelected();
                }

                GUI.backgroundColor = Color.white;
            }
        }
    }

    private void ScanDependencies()
    {
        _entries.Clear();
        _ignoredNonProjectDependencies = 0;

        if (_prefabAsset == null)
        {
            ShowNotification(new GUIContent("Выбери prefab"));
            return;
        }

        _prefabPath = AssetDatabase.GetAssetPath(_prefabAsset);

        if (string.IsNullOrEmpty(_prefabPath) || !_prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Ошибка", "Выбранный объект не является prefab asset.", "OK");
            return;
        }

        string[] deps = AssetDatabase.GetDependencies(_prefabPath, true);

        foreach (string depPath in deps.Distinct())
        {
            if (depPath == _prefabPath)
                continue;

            if (!depPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                _ignoredNonProjectDependencies++;
                continue;
            }

            if (Directory.Exists(depPath))
                continue;

            var asset = AssetDatabase.LoadMainAssetAtPath(depPath);
            string ext = Path.GetExtension(depPath).ToLowerInvariant();

            _entries.Add(new DependencyEntry
            {
                selected = true,
                path = depPath,
                name = Path.GetFileNameWithoutExtension(depPath),
                typeName = asset != null ? asset.GetType().Name : ext,
                extension = ext,
                asset = asset,
                fileSizeBytes = GetFileSizeSafe(depPath),
                otherUsageCount = -1
            });
        }

        _entries = _entries
            .OrderByDescending(e => IsScriptAsset(e)) // scripts наверх
            .ThenBy(e => e.typeName)
            .ThenBy(e => e.path)
            .ToList();

        Repaint();
    }

    private void ScanOtherUsages()
    {
        if (_entries.Count == 0 || string.IsNullOrEmpty(_prefabPath))
            return;

        var trackedPaths = new HashSet<string>(_entries.Select(e => e.path));
        var usageCounts = trackedPaths.ToDictionary(p => p, _ => 0);

        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

        try
        {
            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                string assetPath = allAssetPaths[i];

                if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (assetPath == _prefabPath)
                    continue;

                if (Directory.Exists(assetPath))
                    continue;

                EditorUtility.DisplayProgressBar(
                    "Scanning other usages",
                    assetPath,
                    i / (float)allAssetPaths.Length);

                string[] deps;
                try
                {
                    deps = AssetDatabase.GetDependencies(assetPath, true);
                }
                catch
                {
                    continue;
                }

                var matched = new HashSet<string>();

                foreach (string dep in deps)
                {
                    if (trackedPaths.Contains(dep))
                        matched.Add(dep);
                }

                // Не считаем asset как "использующий сам себя"
                if (trackedPaths.Contains(assetPath))
                    matched.Remove(assetPath);

                foreach (var match in matched)
                {
                    usageCounts[match]++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        foreach (var entry in _entries)
        {
            entry.otherUsageCount = usageCounts.TryGetValue(entry.path, out int count) ? count : 0;
        }

        Repaint();
    }

    private void DeleteSelected()
    {
        if (_prefabAsset == null || string.IsNullOrEmpty(_prefabPath))
        {
            EditorUtility.DisplayDialog("Ошибка", "Сначала выбери и проскань prefab.", "OK");
            return;
        }

        var pathsToDelete = new List<string>();

        pathsToDelete.AddRange(_entries.Where(e => e.selected).Select(e => e.path));

        if (_deletePrefabAsset)
            pathsToDelete.Add(_prefabPath);

        pathsToDelete = pathsToDelete
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();

        if (pathsToDelete.Count == 0)
        {
            EditorUtility.DisplayDialog("Нечего удалять", "Ты ничего не выбрал.", "OK");
            return;
        }

        int scriptsCount = pathsToDelete.Count(IsScriptPath);
        int sharedCount = _entries.Count(e => e.selected && e.usedElsewhere);

        string preview = string.Join("\n", pathsToDelete.Take(20));
        if (pathsToDelete.Count > 20)
            preview += "\n...";

        string message =
            $"Будет удалено: {pathsToDelete.Count} asset(s)\n" +
            $"Scripts selected: {scriptsCount}\n" +
            $"Used elsewhere selected: {sharedCount}\n\n" +
            $"Mode: {(_moveToTrash ? "Move To Trash" : "Permanent Delete")}\n\n" +
            preview;

        bool confirmed = EditorUtility.DisplayDialog(
            "Подтверди удаление",
            message,
            "Удалить",
            "Отмена");

        if (!confirmed)
            return;

        var failed = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();

            // Сначала зависимости, потом prefab
            var dependencyPaths = pathsToDelete
                .Where(p => p != _prefabPath)
                .OrderByDescending(p => p.Length)
                .ToList();

            foreach (var path in dependencyPaths)
            {
                if (!DeleteAssetPath(path))
                    failed.Add(path);
            }

            if (_deletePrefabAsset)
            {
                if (!DeleteAssetPath(_prefabPath))
                    failed.Add(_prefabPath);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (_deletePrefabAsset)
        {
            _prefabAsset = null;
            _prefabPath = null;
            _entries.Clear();
        }
        else
        {
            ScanDependencies();
        }

        if (failed.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Готово, но не всё",
                $"Не удалось удалить {failed.Count} asset(s):\n\n{string.Join("\n", failed.Take(15))}",
                "OK");
        }
        else
        {
            ShowNotification(new GUIContent("Удаление завершено"));
        }
    }

    private bool DeleteAssetPath(string path)
    {
        if (_moveToTrash)
            return AssetDatabase.MoveAssetToTrash(path);

        return AssetDatabase.DeleteAsset(path);
    }

    private IEnumerable<DependencyEntry> GetFilteredEntries()
    {
        IEnumerable<DependencyEntry> result = _entries;

        if (_showOnlySelected)
            result = result.Where(e => e.selected);

        if (_showOnlyUsedElsewhere)
            result = result.Where(e => e.usedElsewhere);

        if (!string.IsNullOrWhiteSpace(_search))
        {
            string s = _search.Trim().ToLowerInvariant();
            result = result.Where(e =>
                e.path.ToLowerInvariant().Contains(s) ||
                e.name.ToLowerInvariant().Contains(s) ||
                e.typeName.ToLowerInvariant().Contains(s));
        }

        return result;
    }

    private void SetAllSelected(bool value)
    {
        foreach (var entry in _entries)
            entry.selected = value;
    }

    private void InvertSelection()
    {
        foreach (var entry in _entries)
            entry.selected = !entry.selected;
    }

    private void SelectByPredicate(Func<DependencyEntry, bool> predicate)
    {
        foreach (var entry in _entries)
            entry.selected = predicate(entry);
    }

    private void UncheckUsedElsewhere()
    {
        foreach (var entry in _entries)
        {
            if (entry.usedElsewhere)
                entry.selected = false;
        }
    }

    private static bool IsScriptAsset(DependencyEntry entry)
    {
        return IsScriptPath(entry.path);
    }

    private static bool IsScriptPath(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".cs" || ext == ".js" || ext == ".boo" || ext == ".asmdef";
    }

    private static bool IsArtOrContentAsset(DependencyEntry entry)
    {
        string ext = entry.extension;
        switch (ext)
        {
            case ".mat":
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".tga":
            case ".psd":
            case ".tif":
            case ".tiff":
            case ".gif":
            case ".bmp":
            case ".exr":
            case ".cubemap":
            case ".fbx":
            case ".obj":
            case ".blend":
            case ".anim":
            case ".controller":
            case ".overridecontroller":
            case ".playable":
            case ".asset":
            case ".shader":
            case ".shadergraph":
            case ".compute":
            case ".wav":
            case ".mp3":
            case ".ogg":
            case ".aif":
            case ".aiff":
            case ".mp4":
            case ".mov":
                return true;
            default:
                return false;
        }
    }

    private static long GetFileSizeSafe(string assetPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath))
                return new FileInfo(fullPath).Length;
        }
        catch
        {
            // ignored
        }

        return 0;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
            return "-";

        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024f && order < sizes.Length - 1)
        {
            order++;
            len /= 1024f;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
#endif