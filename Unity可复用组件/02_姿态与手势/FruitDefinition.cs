using UnityEngine;

namespace EchoWorkSpace
{
    [CreateAssetMenu(menuName = "Training/Fruit Definition")]
    public class FruitDefinition : ScriptableObject
    {
        public Sprite sprite;
        public Color tint = Color.white;
        [Min(16)] public float size = 150f;
        [Range(0.1f, 0.6f)] public float radiusRatio = 0.43f;
        public UIFruitBurstEffect burstPrefab;
        public AudioClip sliceClip;
    }
}
