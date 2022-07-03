using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EditorAnimationPreview
{
	[Serializable]
	public class AnimationEditorSettingsData
	{
		private const string AnimationEditorSettingsDataPrefs = "AnimationEditorSettingsDataPrefs";

		/// <summary>
		/// Color as RGB Hex
		/// </summary>
		public string Color;

		private static AnimationEditorSettingsData _settings;

		public static Color GetCurrentColor()
		{
			var settings = GetSerializedSettings();
			if (!settings.Color.StartsWith("#"))
			{
				settings.Color = "#" + settings.Color;
			}

			ColorUtility.TryParseHtmlString(settings.Color, out var color);
			return color;
		}

		public static AnimationEditorSettingsData GetSerializedSettings()
		{
			if (_settings == null)
			{
				_settings = new AnimationEditorSettingsData();
				if (EditorPrefs.HasKey(AnimationEditorSettingsDataPrefs))
				{
					var json = EditorPrefs.GetString(AnimationEditorSettingsDataPrefs);
					_settings = JsonUtility.FromJson<AnimationEditorSettingsData>(json);
					if (!_settings.Color.StartsWith("#"))
					{
						_settings.Color = "#" + _settings.Color;
					}
				}
			}

			return _settings;
		}

		public void SaveData()
		{
			if (!_settings.Color.StartsWith("#"))
			{
				_settings.Color = "#" + _settings.Color;
			}

			EditorPrefs.SetString(AnimationEditorSettingsDataPrefs, JsonUtility.ToJson(_settings));
		}
	}
}