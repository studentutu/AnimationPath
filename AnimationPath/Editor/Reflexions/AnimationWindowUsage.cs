using System;
using UnityEditor;
using UnityEngine;

namespace EditorAnimationPreview
{
	public static class AnimationWindowUsage
	{
		private static float s_PrevCurrentTime;
		private static Func<float> s_GetCurrentTimeFunc;
		private static Action<float> s_CurrentTimeChange;

		/// <summary>
		/// Register the timeline time change listener for the animation window
		/// Note: only listen to the first animation window
		/// </summary>
		public static void RegisterTimeChangeListener(Action<float> currentTimeChange)
		{
			AnimationWindowReflection animationWindowReflection = GetAnimationWindowReflection();
			if (!animationWindowReflection.EditorAnimationWindow)
			{
				return;
			}

			s_GetCurrentTimeFunc = () => { return animationWindowReflection.CurrentTime; };
			s_PrevCurrentTime = -1f;
			s_CurrentTimeChange = currentTimeChange;
			EditorApplication.update = (EditorApplication.CallbackFunction)
				Delegate.RemoveAll(EditorApplication.update,
					new EditorApplication.CallbackFunction(OnCurrentTimeListening));
			EditorApplication.update = (EditorApplication.CallbackFunction)
				Delegate.Combine(EditorApplication.update,
					new EditorApplication.CallbackFunction(OnCurrentTimeListening));
		}

		/// <summary>
		/// Unregister the timeline time change listener for the animation window
		/// </summary>
		public static void UnRegisterTimeChangeListener()
		{
			EditorApplication.update = (EditorApplication.CallbackFunction)
				Delegate.RemoveAll(EditorApplication.update,
					new EditorApplication.CallbackFunction(OnCurrentTimeListening));
			s_PrevCurrentTime = -1f;
			s_GetCurrentTimeFunc = null;
			s_CurrentTimeChange = null;
		}

		private static void OnCurrentTimeListening()
		{
			float currentTime = -1f;
			if (s_GetCurrentTimeFunc != null)
			{
				currentTime = s_GetCurrentTimeFunc();
			}

			if (!Mathf.Approximately(currentTime, s_PrevCurrentTime))
			{
				s_PrevCurrentTime = currentTime;

				if (s_CurrentTimeChange != null)
				{
					s_CurrentTimeChange(currentTime);
				}
			}
		}


		/// <summary>
		/// Get the currently active animation clip
		/// </summary>
		/// <returns></returns>
		public static AnimationClip GetActiveAnimationClip()
		{
			AnimationWindowReflection animationWindowReflection = GetAnimationWindowReflection();
			if (!animationWindowReflection.EditorAnimationWindow)
			{
				return null;
			}

			return animationWindowReflection.ActiveAnimationClip;
		}

		public static void SetOnFrameRateChange(Action<float> onFrameRateChangeAction, bool removeOnly = false)
		{
			AnimationWindowReflection animationWindowReflection = GetAnimationWindowReflection();
			if (!animationWindowReflection.EditorAnimationWindow)
			{
				return;
			}

			Action<float> onFrameRateChange = animationWindowReflection.OnFrameRateChange;
			onFrameRateChange = (Action<float>) Delegate.RemoveAll(onFrameRateChange, onFrameRateChangeAction);
			if (!removeOnly)
			{
				onFrameRateChange = (Action<float>) Delegate.Combine(onFrameRateChange, onFrameRateChangeAction);
			}

			animationWindowReflection.OnFrameRateChange = onFrameRateChange;
		}

		public static void Repaint()
		{
			AnimationWindowReflection animationWindowReflection = GetAnimationWindowReflection();
			if (!animationWindowReflection.EditorAnimationWindow)
			{
				return;
			}

			animationWindowReflection.EditorAnimationWindow.Repaint();
		}

		public static AnimationWindowReflection GetAnimationWindowReflection()
		{
			AnimationWindowReflection animationWindowReflection = new AnimationWindowReflection();
			return animationWindowReflection;
		}
	}
}