using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorAnimationPreview
{
	public class AnimationKeyframeTangentUtil
	{
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

		private static EditorCurveBinding GetAnimationBindingForRotationCurves(
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
	}
}