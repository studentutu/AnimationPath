using UnityEngine;
using UnityEditor;

namespace EditorAnimationPreview
{
	[CustomEditor(typeof(AnimationPreviewPath))]
	public class AnimationPathEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			AnimationPathSceneUI.OnInspectorGUI((target as MonoBehaviour).gameObject);
		}

		private void OnSceneGUI()
		{
			AnimationPathSceneUI.OnSceneGUI();
		}
	}
}