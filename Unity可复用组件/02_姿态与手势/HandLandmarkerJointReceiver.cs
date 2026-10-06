using System;
using UnityEngine;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

namespace EchoWorkSpace
{
    public enum HandJoint
    {
        Wrist = 0,
        ThumbCmc = 1,
        ThumbMcp = 2,
        ThumbIp = 3,
        ThumbTip = 4,
        IndexMcp = 5,
        IndexPip = 6,
        IndexDip = 7,
        IndexTip = 8,
        MiddleMcp = 9,
        MiddlePip = 10,
        MiddleDip = 11,
        MiddleTip = 12,
        RingMcp = 13,
        RingPip = 14,
        RingDip = 15,
        RingTip = 16,
        PinkyMcp = 17,
        PinkyPip = 18,
        PinkyDip = 19,
        PinkyTip = 20,
    }

    public enum HandSide
    {
        Left = 0,
        Right = 1,
    }

    public class HandLandmarkerJointReceiver : MonoBehaviour
    {
        [Tooltip("HandLandmarkerRunner in the scene.")]
        [SerializeField] private HandLandmarkerRunner _runner;

        [Tooltip("Mirror normalized X when the preview/interaction needs a horizontal mirror.")]
        [SerializeField] private bool _mirrorX = false;

        public const int JointCount = 21;
        public const int HandCount = 2;

        public Vector3?[][] Joints { get; private set; }
        public Vector2?[][] NormalizedJoints { get; private set; }
        public bool[] IsHandTracked { get; private set; }
        public bool IsTracked { get; private set; }

        private HandLandmarkerResult _pendingResult;
        private bool _hasNewData;
        private readonly object _lock = new object();

        private void Awake()
        {
            Joints = CreateJointArray();
            NormalizedJoints = CreateNormalizedJointArray();
            IsHandTracked = new bool[HandCount];
        }

        private void OnEnable()
        {
            if (_runner != null)
                _runner.OnResultOutput += OnResult;
        }

        private void OnDisable()
        {
            if (_runner != null)
                _runner.OnResultOutput -= OnResult;
        }

        private void OnResult(HandLandmarkerResult result)
        {
            lock (_lock)
            {
                result.CloneTo(ref _pendingResult);
                _hasNewData = true;
            }
        }

        private void LateUpdate()
        {
            bool hasNew;
            HandLandmarkerResult result;

            lock (_lock)
            {
                hasNew = _hasNewData;
                result = _pendingResult;
                _hasNewData = false;
                _pendingResult = default;
            }

            if (!hasNew)
                return;

            ClearHands();

            var handLandmarks = result.handLandmarks;
            var handWorldLandmarks = result.handWorldLandmarks;
            int detectedCount = handLandmarks != null ? handLandmarks.Count : 0;
            if (detectedCount == 0)
            {
                IsTracked = false;
                return;
            }

            for (int handIndex = 0; handIndex < detectedCount; handIndex++)
            {
                if (!TryGetSide(result, handIndex, out HandSide side))
                    continue;

                int sideIndex = (int)side;
                bool hasImage = TryCopyImageLandmarks(handLandmarks, handIndex, sideIndex);
                bool hasWorld = TryCopyWorldLandmarks(handWorldLandmarks, handIndex, sideIndex);
                IsHandTracked[sideIndex] = hasImage || hasWorld;
            }

            IsTracked = IsHandTracked[(int)HandSide.Left] || IsHandTracked[(int)HandSide.Right];
        }

        public Vector3? GetJoint(HandSide side, HandJoint joint) => Joints[(int)side][(int)joint];

        public Vector2? GetJointNormalized(HandSide side, HandJoint joint) => NormalizedJoints[(int)side][(int)joint];

        public bool IsTrackedSide(HandSide side) => IsHandTracked[(int)side];

        private bool TryCopyImageLandmarks(
            System.Collections.Generic.List<NormalizedLandmarks> handLandmarks,
            int handIndex,
            int sideIndex)
        {
            if (handLandmarks == null || handIndex >= handLandmarks.Count)
                return false;

            var landmarks = handLandmarks[handIndex].landmarks;
            if (landmarks == null || landmarks.Count < JointCount)
                return false;

            for (int i = 0; i < JointCount; i++)
            {
                var lm = landmarks[i];
                float x = _mirrorX ? 1f - lm.x : lm.x;
                NormalizedJoints[sideIndex][i] = new Vector2(x, lm.y);
            }

            return true;
        }

        private bool TryCopyWorldLandmarks(
            System.Collections.Generic.List<Landmarks> handWorldLandmarks,
            int handIndex,
            int sideIndex)
        {
            if (handWorldLandmarks == null || handIndex >= handWorldLandmarks.Count)
                return false;

            var landmarks = handWorldLandmarks[handIndex].landmarks;
            if (landmarks == null || landmarks.Count < JointCount)
                return false;

            for (int i = 0; i < JointCount; i++)
            {
                var lm = landmarks[i];
                Joints[sideIndex][i] = new Vector3(lm.x, lm.y, lm.z);
            }

            return true;
        }

        private static bool TryGetSide(HandLandmarkerResult result, int handIndex, out HandSide side)
        {
            side = (HandSide)Mathf.Clamp(handIndex, 0, HandCount - 1);

            var handedness = result.handedness;
            if (handedness == null || handIndex >= handedness.Count)
                return handIndex < HandCount;

            var categories = handedness[handIndex].categories;
            if (categories == null || categories.Count == 0)
                return handIndex < HandCount;

            string name = categories[0].categoryName;
            if (string.IsNullOrEmpty(name))
                name = categories[0].displayName;

            if (string.Equals(name, "Left", StringComparison.OrdinalIgnoreCase))
            {
                side = HandSide.Left;
                return true;
            }

            if (string.Equals(name, "Right", StringComparison.OrdinalIgnoreCase))
            {
                side = HandSide.Right;
                return true;
            }

            return handIndex < HandCount;
        }

        private void ClearHands()
        {
            IsTracked = false;

            for (int hand = 0; hand < HandCount; hand++)
            {
                IsHandTracked[hand] = false;
                for (int joint = 0; joint < JointCount; joint++)
                {
                    Joints[hand][joint] = null;
                    NormalizedJoints[hand][joint] = null;
                }
            }
        }

        private static Vector3?[][] CreateJointArray()
        {
            var result = new Vector3?[HandCount][];
            for (int i = 0; i < HandCount; i++)
                result[i] = new Vector3?[JointCount];
            return result;
        }

        private static Vector2?[][] CreateNormalizedJointArray()
        {
            var result = new Vector2?[HandCount][];
            for (int i = 0; i < HandCount; i++)
                result[i] = new Vector2?[JointCount];
            return result;
        }
    }
}
