using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class RoundedUIImage : MonoBehaviour
{
    [Header("Rounded Corners")]
    [Min(0f)]
    [SerializeField] private float cornerRadius = 24f;
    [SerializeField] private bool topLeft = true;
    [SerializeField] private bool topRight = true;
    [SerializeField] private bool bottomRight = true;
    [SerializeField] private bool bottomLeft = true;

    [Header("Border")]
    [Min(0f)]
    [SerializeField] private float borderWidth = 0f;
    [SerializeField] private Color borderColor = Color.white;

    [Header("Edge")]
    [Range(0.1f, 5f)]
    [SerializeField] private float smoothness = 1f;

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int BorderWidthId = Shader.PropertyToID("_BorderWidth");
    private static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
    private static readonly int CornerMaskId = Shader.PropertyToID("_CornerMask");
    private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

    private Image _image;
    private Material _originalMaterial;
    private Material _materialInstance;

    public float CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Mathf.Max(0f, value);
            ApplyProperties();
        }
    }

    public float BorderWidth
    {
        get => borderWidth;
        set
        {
            borderWidth = Mathf.Max(0f, value);
            ApplyProperties();
        }
    }

    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;
            ApplyProperties();
        }
    }

    /// <summary>运行时代码用：批量设置四个圆角开关并立即刷新材质。</summary>
    public void SetCorners(bool topLeftEnabled, bool topRightEnabled, bool bottomRightEnabled,
        bool bottomLeftEnabled)
    {
        topLeft = topLeftEnabled;
        topRight = topRightEnabled;
        bottomRight = bottomRightEnabled;
        bottomLeft = bottomLeftEnabled;
        ApplyProperties();
    }

    private void OnEnable()
    {
        EnsureMaterial();
        ApplyProperties();
    }

    private void OnDisable()
    {
        if (_image != null && _image.material == _materialInstance)
        {
            _image.material = _originalMaterial;
        }
    }

    private void OnDestroy()
    {
        if (_materialInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_materialInstance);
        }
        else
        {
            DestroyImmediate(_materialInstance);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyProperties();
    }

    private void OnValidate()
    {
        cornerRadius = Mathf.Max(0f, cornerRadius);
        borderWidth = Mathf.Max(0f, borderWidth);
        smoothness = Mathf.Max(0.1f, smoothness);

        EnsureMaterial();
        ApplyProperties();
    }

    private void EnsureMaterial()
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }

        if (_originalMaterial == null && _image.material != _materialInstance)
        {
            _originalMaterial = _image.material;
        }

        if (_materialInstance != null)
        {
            _image.material = _materialInstance;
            return;
        }

        Shader shader = Shader.Find("UI/Rounded Image");
        if (shader == null)
        {
            Debug.LogError("RoundedUIImage requires shader 'UI/Rounded Image'. Keep RoundedUIImage.shader under Assets.", this);
            return;
        }

        _materialInstance = new Material(shader)
        {
            name = "Rounded Image Material (Instance)",
            hideFlags = HideFlags.HideAndDontSave
        };

        _image.material = _materialInstance;
    }

    private void ApplyProperties()
    {
        if (_materialInstance == null)
        {
            return;
        }

        Rect rect = ((RectTransform)transform).rect;
        float width = Mathf.Max(1f, rect.width);
        float height = Mathf.Max(1f, rect.height);
        float maxRadius = Mathf.Min(width, height) * 0.5f;
        float radius = Mathf.Min(cornerRadius, maxRadius);
        float border = Mathf.Min(borderWidth, radius);

        _materialInstance.SetFloat(RadiusId, radius);
        _materialInstance.SetFloat(BorderWidthId, border);
        _materialInstance.SetColor(BorderColorId, borderColor);
        _materialInstance.SetVector(RectSizeId, new Vector4(width, height, 0f, 0f));
        _materialInstance.SetFloat(SmoothnessId, smoothness);
        _materialInstance.SetVector(CornerMaskId, new Vector4(
            topLeft ? 1f : 0f,
            topRight ? 1f : 0f,
            bottomRight ? 1f : 0f,
            bottomLeft ? 1f : 0f));
    }
}
