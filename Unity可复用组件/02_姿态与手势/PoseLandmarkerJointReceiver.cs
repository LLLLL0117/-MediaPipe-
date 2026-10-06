using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections.Generic;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace EchoWorkSpace
{
    public enum PoseJoint
    {
        Nose = 0,
        LeftEyeInner = 1,
        LeftEye = 2,
        LeftEyeOuter = 3,
        RightEyeInner = 4,
        RightEye = 5,
        RightEyeOuter = 6,
        LeftEar = 7,
        RightEar = 8,
        MouthLeft = 9,
        MouthRight = 10,
        LeftShoulder = 11,
        RightShoulder = 12,
        LeftElbow = 13,
        RightElbow = 14,
        LeftWrist = 15,
        RightWrist = 16,
        LeftPinky = 17,
        RightPinky = 18,
        LeftIndex = 19,
        RightIndex = 20,
        LeftThumb = 21,
        RightThumb = 22,
        LeftHip = 23,
        RightHip = 24,
        LeftKnee = 25,
        RightKnee = 26,
        LeftAnkle = 27,
        RightAnkle = 28,
        LeftHeel = 29,
        RightHeel = 30,
        LeftFootIndex = 31,
        RightFootIndex = 32,
    }

    public class PoseLandmarkerJointReceiver : MonoBehaviour
    {
        [Tooltip("PoseLandmarkerRunner in the scene.")]
        [SerializeField] private PoseLandmarkerRunner _runner;

        [Tooltip("Mirror normalized X when the preview/interaction needs a horizontal mirror.")]
        [SerializeField] private bool _mirrorX = false;

        public const int JointCount = 33;

        public Vector3?[] Joints { get; private set; } = new Vector3?[JointCount];
        public Vector2?[] NormalizedJoints { get; private set; } = new Vector2?[JointCount];
        public bool IsTracked { get; private set; }

        // Result indices are frame-local, not persistent player identities.
        public int PoseCount { get; private set; }
        public int ResultVersion { get; private set; }
        public float LastResultTime { get; private set; } = float.NegativeInfinity;
        private readonly List<Vector3?[]> _worldPoses = new List<Vector3?[]>();
        private readonly List<Vector2?[]> _imagePoses = new List<Vector2?[]>();
        private readonly List<float[]> _confidence = new List<float[]>();
        private PoseLandmarkerResult _pendingResult;
        private bool _hasNewData;
        private readonly object _lock = new object();

        private void OnEnable()
        {
            if (_runner != null)
                _runner.OnResultOutput += OnResult;
        }

        private void OnDisable()
        {
            if (_runner != null)
                _runner.OnResultOutput -= OnResult;
            lock (_lock)
            {
                _hasNewData = false;
                _pendingResult = default;
            }
            PoseCount = 0;
            IsTracked = false;
            LastResultTime = float.NegativeInfinity;
            ClearJoints();
        }

        private void OnResult(PoseLandmarkerResult result)
        {
            lock (_lock)
            {
                // MediaPipe reuses its result buffers after this callback.
                var snapshot = PoseLandmarkerResult.Alloc(result.poseLandmarks?.Count ?? 0);
                if (result.poseLandmarks != null)
                    foreach (var pose in result.poseLandmarks)
                    {
                        var copy = NormalizedLandmarks.Alloc(JointCount);
                        if (pose.landmarks != null) copy.landmarks.AddRange(pose.landmarks);
                        snapshot.poseLandmarks.Add(copy);
                    }
                if (result.poseWorldLandmarks != null)
                    foreach (var pose in result.poseWorldLandmarks)
                    {
                        var copy = Landmarks.Alloc(JointCount);
                        if (pose.landmarks != null) copy.landmarks.AddRange(pose.landmarks);
                        snapshot.poseWorldLandmarks.Add(copy);
                    }
                _pendingResult = snapshot;
                _hasNewData = true;
            }
        }

        private void LateUpdate()
        {
            PoseLandmarkerResult result;
            lock (_lock)
            {
                if (!_hasNewData) return;
                result = _pendingResult;
                _pendingResult = default;
                _hasNewData = false;
            }

            ResultVersion++;
            LastResultTime = Time.unscaledTime;
            PoseCount = 0;
            ClearJoints();
            int count = Mathf.Max(result.poseLandmarks?.Count ?? 0,
                result.poseWorldLandmarks?.Count ?? 0);
            for (int p = 0; p < count; p++)
            {
                var world = p < (result.poseWorldLandmarks?.Count ?? 0)
                    ? result.poseWorldLandmarks[p].landmarks : null;
                var image = p < (result.poseLandmarks?.Count ?? 0)
                    ? result.poseLandmarks[p].landmarks : null;
                bool worldOk = world != null && world.Count >= JointCount;
                bool imageOk = image != null && image.Count >= JointCount;
                if (!worldOk && !imageOk) continue;

                if (PoseCount == _worldPoses.Count)
                {
                    _worldPoses.Add(new Vector3?[JointCount]);
                    _imagePoses.Add(new Vector2?[JointCount]);
                    _confidence.Add(new float[JointCount]);
                }
                var worldPoints = _worldPoses[PoseCount];
                var imagePoints = _imagePoses[PoseCount];
                for (int j = 0; j < JointCount; j++)
                {
                    _confidence[PoseCount][j] = imageOk
                        ? Mathf.Min(image[j].visibility ?? 1f, image[j].presence ?? 1f) : 0f;
                    worldPoints[j] = worldOk
                        ? new Vector3(world[j].x, world[j].y, world[j].z) : (Vector3?)null;
                    imagePoints[j] = imageOk
                        ? new Vector2(_mirrorX ? 1f - image[j].x : image[j].x, image[j].y)
                        : (Vector2?)null;
                }
                PoseCount++;
            }
            IsTracked = PoseCount > 0;
            if (IsTracked)
            {
                System.Array.Copy(_worldPoses[0], Joints, JointCount);
                System.Array.Copy(_imagePoses[0], NormalizedJoints, JointCount);
            }
        }

        public Vector2? GetJointNormalized(PoseJoint joint, int poseIndex)
        {
            return poseIndex >= 0 && poseIndex < PoseCount
                ? _imagePoses[poseIndex][(int)joint] : null;
        }

        public float GetJointConfidence(PoseJoint joint, int poseIndex)
        {
            return poseIndex >= 0 && poseIndex < PoseCount
                ? _confidence[poseIndex][(int)joint] : 0f;
        }

        public Vector3? GetJoint(PoseJoint joint) => Joints[(int)joint];

        public Vector2? GetJointNormalized(PoseJoint joint) => NormalizedJoints[(int)joint];

        public Vector2? GetJointScreenPosition(PoseJoint joint, int poseIndex = 0)
        {
            var n = GetJointNormalized(joint, poseIndex);
            if (!n.HasValue) return null;

            return new Vector2(n.Value.x * Screen.width, (1f - n.Value.y) * Screen.height);
        }

        public Vector2? GetJointScreenPosition(PoseJoint joint, RectTransform mappingArea, int poseIndex = 0)
        {
            var n = GetJointNormalized(joint, poseIndex);
            if (!n.HasValue || mappingArea == null) return null;

            var canvas = mappingArea.GetComponentInParent<Canvas>();
            var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            UnityEngine.Rect rect = mappingArea.rect;
            Vector2 local = new Vector2(
                Mathf.LerpUnclamped(rect.xMin, rect.xMax, n.Value.x),
                Mathf.LerpUnclamped(rect.yMax, rect.yMin, n.Value.y)
            );
            return RectTransformUtility.WorldToScreenPoint(cam, mappingArea.TransformPoint(local));
        }

        private void ClearJoints()
        {
            for (int i = 0; i < JointCount; i++)
            {
                Joints[i] = null;
                NormalizedJoints[i] = null;
            }
        }
    }
}
