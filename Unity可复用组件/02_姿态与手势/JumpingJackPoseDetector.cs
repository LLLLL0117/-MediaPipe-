using UnityEngine;

namespace EchoWorkSpace
{
    public enum JumpingJackPose
    {
        Unknown,
        FeetTogetherHandsDown,
        FeetApartHandsUp,
    }

    /// <summary>只负责把 MediaPipe 关节点转换为开合跳姿势。</summary>
    public class JumpingJackPoseDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PoseLandmarkerJointReceiver _poseReceiver;

        [Header("Feet (ankle distance / shoulder width)")]
        [SerializeField, Min(0.1f)] private float _feetTogetherMaxRatio = 0.9f;
        [SerializeField, Min(0.1f)] private float _feetApartMinRatio = 1.45f;

        [Header("Hands (relative to torso height)")]
        [Tooltip("举手时手腕需要高于同侧肩膀的比例。")]
        [SerializeField, Min(0f)] private float _handsUpAboveShoulderRatio = 0.12f;
        [Tooltip("放手时手腕需要低于同侧肩膀的比例。")]
        [SerializeField, Min(0f)] private float _handsDownBelowShoulderRatio = 0.35f;

        [Header("判定选项")]
        [Tooltip("勾选后，开合跳姿势需要同时满足腿部和手部条件；取消勾选后只判断手部上下动作。")]
        [SerializeField] private bool _enableLegCheck = true;

        [Header("Tracking")]
        [SerializeField, Range(0f, 1f)] private float _minimumConfidence = 0.45f;

        public PoseLandmarkerJointReceiver PoseReceiver => _poseReceiver;

        public bool TryGetPose(out JumpingJackPose pose)
        {
            pose = JumpingJackPose.Unknown;
            if (_poseReceiver == null || !_poseReceiver.IsTracked || _poseReceiver.PoseCount < 1)
                return false;

            PoseJoint[] required = _enableLegCheck
                ? new[]
                {
                    PoseJoint.LeftShoulder, PoseJoint.RightShoulder,
                    PoseJoint.LeftHip, PoseJoint.RightHip,
                    PoseJoint.LeftWrist, PoseJoint.RightWrist,
                    PoseJoint.LeftAnkle, PoseJoint.RightAnkle,
                }
                : new[]
                {
                    PoseJoint.LeftShoulder, PoseJoint.RightShoulder,
                    PoseJoint.LeftHip, PoseJoint.RightHip,
                    PoseJoint.LeftWrist, PoseJoint.RightWrist,
                };

            for (int i = 0; i < required.Length; i++)
            {
                if (_poseReceiver.GetJointConfidence(required[i], 0) < _minimumConfidence)
                    return false;
            }

            if (!TryJoint(PoseJoint.LeftShoulder, out var leftShoulder) ||
                !TryJoint(PoseJoint.RightShoulder, out var rightShoulder) ||
                !TryJoint(PoseJoint.LeftHip, out var leftHip) ||
                !TryJoint(PoseJoint.RightHip, out var rightHip) ||
                !TryJoint(PoseJoint.LeftWrist, out var leftWrist) ||
                !TryJoint(PoseJoint.RightWrist, out var rightWrist))
                return false;

            float feetRatio = 0f;
            if (_enableLegCheck)
            {
                if (!TryJoint(PoseJoint.LeftAnkle, out var leftAnkle) ||
                    !TryJoint(PoseJoint.RightAnkle, out var rightAnkle))
                    return false;

                float shoulderWidth = Mathf.Max(0.02f, Mathf.Abs(leftShoulder.x - rightShoulder.x));
                float ankleDistance = Mathf.Abs(leftAnkle.x - rightAnkle.x);
                feetRatio = ankleDistance / shoulderWidth;
            }

            float shoulderY = (leftShoulder.y + rightShoulder.y) * 0.5f;
            float hipY = (leftHip.y + rightHip.y) * 0.5f;
            float torsoHeight = Mathf.Max(0.04f, Mathf.Abs(hipY - shoulderY));

            bool handsUp =
                leftWrist.y <= leftShoulder.y - torsoHeight * _handsUpAboveShoulderRatio &&
                rightWrist.y <= rightShoulder.y - torsoHeight * _handsUpAboveShoulderRatio;
            bool handsDown =
                leftWrist.y >= leftShoulder.y + torsoHeight * _handsDownBelowShoulderRatio &&
                rightWrist.y >= rightShoulder.y + torsoHeight * _handsDownBelowShoulderRatio;

            bool feetTogether = !_enableLegCheck || feetRatio <= _feetTogetherMaxRatio;
            bool feetApart = !_enableLegCheck || feetRatio >= _feetApartMinRatio;

            if (feetTogether && handsDown)
                pose = JumpingJackPose.FeetTogetherHandsDown;
            else if (feetApart && handsUp)
                pose = JumpingJackPose.FeetApartHandsUp;

            return true;
        }

        private bool TryJoint(PoseJoint joint, out Vector2 point)
        {
            Vector2? value = _poseReceiver.GetJointNormalized(joint, 0);
            point = value.GetValueOrDefault();
            return value.HasValue;
        }
    }
}
