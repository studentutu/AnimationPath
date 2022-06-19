using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace EditorAnimationPreview
{
	public class AnimationWindowReflect
	{
		private Assembly m_Assembly;
		private Type m_TypeAnimEditor;
		private Type m_TypeAnimationWindowState;
		private Type m_TypeAnimationWindowSelection;
		private AnimationWindow m_FirstAnimationWindow;

		// The following are the objects for the first animation form
		private object m_AnimEditor;
		private object m_AnimationWindowState;
		private object m_AnimationWindowSelection;
		private object m_AnimationWindowSelectionItem;
		private PropertyInfo m_playingInfo;
		private PropertyInfo m_recordingInfo;
		private PropertyInfo m_currentTimeInfo;
		private GameObject m_activeRootGameObjectInfo;
		private PropertyInfo m_activeAnimationClipInfo;
		private FieldInfo m_onClipSelectionChangedInfo;
		private Func<float> m_CurrentTimeGetFunc;
		private MethodInfo m_ResampleAnimationMethod;
		private MethodInfo m_UpdateClipMethodInfo;
		private MethodInfo m_StartRecordingethodInfo;
		private FieldInfo m_onFrameRateChangeInfo;

		private Assembly assembly
		{
			get
			{
				if (m_Assembly == null)
				{
					m_Assembly = Assembly.GetAssembly(typeof(EditorGUIUtility));
				}

				return m_Assembly;
			}
		}


		private Type animationWindowStateType
		{
			get
			{
				if (m_TypeAnimationWindowState == null)
				{
					m_TypeAnimationWindowState = assembly.GetType("UnityEditorInternal.AnimationWindowState");
				}

				return m_TypeAnimationWindowState;
			}
		}

		private Type animationWindowSelectionType
		{
			get
			{
				if (m_TypeAnimationWindowSelection == null)
				{
					m_TypeAnimationWindowSelection = assembly.GetType("UnityEditorInternal.AnimationWindowSelection");
				}

				return m_TypeAnimationWindowSelection;
			}
		}

		/// <summary>
		/// Get the first animation window
		/// </summary>
		public AnimationWindow firstAnimationWindow
		{
			get
			{
				if (m_FirstAnimationWindow == null)
				{
					m_FirstAnimationWindow = AnimationWindow.GetWindow<AnimationWindow>();
				}

				return m_FirstAnimationWindow;
			}
		}

		private object animationWindowState
		{
			get
			{
				if (m_AnimationWindowState == null)
				{
					var animationWindowStateInfo = m_FirstAnimationWindow.GetType()
						.GetProperty("state", BindingFlags.Instance | BindingFlags.NonPublic);

					if (animationWindowStateInfo != null)
					{
						m_AnimationWindowState = animationWindowStateInfo.GetValue(m_FirstAnimationWindow);
					}
				}

				return m_AnimationWindowState;
			}
		}


		private object animationWindowSelection
		{
			get
			{
				if (m_AnimationWindowSelection == null)
				{
					PropertyInfo selectionInfo =
						animationWindowStateType.GetProperty("selection", BindingFlags.Instance | BindingFlags.Public);
					if (animationWindowState != null)
					{
						m_AnimationWindowSelection = selectionInfo.GetValue(animationWindowState, null);
					}
				}

				return m_AnimationWindowSelection;
			}
		}

		private object animationWindowSelectionItem
		{
			get
			{
				if (m_AnimationWindowSelectionItem == null)
				{
					PropertyInfo selectionInfo = animationWindowStateType.GetProperty("selectedItem",
						BindingFlags.Instance | BindingFlags.Public);
					if (animationWindowState != null)
					{
						m_AnimationWindowSelectionItem = selectionInfo.GetValue(animationWindowState, null);
					}
				}

				return m_AnimationWindowSelectionItem;
			}
		}

		/// <summary>
		/// whether the animation is playing
		/// </summary>
		public bool playing
		{
			get { return firstAnimationWindow.playing; }
		}

		/// <summary>
		/// Whether an animation is being recorded
		/// </summary>
		public bool recording
		{
			get { return firstAnimationWindow.recording; }
		}

		/// <summary>
		/// current recorded time
		/// </summary>
		public float currentTime
		{
			get { return firstAnimationWindow.time; }
		}

		/// <summary>
		/// The animation root node object of the current object
		/// </summary>
		public GameObject activeRootGameObject
		{
			get
			{
				if (m_activeRootGameObjectInfo == null)
				{
					m_activeRootGameObjectInfo = animationWindowStateType
						.GetProperty("activeRootGameObject", BindingFlags.Instance | BindingFlags.Public)
						?.GetValue(animationWindowState) as GameObject;
				}

				return m_activeRootGameObjectInfo;
			}
		}


		/// <summary>
		/// The currently active animation clip
		/// </summary>
		public AnimationClip activeAnimationClip
		{
			get { return firstAnimationWindow.animationClip; }
		}

		private FieldInfo onClipSelectionChangedInfo
		{
			get
			{
				if (m_onClipSelectionChangedInfo == null)
				{
					m_onClipSelectionChangedInfo =
						animationWindowSelectionType.GetField("onSelectionChanged",
							BindingFlags.Instance | BindingFlags.Public);
				}

				return m_onClipSelectionChangedInfo;
			}
		}

		private FieldInfo onFrameRateChangeInfo
		{
			get
			{
				if (m_onFrameRateChangeInfo == null)
				{
					m_onFrameRateChangeInfo = animationWindowStateType.GetField("onFrameRateChange",
						BindingFlags.Instance | BindingFlags.Public);
				}

				return m_onFrameRateChangeInfo;
			}
		}

		/// <summary>
		/// animation clip switching event
		/// </summary>
		public Action onClipSelectionChanged
		{
			get { return (Action) onClipSelectionChangedInfo.GetValue(animationWindowSelection); }
			set { onClipSelectionChangedInfo.SetValue(animationWindowSelection, value); }
		}

		public Action<float> onFrameRateChange
		{
			get { return (Action<float>) onFrameRateChangeInfo.GetValue(animationWindowState); }
			set { onFrameRateChangeInfo.SetValue(animationWindowState, value); }
		}
	}
}