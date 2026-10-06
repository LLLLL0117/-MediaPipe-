using UnityEngine;
using UnityEngine.UI;

namespace EchoWorkSpace
{
    public enum PoseTouchJoint
    {
        Wrist,
        Thumb,
        Index,
        Pinky,
    }

    [DefaultExecutionOrder(100)]
    public class CanvasPoseLandmarkRenderer : MonoBehaviour
    {
        private static readonly (int, int)[] Connections =
        {
            (0, 1), (1, 2), (2, 3), (3, 7),
            (0, 4), (4, 5), (5, 6), (6, 8),
            (9, 10),
            (11, 13), (13, 15),
            (15, 17), (15, 19), (15, 21), (17, 19),
            (12, 14), (14, 16),
            (16, 18), (16, 20), (16, 22), (18, 20),
            (11, 12), (12, 24), (24, 23), (23, 11),
            (23, 25), (25, 27), (27, 29), (27, 31), (29, 31),
            (24, 26), (26, 28), (28, 30), (28, 32), (30, 32),
        };

        [Header("References")]
        [SerializeField] private PoseLandmarkerJointReceiver _poseReceiver;
        [SerializeField] private Canvas _canvas;
        [Tooltip("The UI area that the normalized pose should be mapped into. Leave empty to use the whole Canvas.")]
        [SerializeField] private RectTransform _targetRect;

        [Header("Appearance")]
        [SerializeField] private Color _jointColor = Color.cyan;
        [SerializeField] private Color _handJointColor = new Color(1f, 0.85f, 0f, 1f);
        [SerializeField] private Color _touchJointColor = Color.red;
        [SerializeField] private Color _connectionColor = Color.white;
        [SerializeField] private float _jointSize = 14f;
        [SerializeField] private float _connectionWidth = 5f;

        [Header("Touch")]
        [SerializeField] private PoseTouchJoint _touchJoint = PoseTouchJoint.Index;

        [Header("Mapping")]
        [SerializeField] private float _scale = 1f;
        [SerializeField] private float _offsetX = 0f;
        [SerializeField] private float _offsetY = 0f;
        [SerializeField] private bool _flipX = false;
        [SerializeField] private bool _flipY = false;

        [Header("Limits")]
        [SerializeField] private int _maxPoses = 1;

        private RectTransform[][] _jointRects;
        private Image[][] _jointImages;
        private RectTransform[][] _connectionRects;
        private RectTransform[] _poseRoots;
        private RectTransform _canvasRect;
        private RectTransform _selfRect;

        private void Awake()
        {
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            _selfRect = GetComponent<RectTransform>();
            if (_selfRect == null)
                _selfRect = gameObject.AddComponent<RectTransform>();

            _canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
            int maxPoses = Mathf.Max(1, _maxPoses);
            _poseRoots = new RectTransform[maxPoses];
            _jointRects = new RectTransform[maxPoses][];
            _jointImages = new Image[maxPoses][];
            _connectionRects = new RectTransform[maxPoses][];

            for (int p = 0; p < maxPoses; p++)
            {
                var poseRoot = CreateRect($"Pose_{p}", transform);
                _poseRoots[p] = poseRoot;
                _jointRects[p] = new RectTransform[PoseLandmarkerJointReceiver.JointCount];
                _jointImages[p] = new Image[PoseLandmarkerJointReceiver.JointCount];
                _connectionRects[p] = new RectTransform[Connections.Length];

                var connectionRoot = CreateRect("Connections", poseRoot);
                for (int c = 0; c < Connections.Length; c++)
                {
                    var rect = CreateImageRect($"Conn_{c}", connectionRoot, _connectionColor);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    _connectionRects[p][c] = rect;
                }

                var jointRoot = CreateRect("Joints", poseRoot);
                for (int j = 0; j < PoseLandmarkerJointReceiver.JointCount; j++)
                {
                    var rect = CreateImageRect($"Joint_{j}", jointRoot, GetJointColor(j));
                    rect.sizeDelta = new Vector2(_jointSize, _jointSize);
                    _jointRects[p][j] = rect;
                    _jointImages[p][j] = rect.GetComponent<Image>();
                }

                poseRoot.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (_poseReceiver == null || _canvasRect == null) return;

            int poseCount = Mathf.Min(_poseReceiver.PoseCount, _poseRoots.Length);
            for (int p = 0; p < poseCount; p++)
            {
                if (!TryGetPosePositions(p, out var positions))
                {
                    SetPoseVisible(p, false);
                    continue;
                }

                SetPoseVisible(p, true);

                for (int j = 0; j < PoseLandmarkerJointReceiver.JointCount; j++)
                {
                    _jointRects[p][j].anchoredPosition = positions[j];
                    var image = _jointImages[p][j];
                    if (image != null)
                        image.color = GetJointColor(j);
                }

                for (int c = 0; c < Connections.Length; c++)
                {
                    var (a, b) = Connections[c];
                    Vector2 posA = positions[a];
                    Vector2 posB = positions[b];
                    Vector2 diff = posB - posA;

                    var rect = _connectionRects[p][c];
                    rect.anchoredPosition = (posA + posB) * 0.5f;
                    rect.sizeDelta = new Vector2(diff.magnitude, _connectionWidth);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                }
            }

            for (int p = poseCount; p < _poseRoots.Length; p++)
            {
                SetPoseVisible(p, false);
            }
        }

        private bool TryGetPosePositions(int poseIndex, out Vector2[] positions)
        {
            positions = new Vector2[PoseLandmarkerJointReceiver.JointCount];

            for (int j = 0; j < PoseLandmarkerJointReceiver.JointCount; j++)
            {
                Vector2? normalized = _poseReceiver.GetJointNormalized((PoseJoint)j, poseIndex);
                if (!normalized.HasValue)
                    return false;

                positions[j] = CanvasToRendererLocal(NormalizedToCanvasPosition(normalized.Value.x, normalized.Value.y));
            }

            return true;
        }

        public Vector2 NormalizedToCanvasPosition(float nx, float ny)
        {
            if (_flipX) nx = 1f - nx;
            if (_flipY) ny = 1f - ny;

            Rect rect = GetTargetLocalRect();
            float x = Mathf.LerpUnclamped(rect.xMin, rect.xMax, nx);
            float y = Mathf.LerpUnclamped(rect.yMax, rect.yMin, ny);

            Vector2 canvasPoint = TargetLocalToCanvasLocal(new Vector2(x, y));
            Vector2 center = TargetLocalToCanvasLocal(rect.center);
            return center + (canvasPoint - center) * _scale + new Vector2(_offsetX, _offsetY);
        }

        public Vector2 CanvasPositionToScreenPoint(Vector2 canvasPosition)
        {
            if (_canvasRect == null)
                return canvasPosition;

            Vector3 world = _canvasRect.TransformPoint(canvasPosition);
            return RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), world);
        }

