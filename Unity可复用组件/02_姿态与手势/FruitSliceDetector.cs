using System;
using UnityEngine;

namespace EchoWorkSpace
{
    public class FruitSliceDetector : MonoBehaviour
    {
        [Tooltip("Hand speed in playfield heights per second.")]
        [Min(0.01f)] public float minimumSpeed = 0.45f;
        public float maximumSampleGap = 0.22f;
        public float maximumStep = 0.45f;
        public float bladeRadius = 12f;
        public UIHandTrail[] trails = new UIHandTrail[4];
        private sealed class Hand { public bool valid; public Vector2 point; public float time; }
        private readonly Hand[] _hands = { new Hand(), new Hand(), new Hand(), new Hand() };
        public void ResetHand(int index)
        {
            _hands[index].valid = false;
            if (index < trails.Length && trails[index] != null) trails[index].Clear();
        }
        public void ResetAll() { for (int i = 0; i < 4; i++) ResetHand(i); }

        public void Sample(int index, Vector2? point, float time, RectTransform playfield,
            FruitWaveSpawner spawner, Action<FruitTarget, Vector2> onSlice)
        {
            if (!point.HasValue) { ResetHand(index); return; }
            var h = _hands[index]; var current = point.Value;
            float height = Mathf.Max(1, playfield.rect.height);
            float dt = time - h.time;
            bool continuous = h.valid && dt > 0.0001f && dt <= maximumSampleGap &&
                Vector2.Distance(current, h.point) <= height * maximumStep;
            bool fast = continuous && Vector2.Distance(current, h.point) / dt / height >= minimumSpeed;
            if (!continuous) ResetHand(index);
            int owner = index / 2;
            Rect half = playfield.rect;
            if (owner == 0) half.xMax = half.center.x; else half.xMin = half.center.x;
            if (index < trails.Length && trails[index] != null)
            {
                // The trail follows the player's real hand across the center line.
                // The half-rectangle is used below for slicing only.
                if (playfield.rect.Contains(current)) trails[index].Push(current, fast);
                else trails[index].Clear();
            }
            if (fast)
            {
                Vector2 from = h.point, to = current;
                float enter = 0, exit = 1;
                if (Clip(half, from, to, ref enter, ref exit))
                {
                    var area = spawner.Area(owner);
                    foreach (var fruit in spawner.Active)
                    {
                        if (fruit == null || !fruit.Alive || fruit.Owner != owner) continue;
                        float begin = Mathf.Max(enter, (fruit.SpawnTime - h.time) / dt);
                        if (begin > exit) continue;
                        Vector2 a = area.InverseTransformPoint(playfield.TransformPoint(Vector2.Lerp(from, to, begin)));
                        Vector2 b = area.InverseTransformPoint(playfield.TransformPoint(Vector2.Lerp(from, to, exit)));
                        Vector2 ra = a - fruit.PositionAt(h.time + begin * dt);
                        Vector2 rb = b - fruit.PositionAt(h.time + exit * dt);
                        float u;
                        if (!SegmentCircle(ra, rb, fruit.Radius + bladeRadius, out u)) continue;
                        Vector2 hit = Vector2.Lerp(a, b, u);
                        if (fruit.TrySlice()) onSlice(fruit, area.TransformPoint(hit));
                    }
                }
            }
            h.valid = true; h.point = current; h.time = time;
        }

        public static bool SegmentCircle(Vector2 a, Vector2 b, float radius, out float fraction)
        {
            var delta = b - a;
            fraction = delta.sqrMagnitude < 0.000001f ? 0 : Mathf.Clamp01(-Vector2.Dot(a, delta) / delta.sqrMagnitude);
            return (a + delta * fraction).sqrMagnitude <= radius * radius;
        }

        public static bool Clip(Rect rect, Vector2 a, Vector2 b, ref float enter, ref float exit)
        {
            Vector2 d = b - a;
            return ClipEdge(-d.x, a.x - rect.xMin, ref enter, ref exit) &&
                ClipEdge(d.x, rect.xMax - a.x, ref enter, ref exit) &&
                ClipEdge(-d.y, a.y - rect.yMin, ref enter, ref exit) &&
                ClipEdge(d.y, rect.yMax - a.y, ref enter, ref exit);
        }
        private static bool ClipEdge(float p, float q, ref float enter, ref float exit)
        {
            if (Mathf.Abs(p) < 0.000001f) return q >= 0;
            float t = q / p;
            if (p < 0) enter = Mathf.Max(enter, t); else exit = Mathf.Min(exit, t);
            return enter <= exit;
        }
    }
}
