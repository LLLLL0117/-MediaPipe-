using System;
using UnityEngine;
using UnityEngine.UI;

namespace EchoWorkSpace
{
    /// <summary>
    /// 单个可命中图标：挂在 UI 图标 prefab 根上（需要 RectTransform）。
    /// Spawner 生成后调用 Init，HandHitDetector 每帧调用 TryHit 做命中判定。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HitTarget : MonoBehaviour
    {
        [Tooltip("命中半径（屏幕像素）")]
        [SerializeField] private float _hitRadiusPixels = 90f;

        /// <summary>命中半径（屏幕像素）。可在运行时修改，下一帧生效。</summary>
        public float HitRadiusPixels
        {
            get => GetScaledHitRadius();
            set => _hitRadiusPixels = Mathf.Max(0f, value);
        }

        [Tooltip("Scale the hit radius by the parent Canvas scaleFactor so it behaves consistently across resolutions.")]
        [SerializeField] private bool _scaleHitRadiusWithCanvas = true;

        [Tooltip("是否在生成时随机改变颜色。第二关固定颜色的 target 请关闭。")]
        [SerializeField] private bool _randomizeColorOnInit = true;

        [Header("随机配色")]
        [Tooltip("要被随机着色的 Graphic（Image / RawImage / Text 都行）。为空则自动取自身的 Image。")]
        [SerializeField] private Graphic _colorTarget;
        [Tooltip("候选颜色池；为空则使用下面的 HSV 随机。")]
        [SerializeField] private Color[] _colorPalette;
        [Tooltip("使用 HSV 随机时的饱和度与明度范围（H 全随机）")]
        [SerializeField] private Vector2 _hsvSaturation = new Vector2(0.6f, 0.9f);
        [SerializeField] private Vector2 _hsvValue      = new Vector2(0.85f, 1f);
        [Tooltip("是否保留原色的 Alpha（一般保留以便做半透明图标）")]
        [SerializeField] private bool _keepOriginalAlpha = true;

        public event Action<HitTarget, Vector2> OnHit; // Vector2 = 命中时图标屏幕坐标

        public bool IsAlive { get; private set; }

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (_colorTarget == null) _colorTarget = GetComponent<Graphic>();
        }

        public void Init(Vector2 anchoredPos)
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            _rect.anchoredPosition = anchoredPos;
            if (_randomizeColorOnInit)
                RandomizeColor();
            IsAlive = true;
            gameObject.SetActive(true);
        }

        private void RandomizeColor()
        {
            if (_colorTarget == null) return;

            Color c;
            if (_colorPalette != null && _colorPalette.Length > 0)
            {
                c = _colorPalette[UnityEngine.Random.Range(0, _colorPalette.Length)];
            }
            else
            {
                float h = UnityEngine.Random.value;
                float s = UnityEngine.Random.Range(_hsvSaturation.x, _hsvSaturation.y);
                float v = UnityEngine.Random.Range(_hsvValue.x, _hsvValue.y);
                c = Color.HSVToRGB(h, s, v);
            }

            if (_keepOriginalAlpha) c.a = _colorTarget.color.a;
            _colorTarget.color = c;
        }

        /// <summary>取图标中心的屏幕像素坐标（兼容 Overlay / Camera / World 三种 Canvas 渲染模式）。</summary>
        public Vector2 GetScreenCenter()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            return RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), _rect.position);
        }

        /// <summary>
        /// 用一个屏幕坐标点尝试命中。命中则触发 OnHit 并标记为已消费（等待 Spawner 回收）。
        /// </summary>
        public bool TryHit(Vector2 screenPos)
        {
            if (!IsAlive) return false;

            if (ContainsScreenPoint(screenPos))
            {
                IsAlive = false;
                OnHit?.Invoke(this, GetScreenCenter());
                return true;
            }

            return false;
        }

        public bool ContainsScreenPoint(Vector2 screenPos)
        {
            if (!IsAlive) return false;

            if (_rect == null) _rect = GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPos, GetCanvasCamera()))
                return true;

            Vector2 center = GetScreenCenter();
            return Vector2.Distance(screenPos, center) <= HitRadiusPixels;
        }

        public void Despawn()
        {
            IsAlive = false;
            gameObject.SetActive(false);
        }

        private Camera GetCanvasCamera()
        {
            var canvas = _rect != null ? _rect.GetComponentInParent<Canvas>() : null;
            return (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;
        }

        private float GetScaledHitRadius()
        {
            if (!_scaleHitRadiusWithCanvas) return _hitRadiusPixels;

            if (_rect == null) _rect = GetComponent<RectTransform>();
            var canvas = _rect != null ? _rect.GetComponentInParent<Canvas>() : null;
            return _hitRadiusPixels * (canvas != null ? canvas.scaleFactor : 1f);
        }
    }
}
