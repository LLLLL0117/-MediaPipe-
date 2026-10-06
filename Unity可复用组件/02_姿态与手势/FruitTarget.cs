using UnityEngine;
using UnityEngine.UI;

namespace EchoWorkSpace
{
    [RequireComponent(typeof(Image))]
    public class FruitTarget : MonoBehaviour
    {
        public int Owner { get; private set; }
        public bool Alive { get; private set; }
        public FruitDefinition Definition { get; private set; }
        public float SpawnTime { get; private set; }
        public float Radius => Definition.size * Definition.radiusRatio;
        public RectTransform Rect => (RectTransform)transform;
        private Vector2 _origin, _velocity;
        private float _gravity, _spin, _bottom;

        public void Launch(int owner, FruitDefinition definition, Vector2 origin, Vector2 velocity,
            float gravity, float spin, float time, float bottom)
        {
            Owner = owner; Definition = definition; _origin = origin; _velocity = velocity;
            _gravity = gravity; _spin = spin; SpawnTime = time; _bottom = bottom; Alive = true;
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.sizeDelta = Vector2.one * definition.size;
            Rect.anchoredPosition = origin;
            Rect.localRotation = Quaternion.identity;
            var image = GetComponent<Image>();
            image.sprite = definition.sprite; image.color = definition.tint;
            image.preserveAspect = true; image.raycastTarget = false;
            gameObject.SetActive(true);
        }

        public Vector2 PositionAt(float time)
        {
            float t = Mathf.Max(0f, time - SpawnTime);
            return _origin + _velocity * t + Vector2.down * (0.5f * _gravity * t * t);
        }

        public bool Tick(float time)
        {
            var position = PositionAt(time);
            Rect.anchoredPosition = position;
            Rect.localRotation = Quaternion.Euler(0, 0, _spin * (time - SpawnTime));
            return position.y < _bottom && time > SpawnTime + 0.1f;
        }

        public bool TrySlice()
        {
            if (!Alive) return false;
            Alive = false; gameObject.SetActive(false); return true;
        }

        public void Retire() { Alive = false; gameObject.SetActive(false); }
    }
}
