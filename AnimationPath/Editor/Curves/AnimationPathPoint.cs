using UnityEngine;
using System.Collections.Generic;

namespace EditorAnimationPreview
{
	public class AnimationPathPoint
	{
		public float time;
		public Vector3 position;
		public Vector3 inTangent;
		public Vector3 outTangent;

		public Vector3 worldPosition { get; set; }
		public Vector3 worldInTangent { get; set; }
		public Vector3 worldOutTangent { get; set; }

		public AnimationPathPoint(Keyframe keyframeX, Keyframe keyframeY, Keyframe keyframeZ)
		{
			time = keyframeX.time;
			position = new Vector3(keyframeX.value, keyframeY.value, keyframeZ.value);
			inTangent = new Vector3(keyframeX.inTangent, keyframeY.inTangent, keyframeZ.inTangent);
			outTangent = new Vector3(keyframeX.outTangent, keyframeY.outTangent, keyframeZ.outTangent);
		}

		public static void CalcTangents(AnimationPathPoint pathPoint, AnimationPathPoint nextPathPoint,
			out Vector3 startTangent, out Vector3 endTangent)
		{
			startTangent = pathPoint.position;
			endTangent = nextPathPoint.position;

			float dx = nextPathPoint.time - pathPoint.time;

			startTangent.x += (dx * pathPoint.outTangent.x * 1 / 3);
			startTangent.y += (dx * pathPoint.outTangent.y * 1 / 3);
			startTangent.z += (dx * pathPoint.outTangent.z * 1 / 3);

			endTangent.x -= (dx * nextPathPoint.inTangent.x * 1 / 3);
			endTangent.y -= (dx * nextPathPoint.inTangent.y * 1 / 3);
			endTangent.z -= (dx * nextPathPoint.inTangent.z * 1 / 3);
		}

		public static List<AnimationPathPoint> MakePoints(AnimationCurve curveX, AnimationCurve curveY,
			AnimationCurve curveZ, Vector3 initPosition)
		{
			List<AnimationPathPoint> points = new List<AnimationPathPoint>();
			List<float> times = new List<float>();
			if (curveX != null)
			{
				for (int i = 0; i < curveX.length; i++)
				{
					if (!times.Contains(curveX.keys[i].time))
					{
						times.Add(curveX.keys[i].time);
					}
				}
			}

			if (curveY != null)
			{
				for (int i = 0; i < curveY.length; i++)
				{
					if (!times.Contains(curveY.keys[i].time))
					{
						times.Add(curveY.keys[i].time);
					}
				}
			}

			if (curveZ != null)
			{
				for (int i = 0; i < curveZ.length; i++)
				{
					if (!times.Contains(curveZ.keys[i].time))
					{
						times.Add(curveZ.keys[i].time);
					}
				}
			}

			times.Sort();

			for (int i = 0; i < times.Count; i++)
			{
				float time = times[i];
				AnimationPathPoint pathPoint = new AnimationPathPoint(
					GetKeyframeAtTime(curveX, time, initPosition.x),
					GetKeyframeAtTime(curveY, time, initPosition.y),
					GetKeyframeAtTime(curveZ, time, initPosition.z)
				);
				points.Add(pathPoint);
			}

			return points;
		}

		private static Keyframe GetKeyframeAtTime(AnimationCurve curve, float time, float initVal)
		{
			if (curve == null)
			{
				return new Keyframe(time, initVal);
			}

			for (int j = 0; j < curve.length; j++)
			{
				if (Mathf.Approximately(curve.keys[j].time, time))
				{
					return curve.keys[j];
				}
			}

			float num = 0.0001f;
			float num2 = (curve.Evaluate(time + num) - curve.Evaluate(time - num)) / (num * 2f);
			return new Keyframe(time, curve.Evaluate(time), num2, num2);
		}
	}
}