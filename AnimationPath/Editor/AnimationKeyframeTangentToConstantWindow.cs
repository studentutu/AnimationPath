using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorAnimationPreview
{
	public class AnimationKeyframeTangentToConstantWindow : EditorWindow
	{
		public static Action<AnimationClip, AnimationClip> onClipCopyModify;

		[MenuItem("Tool/Animation instant frame tool")]
		public static void Init()
		{
			EditorWindow editorWindow =
				GetWindow<AnimationKeyframeTangentToConstantWindow>(true, "Animation instant cut frame tool");
			editorWindow.minSize = new Vector2(160f, 30f);
			editorWindow.maxSize = new Vector2(editorWindow.minSize.x, editorWindow.minSize.y);
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Instant cut", "LargeButton", GUILayout.Width(150f)))
			{
				DoKeyframeTangentToConstant();
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DoKeyframeTangentToConstant()
		{
			AnimationWindowReflect animationWindowReflect = AnimationWindowUtil.GetAnimationWindowReflect();
			if (!animationWindowReflect.firstAnimationWindow)
			{
				SimpleDisplayDialog("Animation window is not open");
				return;
			}

			AnimationClip activeAnimationClip = animationWindowReflect.activeAnimationClip;
			if (activeAnimationClip == null)
			{
				SimpleDisplayDialog("Animation window doesn't have any animation clips");
				return;
			}

			float currentTime = animationWindowReflect.currentTime;
			if ((activeAnimationClip.hideFlags & HideFlags.NotEditable) != HideFlags.None)
			{
				// FBX Animations are automatically copied
				AnimationClip oldClip = activeAnimationClip;
				activeAnimationClip = CopyAnimationClipAsset(activeAnimationClip);
				if (onClipCopyModify != null)
				{
					onClipCopyModify(oldClip, activeAnimationClip);
				}
			}

			KeyframeTangentToConstant(activeAnimationClip, currentTime);
			animationWindowReflect.firstAnimationWindow.Repaint();
		}

		private static void SimpleDisplayDialog(string text)
		{
			EditorUtility.DisplayDialog("Hint", text, "Fix");
		}

		/// <summary>
		/// 参照 ProjectWindowUtil.DuplicateSelectedAssets
		/// </summary>
		/// <param name="clip"></param>
		/// <returns></returns>
		private static AnimationClip CopyAnimationClipAsset(AnimationClip clip)
		{
			string assetPath = AssetDatabase.GetAssetPath(clip);
			string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(assetPath),
				Path.GetFileNameWithoutExtension(assetPath)) + ".anim");
			AnimationClip animationClip2 = new AnimationClip();
			EditorUtility.CopySerialized(clip, animationClip2);
			AssetDatabase.CreateAsset(animationClip2, path);
			AssetDatabase.ImportAsset(path);

			if (Selection.activeObject == clip)
			{
				Selection.activeObject = animationClip2;
			}

			return animationClip2;
		}

		private static void KeyframeTangentToConstant(AnimationClip clip, float time)
		{
			Undo.RegisterCompleteObjectUndo(clip, "Keyframe Tangent To Constant");
			EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
			SetInterpolation(clip, curveBindings, Mode.RawEuler);
			curveBindings = AnimationUtility.GetCurveBindings(clip);
			foreach (var curveBinding in curveBindings)
			{
				AnimationCurve animationCurve = AnimationUtility.GetEditorCurve(clip, curveBinding);
				for (var i = 0; i < animationCurve.keys.Length; i++)
				{
					var keyframe = animationCurve.keys[i];
					if (Mathf.Approximately(keyframe.time, time))
					{
						AnimationUtility.SetKeyRightTangentMode(animationCurve, i,
							AnimationUtility.TangentMode.Constant);
					}
				}

				AnimationUtility.SetEditorCurve(clip, curveBinding, animationCurve);
			}
		}

		private enum Mode
		{
			Baked,
			NonBaked,
			RawQuaternions,
			RawEuler,
			Undefined,
		}

		private static bool IsTransformType(System.Type type)
		{
			return type == typeof(Transform) || type == typeof(RectTransform);
		}

		private static Mode GetModeFromCurveData(EditorCurveBinding data)
		{
			if (IsTransformType(data.type) && data.propertyName.StartsWith("localEulerAngles"))
			{
				if (data.propertyName.StartsWith("localEulerAnglesBaked"))
					return Mode.Baked;
				return data.propertyName.StartsWith("localEulerAnglesRaw") ? Mode.RawEuler : Mode.NonBaked;
			}

			return IsTransformType(data.type) && data.propertyName.StartsWith("m_LocalRotation")
				? Mode.RawQuaternions
				: Mode.Undefined;
		}

		private static string GetPrefixForInterpolation(Mode newInterpolationMode)
		{
			if (newInterpolationMode == Mode.Baked)
				return "localEulerAnglesBaked";
			if (newInterpolationMode == Mode.NonBaked)
				return "localEulerAngles";
			if (newInterpolationMode == Mode.RawEuler)
				return "localEulerAnglesRaw";
			if (newInterpolationMode == Mode.RawQuaternions)
				return "m_LocalRotation";
			return null;
		}

		private static EditorCurveBinding RemapAnimationBindingForRotationCurves(
			EditorCurveBinding curveBinding, AnimationClip clip)
		{
			if (!IsTransformType(curveBinding.type))
				return curveBinding;
			Mode modeFromCurveData = GetModeFromCurveData(curveBinding);
			if (modeFromCurveData == Mode.Undefined)
				return curveBinding;
			string str = curveBinding.propertyName.Split('.')[1];
			EditorCurveBinding binding = curveBinding;
			if (modeFromCurveData != Mode.NonBaked)
			{
				binding.propertyName = GetPrefixForInterpolation(Mode.NonBaked) + "." + str;
				if (AnimationUtility.GetEditorCurve(clip, binding) != null)
					return binding;
			}

			if (modeFromCurveData != Mode.Baked)
			{
				binding.propertyName = GetPrefixForInterpolation(Mode.Baked) + "." + str;
				if (AnimationUtility.GetEditorCurve(clip, binding) != null)
					return binding;
			}

			if (modeFromCurveData != Mode.RawEuler)
			{
				binding.propertyName = GetPrefixForInterpolation(Mode.RawEuler) + "." + str;
				if (AnimationUtility.GetEditorCurve(clip, binding) != null)
					return binding;
			}

			return curveBinding;
		}

		/// <summary>
		/// 参照 RotationCurveInterpolation.SetInterpolation
		/// </summary>
		/// <param name="clip"></param>
		private static void SetInterpolation(AnimationClip clip, EditorCurveBinding[] curveBindings,
			Mode newInterpolationMode)
		{
			List<EditorCurveBinding> list1 = new List<EditorCurveBinding>();
			List<AnimationCurve> list2 = new List<AnimationCurve>();
			List<EditorCurveBinding> list3 = new List<EditorCurveBinding>();
			foreach (var curveBinding in curveBindings)
			{
				EditorCurveBinding editorCurveBinding = RemapAnimationBindingForRotationCurves(curveBinding, clip);
				switch (GetModeFromCurveData(editorCurveBinding))
				{
					case Mode.Undefined:
						break;
					case Mode.RawQuaternions:
						break;
					default:
						AnimationCurve editorCurve = AnimationUtility.GetEditorCurve(clip, editorCurveBinding);
						if (editorCurve != null)
						{
							string propertyName = editorCurveBinding.propertyName;
							string str = GetPrefixForInterpolation(newInterpolationMode) + '.' +
							             propertyName[propertyName.Length - 1];
							list1.Add(new EditorCurveBinding()
							{
								propertyName = str,
								type = editorCurveBinding.type,
								path = editorCurveBinding.path
							});
							list2.Add(editorCurve);
							list3.Add(new EditorCurveBinding()
							{
								propertyName = editorCurveBinding.propertyName,
								type = editorCurveBinding.type,
								path = editorCurveBinding.path
							});
						}

						break;
				}
			}

			foreach (EditorCurveBinding binding in list3)
				AnimationUtility.SetEditorCurve(clip, binding, null);
			foreach (EditorCurveBinding binding in list1)
				AnimationUtility.SetEditorCurve(clip, binding, list2[list1.IndexOf(binding)]);
		}
	}
}