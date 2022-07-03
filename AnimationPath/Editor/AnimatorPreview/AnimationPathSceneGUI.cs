using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace EditorAnimationPreview
{
	[InitializeOnLoad]
	public class AnimationPathSceneUI
	{
		public static GameObject activeGameObject;
		public static AnimationClip activeAnimationClip;
		private static GameObject activeRootGameObject;
		private static Transform activeParentTransform;
		private static List<AnimationPathPoint> animationPoints;
		private static bool reloadPointsInfo;

		public static bool Preview_Enabled = false;
		private const string MenuItemString = "Tools/Toggle Animation Preview";

		static AnimationPathSceneUI()
		{
			Preview_Enabled = EditorPrefs.GetBool(MenuItemString, false);

			// Delaying until first editor tick so that the menu
			// will be populated before setting check state, and
			// re-apply correct action
			EditorApplication.delayCall += () => { PerformAction(Preview_Enabled); };
		}

		private static void PerformAction(bool isToggled)
		{
			// Set checkmark on menu item
			Menu.SetChecked(MenuItemString, isToggled);
			// Saving editor state
			EditorPrefs.SetBool(MenuItemString, isToggled);

			Preview_Enabled = isToggled;
			SceneView.onSceneGUIDelegate -= RenderSceneGUI;
			if (Preview_Enabled)
			{
				SceneView.onSceneGUIDelegate += RenderSceneGUI;
			}
		}

		[MenuItem(MenuItemString)]
		public static void TogglePreviewGui()
		{
			PerformAction(!Preview_Enabled);
		}

		private static void RenderSceneGUI(SceneView sceneview)
		{
			if (Preview_Enabled)
			{
				if (Selection.activeGameObject != null)
				{
					OpenSceneTool(Selection.activeGameObject);
				}
				else
				{
					CloseSceneTool();
					return;
				}

				DrawSceneViewGUI();
			}
		}

		private static void OpenSceneTool(GameObject go)
		{
			activeGameObject = go;

			CloseSceneTool();
			activeAnimationClip = AnimationWindowUsage.GetActiveAnimationClip();
			if (activeAnimationClip == null)
			{
				return;
			}

			InitPointsInfo();
			AnimationWindowUsage.SetOnFrameRateChange(OnClipSelectionChanged);
			AnimationUtility.onCurveWasModified -= OnCurveWasModified;
			AnimationUtility.onCurveWasModified += OnCurveWasModified;
		}

		private static void CloseSceneTool()
		{
			AnimationWindowUsage.SetOnFrameRateChange(OnClipSelectionChanged, true);
			AnimationUtility.onCurveWasModified -= OnCurveWasModified;
		}

		private static void InitPointsInfo()
		{
			if (animationPoints == null)
			{
				animationPoints = new List<AnimationPathPoint>();
			}

			animationPoints.Clear();

			String inPath = String.Empty;
			activeRootGameObject = activeGameObject;
			var findAnimator = activeGameObject.GetComponentInParent<Animator>();
			findAnimator = findAnimator == null ? activeGameObject.GetComponent<Animator>() : findAnimator;
			var findAnimation = activeGameObject.GetComponentInParent<Animation>();
			findAnimation = findAnimation == null ? activeGameObject.GetComponent<Animation>() : findAnimation;

			if (findAnimator == null && findAnimation == null)
			{
				return;
			}

			activeRootGameObject = findAnimator == null ? findAnimation.gameObject : findAnimator.gameObject;
			inPath = AnimationUtility.CalculateTransformPath(activeGameObject.transform,
				activeRootGameObject.transform);

			activeParentTransform = activeGameObject.transform.parent;

			Type inType = typeof(Transform);
			AnimationCurve curveX = AnimationUtility.GetEditorCurve(activeAnimationClip,
				EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.x"));
			AnimationCurve curveY = AnimationUtility.GetEditorCurve(activeAnimationClip,
				EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.y"));
			AnimationCurve curveZ = AnimationUtility.GetEditorCurve(activeAnimationClip,
				EditorCurveBinding.FloatCurve(inPath, inType, "m_LocalPosition.z"));
			Vector3 initPosition = activeRootGameObject.transform.localPosition;

			if (curveX == null || curveY == null || curveZ == null)
			{
				// There may be UI animations
				var rt = activeRootGameObject.transform.GetComponent<RectTransform>();
				if (rt)
				{
					inType = typeof(RectTransform);
					curveX = AnimationUtility.GetEditorCurve(activeAnimationClip,
						EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.x"));
					curveY = AnimationUtility.GetEditorCurve(activeAnimationClip,
						EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.y"));
					curveZ = AnimationUtility.GetEditorCurve(activeAnimationClip,
						EditorCurveBinding.FloatCurve(inPath, inType, "m_AnchoredPosition.z"));
					initPosition = rt.anchoredPosition;

					if (curveX == null && curveY == null && curveZ == null)
					{
						return;
					}
				}
				else
				{
					return;
				}
			}

			animationPoints = AnimationPathPoint.MakePoints(curveX, curveY, curveZ, initPosition);
		}

		private static void DrawSceneViewGUI()
		{
			if (reloadPointsInfo)
			{
				reloadPointsInfo = false;
				int num = animationPoints.Count;
				InitPointsInfo();
			}

			if (activeGameObject == null)
			{
				return;
			}

			List<AnimationPathPoint> points = animationPoints;
			if (points == null)
			{
				return;
			}

			int numPos = points.Count;
			for (int i = 0; i < numPos; i++)
			{
				AnimationPathPoint pathPoint = points[i];
				pathPoint.worldPosition = GetWorldPosition(pathPoint.position);
			}

			for (int i = 0; i < numPos - 1; i++)
			{
				AnimationPathPoint pathPoint = points[i];
				AnimationPathPoint nextPathPoint = points[i + 1];
				Vector3 startTangent;
				Vector3 endTangent;
				AnimationPathPoint.CalcTangents(pathPoint, nextPathPoint, out startTangent, out endTangent);

				Vector3 p0 = pathPoint.worldPosition;
				Vector3 p1 = GetWorldPosition(startTangent);
				Vector3 p2 = GetWorldPosition(endTangent);
				Vector3 p3 = nextPathPoint.worldPosition;

				Handles.DrawBezier(p0, p3, p1, p2, AnimationEditorSettingsData.GetCurrentColor(), null, 2f);

				pathPoint.worldOutTangent = p1;
				nextPathPoint.worldInTangent = p2;
			}
		}

		private static void OnCurveWasModified(AnimationClip clip, EditorCurveBinding binding,
			AnimationUtility.CurveModifiedType type)
		{
			if (!Preview_Enabled && activeAnimationClip != clip)
			{
				return;
			}

			reloadPointsInfo = true;
		}

		private static void OnClipSelectionChanged(float frameRate)
		{
			if (Preview_Enabled)
			{
				activeAnimationClip = AnimationWindowUsage.GetActiveAnimationClip();

				AnimationClip[] clips = AnimationUtility.GetAnimationClips(activeRootGameObject);
				for (int i = 0; i < clips.Length; i++)
				{
					if (clips[i] == activeAnimationClip)
					{
						reloadPointsInfo = true;
						SceneView.RepaintAll();
						return;
					}
				}

				CloseSceneTool();
			}
		}

		private static Vector3 GetWorldPosition(Vector3 localPosition)
		{
			if (activeParentTransform == null)
			{
				return localPosition;
			}

			return activeParentTransform.TransformPoint(localPosition);
		}

		private static Vector3 GetLocalPosition(Vector3 worldPosition)
		{
			if (activeParentTransform == null)
			{
				return worldPosition;
			}

			return activeParentTransform.InverseTransformPoint(worldPosition);
		}
	}
}