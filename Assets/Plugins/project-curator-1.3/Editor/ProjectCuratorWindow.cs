using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ogxd.ProjectCurator {
	public class ProjectCuratorWindow : EditorWindow, IHasCustomMenu {
		#region Init
		const string WINDOW_NAME = "Project Curator";
		[MenuItem("Window/Project Curator")]
		static void Init() {
			GetWindow<ProjectCuratorWindow>(WINDOW_NAME);
		}
		#endregion//Init


		#region Register callback from Unity editor
		//public ProjectCuratorWindow() {
		//	Selection.selectionChanged += OnSelectionChanged;
		//}

		//void OnSelectionChanged() {
		//	Repaint();
		//}

		[MenuItem("FindReference/Show Dependencies And Referencers", priority = 2)]
		static void ContextShowDependenciesAndReferencers(MenuCommand command) {
			var selectedObject = Selection.activeObject;
			if(selectedObject == null) {
				Debug.LogWarning("You must select an object !!");
				return;
			}
			// set select object and call repaint to let window update content
			selectedObjectToInspect = selectedObject;
			var window = GetWindow<ProjectCuratorWindow>(WINDOW_NAME);
			if(window != null) {
				window.Repaint();
			}
		}
		#endregion//Register callback from Unity editor


		#region Render functions
		///<summary> The selected object to inspect dependencies and referencers </summary>
		static Object selectedObjectToInspect = null;

		///<summary> Need a local var for the GUILayout.BeginScrollView() </summary>
		Vector2 _scrollPosition;
		bool _isDependenciesOpen = true;
		bool _isReferencesOpen = true;

		static GUIStyle titleStyle;
		static GUIStyle TitleStyle => titleStyle ?? (titleStyle = new GUIStyle(EditorStyles.label) { fontSize = 13 });

		static GUIStyle itemStyle;
		static GUIStyle ItemStyle => itemStyle ?? (itemStyle = new GUIStyle(EditorStyles.label) { margin = new RectOffset(32, 0, 0, 0) });

		void OnGUI() {
			if(selectedObjectToInspect == null)
				return;

			//string selectedPath = AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
			string selectedPath = AssetDatabase.GetAssetPath(selectedObjectToInspect);
			if(string.IsNullOrEmpty(selectedPath))
				return;
			// header
			DrawHeaderWithSelectObjectInfo(selectedPath);
			// path not exist, then ignore
			if(Directory.Exists(selectedPath))
				return;

			// check if selected asset is exist in our database (database not get build the first time or just got cleared).
			AssetInfo selectedAssetInfo = ProjectCurator.GetAsset(selectedPath);
			if(selectedAssetInfo == null) {
				DrawWarningAndLetUserRebuildDatabase(selectedPath);
				return;
			}

			// the icon the know asset was included in build or not
			var rect = GUILayoutUtility.GetLastRect();
			var content = new GUIContent(selectedAssetInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack, selectedAssetInfo.IncludedStatus.ToString());
			GUI.Label(new Rect(position.width - 20, rect.y + 1, 16, 16), content);

			// begin the scrollview to render dependencies and referencers
			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
			DrawDependencySession(selectedAssetInfo);
			GUILayout.Space(6);
			DrawReferencerSession(selectedAssetInfo);
			GUILayout.Space(5);
			GUILayout.EndScrollView();

			// delete asset not included in build?
			if(!selectedAssetInfo.IsIncludedInBuild) {
				DrawDeleteNotInBuildAssetSession(selectedPath);
			}
		}

		void DrawHeaderWithSelectObjectInfo(string selectedPath) {
			// Standard spacing to mimic Unity's Inspector header
			GUILayout.Space(2);
			const string HEADER_STYLE = "In BigTitle";
			GUILayout.BeginHorizontal(HEADER_STYLE);
			// draw the icon off selected asset
			GUILayout.Label(AssetDatabase.GetCachedIcon(selectedPath), GUILayout.Width(36), GUILayout.Height(36));
			// then it's path
			GUILayout.BeginVertical();
			GUILayout.Label(Path.GetFileName(selectedPath), TitleStyle);
			// Display directory (without "Assets/" prefix)
			const string PATTERN = "(\\\\.*)$";
			GUILayout.Label(Regex.Match(Path.GetDirectoryName(selectedPath), PATTERN).Value);
			GUILayout.EndVertical();
			//GUILayout.Space(44);
			GUILayout.EndHorizontal();
		}

		void DrawWarningAndLetUserRebuildDatabase(string selectedPath) {
			const string ASSET_FOLDER = "Assets";
			const string REMIND_REBUILD = "You must rebuild database to obtain information on this asset";
			const string WARNING_ICON = "console.warnicon";
			const string REBUILD_DATABASE = "Rebuild Database";
			const string IGNORE_OUT_ASSET_FOLDER = "Project Curator ignores assets that are not in the Asset folder.";
			// we only watching for asset inside Assets/ folder
			if(selectedPath.StartsWith(ASSET_FOLDER)) {
				bool rebuildClicked = DrawHelpBoxWithButton(new GUIContent(REMIND_REBUILD, EditorGUIUtility.IconContent(WARNING_ICON).image), new GUIContent(REBUILD_DATABASE));
				if(rebuildClicked) {
					ProjectCurator.RebuildDatabase();
				}
			}
			else {
				EditorGUILayout.HelpBox(IGNORE_OUT_ASSET_FOLDER, MessageType.Warning);
			}
			return;
		}

		void DrawDependencySession(AssetInfo selectedAssetInfo) {
			_isDependenciesOpen = EditorGUILayout.Foldout(_isDependenciesOpen, $"Dependencies ({selectedAssetInfo.dependencies.Count})");
			// if user fold it out, then render the asset list
			if(_isDependenciesOpen) {
				foreach(var dependency in selectedAssetInfo.dependencies) {
					// draw item, it also a button, when user click to it, set it as current active one
					if(GUILayout.Button(new GUIContent(Path.GetFileName(dependency), dependency), ItemStyle)) {
						//UnityEditor.Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dependency);
						PingObjectAtPath(dependency);
					}
					// draw the asset icon at beginning of the line
					var rect = GUILayoutUtility.GetLastRect();
					GUI.DrawTexture(new Rect(rect.x - 16, rect.y, rect.height, rect.height), AssetDatabase.GetCachedIcon(dependency));
					// draw the icon to know if this asset included in the build or not
					AssetInfo dependencyInfo = ProjectCurator.GetAsset(dependency);
					var content = new GUIContent(dependencyInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack, dependencyInfo.IncludedStatus.ToString());
					GUI.Label(new Rect(rect.width + rect.x - 20, rect.y + 1, 16, 16), content);
				}
			}
		}

		void DrawReferencerSession(AssetInfo selectedAssetInfo) {
			_isReferencesOpen = EditorGUILayout.Foldout(_isReferencesOpen, $"Referencers ({selectedAssetInfo.referencers.Count})");
			// if user fold it out, then render the asset list
			if(_isReferencesOpen) {
				foreach(var referencer in selectedAssetInfo.referencers) {
					// draw item, it also a button, when user click to it, set it as current active one
					if(GUILayout.Button(new GUIContent(Path.GetFileName(referencer), referencer), ItemStyle)) {
						//UnityEditor.Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(referencer);
						PingObjectAtPath(referencer);
					}
					// draw the asset icon at beginning of the line
					var rect = GUILayoutUtility.GetLastRect();
					GUI.DrawTexture(new Rect(rect.x - 16, rect.y, rect.height, rect.height), AssetDatabase.GetCachedIcon(referencer));
					// draw the icon to know if this asset included in the build or not
					AssetInfo refInfo = ProjectCurator.GetAsset(referencer);
					var content = new GUIContent(refInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack, refInfo.IncludedStatus.ToString());
					GUI.Label(new Rect(rect.width + rect.x - 20, rect.y + 1, 16, 16), content);
				}
			}
		}

		void DrawDeleteNotInBuildAssetSession(string selectedPath) {
			const string MESSAGE = "This asset is not referenced and never used. Would you like to delete it ?";
			const string WARNING_ICON = "console.warnicon";
			const string DELETE_TEXT = "Delete Asset";
			bool deleteClicked = DrawHelpBoxWithButton(new GUIContent(MESSAGE, EditorGUIUtility.IconContent(WARNING_ICON).image), new GUIContent(DELETE_TEXT));
			if(deleteClicked) {
				File.Delete(selectedPath);
				AssetDatabase.Refresh();
				ProjectCurator.RemoveAssetFromDatabase(selectedPath);
			}
		}
		#endregion//Render functions


		#region Right click menu context on Window
		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu) {
			menu.AddItem(new GUIContent("Rebuild Database"), false, ProjectCurator.RebuildDatabase);
			menu.AddItem(new GUIContent("Clear Database"), false, ProjectCurator.ClearDatabase);
			menu.AddItem(new GUIContent("Project Overlay"), ProjectWindowOverlay.Enabled, () => { ProjectWindowOverlay.Enabled = !ProjectWindowOverlay.Enabled; });
		}
		#endregion//Right click menu context on Window


		#region Utils
		bool DrawHelpBoxWithButton(GUIContent messageContent, GUIContent buttonContent) {
			float buttonWidth = buttonContent.text.Length * 8;
			const float buttonSpacing = 5f;
			const float buttonHeight = 18f;

			// Reserve size of wrapped text
			Rect contentRect = GUILayoutUtility.GetRect(messageContent, EditorStyles.helpBox);
			// Reserve size of button
			GUILayoutUtility.GetRect(1, buttonHeight + buttonSpacing);

			// Render background box with text at full height
			contentRect.height += buttonHeight + buttonSpacing;
			GUI.Label(contentRect, messageContent, EditorStyles.helpBox);

			// Button (align lower right)
			Rect buttonRect = new Rect(contentRect.xMax - buttonWidth - 4f, contentRect.yMax - buttonHeight - 4f, buttonWidth, buttonHeight);
			return GUI.Button(buttonRect, buttonContent);
		}

		void PingObjectAtPath(string path) {
			var target = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
			if(target != null) {
				EditorGUIUtility.PingObject(target);
			}
			else {
				Debug.LogWarning($"Not found object at path [{path}]");
			}
		}
		#endregion//Utils
	}
}