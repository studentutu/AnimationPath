using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace EditorAnimationPreview
{
	public class AnimationWindowReflection
	{
		private Assembly m_Assembly;
		private Type m_TypeAnimEditor;
		private Type m_TypeAnimationWindowState;
		private Type m_TypeAnimationWindowSelection;
		private AnimationWindow m_AnimationWindow;

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

		private Assembly EditorAssembly
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


		private Type AnimationWindowStateType
		{
			get
			{
				if (m_TypeAnimationWindowState == null)
				{
					m_TypeAnimationWindowState = EditorAssembly.GetType("UnityEditorInternal.AnimationWindowState");
				}

				return m_TypeAnimationWindowState;
			}
		}

		private Type AnimationWindowSelectionType
		{
			get
			{
				if (m_TypeAnimationWindowSelection == null)
				{
					m_TypeAnimationWindowSelection =
						EditorAssembly.GetType("UnityEditorInternal.AnimationWindowSelection");
				}

				return m_TypeAnimationWindowSelection;
			}
		}

		/// <summary>
		/// Get the first animation window
		/// </summary>
		public AnimationWindow EditorAnimationWindow
		{
			get
			{
				if (m_AnimationWindow == null)
				{
					var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
					foreach (var window in windows)
					{
						if (window is AnimationWindow)
						{
							m_AnimationWindow = window as AnimationWindow;
							break;
						}
					}
				}

				return m_AnimationWindow;
			}
		}

		private object AnimationWindowState
		{
			get
			{
				if (m_AnimationWindowState == null)
				{
					var animationWindowStateInfo = m_AnimationWindow.GetType()
						.GetProperty("state", BindingFlags.Instance | BindingFlags.NonPublic);

					if (animationWindowStateInfo != null)
					{
						m_AnimationWindowState = animationWindowStateInfo.GetValue(m_AnimationWindow);
					}
				}

				return m_AnimationWindowState;
			}
		}


		private object AnimationWindowSelection
		{
			get
			{
				if (m_AnimationWindowSelection == null)
				{
					PropertyInfo selectionInfo =
						AnimationWindowStateType.GetProperty("selection",
							BindingFlags.Instance | BindingFlags.Public);
					if (AnimationWindowState != null)
					{
						m_AnimationWindowSelection = selectionInfo.GetValue(AnimationWindowState, null);
					}
				}

				return m_AnimationWindowSelection;
			}
		}

		private object AnimationWindowSelectionItem
		{
			get
			{
				if (m_AnimationWindowSelectionItem == null)
				{
					PropertyInfo selectionInfo = AnimationWindowStateType.GetProperty("selectedItem",
						BindingFlags.Instance | BindingFlags.Public);
					if (AnimationWindowState != null)
					{
						m_AnimationWindowSelectionItem = selectionInfo.GetValue(AnimationWindowState, null);
					}
				}

				return m_AnimationWindowSelectionItem;
			}
		}

		/// <summary>
		/// whether the animation is playing in window
		/// </summary>
		public bool Playing
		{
			get { return EditorAnimationWindow.playing; }
		}

		/// <summary>
		/// Whether an animation is being recorded in window
		/// </summary>
		public bool Recording
		{
			get { return EditorAnimationWindow.recording; }
		}

		/// <summary>
		/// current recorded time
		/// </summary>
		public float CurrentTime
		{
			get { return EditorAnimationWindow.time; }
		}

		/// <summary>
		/// The animation root node object of the current object
		/// </summary>
		public GameObject ActiveRootGameObject
		{
			get
			{
				m_activeRootGameObjectInfo = AnimationWindowStateType
					.GetProperty("activeRootGameObject", BindingFlags.Instance | BindingFlags.Public)
					?.GetValue(AnimationWindowState) as GameObject;


				return m_activeRootGameObjectInfo;
			}
		}


		/// <summary>
		/// The currently active animation clip
		/// </summary>
		public AnimationClip ActiveAnimationClip
		{
			get { return EditorAnimationWindow.animationClip; }
		}

		private FieldInfo OnClipSelectionChangedInfo
		{
			get
			{
				if (m_onClipSelectionChangedInfo == null)
				{
					m_onClipSelectionChangedInfo =
						AnimationWindowSelectionType.GetField("onSelectionChanged",
							BindingFlags.Instance | BindingFlags.Public);
				}

				return m_onClipSelectionChangedInfo;
			}
		}

		private FieldInfo OnFrameRateChangeInfo
		{
			get
			{
				if (m_onFrameRateChangeInfo == null)
				{
					m_onFrameRateChangeInfo = AnimationWindowStateType.GetField("onFrameRateChange",
						BindingFlags.Instance | BindingFlags.Public);
				}

				return m_onFrameRateChangeInfo;
			}
		}

		/// <summary>
		/// animation clip switching event
		/// </summary>
		public Action OnClipSelectionChanged
		{
			get { return (Action) OnClipSelectionChangedInfo.GetValue(AnimationWindowSelection); }
			set
			{
				OnClipSelectionChangedInfo.SetValue(AnimationWindowSelection, value);
				m_AnimationWindowSelectionItem = null;
				m_AnimationWindowSelection = null;
				m_AnimationWindowState = null;
			}
		}

		public Action<float> OnFrameRateChange
		{
			get { return (Action<float>) OnFrameRateChangeInfo.GetValue(AnimationWindowState); }
			set { OnFrameRateChangeInfo.SetValue(AnimationWindowState, value); }
		}
	}
}