        public Vector2 CanvasToRendererLocal(Vector2 canvasPosition)
        {
            if (_canvasRect == null || _selfRect == null)
                return canvasPosition;

            Vector3 world = _canvasRect.TransformPoint(canvasPosition);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_selfRect, screenPoint, GetCanvasCamera(), out var local);
            return local;
        }

        public Vector2 NormalizedToScreenPoint(float nx, float ny)
        {
            return CanvasPositionToScreenPoint(NormalizedToCanvasPosition(nx, ny));
        }

        public bool TryGetTouchScreenPositions(out Vector2 leftScreen, out Vector2 rightScreen, int poseIndex = 0)
        {
            leftScreen = default;
            rightScreen = default;

            if (_poseReceiver == null || !_poseReceiver.IsTracked)
                return false;

            PoseJoint leftJoint = GetLeftPoseJoint(_touchJoint);
            PoseJoint rightJoint = GetRightPoseJoint(_touchJoint);
            Vector2? left = _poseReceiver.GetJointNormalized(leftJoint, poseIndex);
            Vector2? right = _poseReceiver.GetJointNormalized(rightJoint, poseIndex);
            if (!left.HasValue || !right.HasValue)
                return false;

            leftScreen = NormalizedToScreenPoint(left.Value.x, left.Value.y);
            rightScreen = NormalizedToScreenPoint(right.Value.x, right.Value.y);
            return true;
        }

        public PoseTouchJoint TouchJoint => _touchJoint;

        public Vector2 DebugTargetSize => GetTargetLocalRect().size;

        private Rect GetTargetLocalRect()
        {
            if (_targetRect == null)
                return _canvasRect.rect;

            return _targetRect.rect;
        }

        private Vector2 TargetLocalToCanvasLocal(Vector2 targetLocal)
        {
            if (_targetRect == null)
                return targetLocal;

            return WorldToCanvasLocal(_targetRect.TransformPoint(targetLocal));
        }

        private Vector2 WorldToCanvasLocal(Vector3 world)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, GetCanvasCamera(), out var local);
            return local;
        }

        private Camera GetCanvasCamera()
        {
            return _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
        }

        private void SetPoseVisible(int poseIndex, bool visible)
        {
            var root = _poseRoots[poseIndex].gameObject;
            if (root.activeSelf != visible)
                root.SetActive(visible);
        }

        private Color GetJointColor(int jointIndex)
        {
            if (IsTouchJoint(jointIndex))
                return _touchJointColor;

            return jointIndex >= (int)PoseJoint.LeftWrist && jointIndex <= (int)PoseJoint.RightThumb
                ? _handJointColor
                : _jointColor;
        }

        private bool IsTouchJoint(int jointIndex)
        {
            return jointIndex == (int)GetLeftPoseJoint(_touchJoint) ||
                   jointIndex == (int)GetRightPoseJoint(_touchJoint);
        }

        private static PoseJoint GetLeftPoseJoint(PoseTouchJoint touchJoint)
        {
            switch (touchJoint)
            {
                case PoseTouchJoint.Wrist:
                    return PoseJoint.LeftWrist;
                case PoseTouchJoint.Thumb:
                    return PoseJoint.LeftThumb;
                case PoseTouchJoint.Pinky:
                    return PoseJoint.LeftPinky;
                default:
                    return PoseJoint.LeftIndex;
            }
        }

        private static PoseJoint GetRightPoseJoint(PoseTouchJoint touchJoint)
        {
            switch (touchJoint)
            {
                case PoseTouchJoint.Wrist:
                    return PoseJoint.RightWrist;
                case PoseTouchJoint.Thumb:
                    return PoseJoint.RightThumb;
                case PoseTouchJoint.Pinky:
                    return PoseJoint.RightPinky;
                default:
                    return PoseJoint.RightIndex;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateImageRect(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
