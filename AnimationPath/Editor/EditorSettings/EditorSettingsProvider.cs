using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorAnimationPreview
{
	public class EditorSettingsProvider
	{
		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			// First parameter is the path in the Settings window.
			// Second parameter is the scope of this setting: it only appears in the Project Settings window.
			var provider = new SettingsProvider("Project/AnimationPathSettings", SettingsScope.Project)
			{
				// By default the last token of the path is used as display name if no label is provided.
				label = "Animation Path Preview",
				// Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
				guiHandler = (searchContext) =>
				{
					var settings = AnimationEditorSettingsData.GetSerializedSettings();
					EditorGUI.BeginChangeCheck();
					var color = AnimationEditorSettingsData.GetCurrentColor();

					var colorFrom = EditorGUILayout.ColorField(new GUIContent("Color for Preview"), color);
					settings.Color = ColorUtility.ToHtmlStringRGBA(colorFrom);
					if (EditorGUI.EndChangeCheck())
					{
						settings.SaveData();
						SceneView.RepaintAll();
					}
				},

				// Populate the search keywords to enable smart search filtering and label highlighting:
				keywords = new HashSet<string>(new[] {"Animation", "Path", "Preview"})
			};

			return provider;
		}
	}
}