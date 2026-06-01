#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable once IdentifierTypo

namespace Neuston.UnusedAssetFinder
{
	public class UnusedAssetFinderWindow : EditorWindow
	{
		List<UnusedAsset> unusedAssets = new List<UnusedAsset>();
		Vector2 scrollPosition;
		int expectedProjectChanges;

		class UnusedAsset
		{
			public string AssetPath { get; }
			public bool IsChecked { set; get; }

			public UnusedAsset(string assetPath)
			{
				AssetPath = assetPath;
			}
		}

		[MenuItem("Tools/t.me filiposin/Удаление неиспользуемых ассетов")]
		public static void FindReferences()
		{
			var window = GetWindow<UnusedAssetFinderWindow>();
			window.Start();
		}

		void Start()
		{
			titleContent.text = "Удаление неиспользуемых ассетов";
		}

		void OnGUI()
		{
			var wordWrapStyle = new GUIStyle(EditorStyles.textArea)
			{
				wordWrap = true
			};

			GUILayout.Label("Этот скрипт сканирует проект и находит ресурсы, которые не упоминаются в сценах и в настройках билда игры или в ресурсах (ПРОВЕРЯЙТЕ ФАЙЛЫ, СКРИПТ РАБОТАЕТ НЕ ТОЧНО И МОЖЕТ УДАЛИТЬ НУЖНЫЕ ФАЙЛЫ)", wordWrapStyle);

			if (GUILayout.Button("Найти неиспользуемые ассеты", GUILayout.Width(220)))
			{
				FindUnusedAssets();
			}

			GUILayout.Space(16);

			if (unusedAssets.Any())
			{
				DrawUnusedAssets();
			}
		}

		void DrawUnusedAssets()
		{
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Выбрать все", GUILayout.Width(120)))
			{
				SetAllAssetsChecked(true);
			}

			if (GUILayout.Button("Удалить выбраное", GUILayout.Width(200)))
			{
				DeleteSelectedAssets();
			}

			GUILayout.EndHorizontal();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			foreach (var unusedAsset in unusedAssets)
			{
				GUILayout.BeginHorizontal();

				var height = GUILayout.Height(18);

				// Checkbox
				unusedAsset.IsChecked = GUILayout.Toggle(unusedAsset.IsChecked, string.Empty, GUILayout.Width(16), height);

				// Object
				var assetPath = unusedAsset.AssetPath;
				var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
				var guiContent = EditorGUIUtility.ObjectContent(null, type);
				string fileName = Path.GetFileName(assetPath);
				guiContent.text = fileName;
				guiContent.tooltip = fileName;
				var before = GUI.skin.button.alignment;
				GUI.skin.button.alignment = TextAnchor.MiddleLeft;
				if (GUILayout.Button(guiContent, GUILayout.Width(240), height))
				{
					EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
				}
				GUI.skin.button.alignment = before;

				// Path
				GUILayout.Label(assetPath, height);

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}

		void SetAllAssetsChecked(bool isChecked)
		{
			foreach (var unusedAsset in unusedAssets)
			{
				unusedAsset.IsChecked = isChecked;
			}
		}

		void DeleteSelectedAssets()
		{
			var assetsToDelete = unusedAssets.Where(a => a.IsChecked).Select(a => a.AssetPath).ToList();
			foreach (string assetPath in assetsToDelete)
			{
				expectedProjectChanges++;
				AssetDatabase.DeleteAsset(assetPath);
				RemoveDeletedAssetFromState(assetPath);
				Debug.Log($"Deleted {assetPath}");
			}
		}

		void RemoveDeletedAssetFromState(string deletedAssetPath)
		{
			unusedAssets.RemoveAll(a => a.AssetPath == deletedAssetPath);
		}

		void FindUnusedAssets()
		{
			unusedAssets = UnusedAssetFinder.FindUnusedAssets().Select(p => new UnusedAsset(p)).ToList();
		}

		void OnProjectChange()
		{
			if (expectedProjectChanges > 0)
			{
				expectedProjectChanges--;
			}
			else
			{
				ClearState();
			}
		}

		void OnDestroy()
		{
			ClearState();
		}

		void ClearState()
		{
			unusedAssets.Clear();
			expectedProjectChanges = 0;
		}
	}
}
