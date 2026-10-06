using UnityEngine;
using UnityEngine.Events;

namespace EchoWorkSpace
{
    public class HandHitDetector : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private PoseLandmarkerJointReceiver _poseReceiver;
        [SerializeField] private HitTargetSpawner _spawner;
        [SerializeField] private GameManager _gameManager;

        [Tooltip("把归一化坐标映射到这个区域；为空则映射到全屏。通常等于摄像头预览所在的 RectTransform。")]
        [SerializeField] private RectTransform _mappingArea;

        [Tooltip("可选：使用 CanvasPoseLandmarkRenderer 中完全一致的坐标映射。")]
        [SerializeField] private CanvasPoseLandmarkRenderer _poseRenderer;

        [Header("调试")]
        [Tooltip("打开后每 0.5 秒打印一次当前检测状态。")]
        [SerializeField] private bool _debugLog = false;

        [Tooltip("打开后在 Scene 视图绘制手指点与命中半径。")]
        [SerializeField] private bool _debugGizmos = false;

        [Header("命中事件（命中点屏幕坐标 + 命中的 Target）")]
        public UnityEvent<Vector2, HitTarget> OnHit;

        private float _logTimer;
        private Vector2? _lastLeftScreen;
        private Vector2? _lastRightScreen;

        private void Update()
        {
            _lastLeftScreen = null;
            _lastRightScreen = null;

            if (_poseReceiver == null) { DebugTick("PoseReceiver 未赋值"); return; }
            if (_spawner == null) { DebugTick("Spawner 未赋值"); return; }
            if (_gameManager == null) { DebugTick("GameManager 未赋值"); return; }

            if (_gameManager.State != GameState.Playing)
            {
                DebugTick($"GameState = {_gameManager.State}，还没有进入 Playing");
                return;
            }

            if (!_poseReceiver.IsTracked)
            {
                DebugTick("PoseReceiver.IsTracked = false，没有检测到人体");
                return;
            }

            var targets = _spawner.ActiveTargets;
            if (targets == null || targets.Count == 0)
            {
                DebugTick("当前没有活动的 HitTarget");
                return;
            }

            for (int poseIndex = 0; poseIndex < _poseReceiver.PoseCount; poseIndex++)
            {
                _lastRightScreen = GetScreen(PoseJoint.RightIndex, poseIndex);
                _lastLeftScreen = GetScreen(PoseJoint.LeftIndex, poseIndex);
                if (_poseRenderer != null &&
                    _poseRenderer.TryGetTouchScreenPositions(out var leftScreen, out var rightScreen, poseIndex))
                {
                    _lastLeftScreen = leftScreen;
                    _lastRightScreen = rightScreen;
                }

                if (_debugLog)
                    DebugTick($"targets={targets.Count}  R={_lastRightScreen}  L={_lastLeftScreen}");

                if (_lastRightScreen.HasValue && TryHitTargets(_lastRightScreen.Value))
                    continue;

                if (_lastLeftScreen.HasValue)
                    TryHitTargets(_lastLeftScreen.Value);
            }
        }

        private Vector2? GetScreen(PoseJoint joint, int poseIndex)
        {
            return _mappingArea != null
                ? _poseReceiver.GetJointScreenPosition(joint, _mappingArea, poseIndex)
                : _poseReceiver.GetJointScreenPosition(joint, poseIndex);
        }

        private bool TryHitTargets(Vector2 screenPos)
        {
            var targets = _spawner.ActiveTargets;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target == null || !target.IsAlive)
                    continue;

                if (!target.ContainsScreenPoint(screenPos))
                    continue;

                if (!_spawner.TryNotifyHit(target))
                    return false;

                OnHit?.Invoke(target.GetScreenCenter(), target);
                return true;
            }

            return false;
        }

        private void DebugTick(string msg)
        {
            if (!_debugLog) return;

            _logTimer += Time.deltaTime;
            if (_logTimer < 0.5f) return;

            _logTimer = 0f;
            Debug.Log($"[HandHitDetector] {msg}");
        }

        private void OnDrawGizmos()
        {
            if (!_debugGizmos) return;

            if (_spawner != null && _spawner.ActiveTargets != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var target in _spawner.ActiveTargets)
                {
                    if (target == null) continue;
                    var c = target.GetScreenCenter();
                    Gizmos.DrawWireSphere(new Vector3(c.x, c.y, 0f), target.HitRadiusPixels);
                }
            }

            if (_lastRightScreen.HasValue)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(new Vector3(_lastRightScreen.Value.x, _lastRightScreen.Value.y, 0f), 10f);
            }

            if (_lastLeftScreen.HasValue)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(new Vector3(_lastLeftScreen.Value.x, _lastLeftScreen.Value.y, 0f), 10f);
            }
        }
    }
}
