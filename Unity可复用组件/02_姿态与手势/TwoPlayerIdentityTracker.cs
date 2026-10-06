using System.Collections.Generic;
using UnityEngine;

namespace EchoWorkSpace
{
    /// <summary>Two persistent player slots. Pose indices identify only the current result.</summary>
    public sealed class TwoPlayerIdentityTracker
    {
        public struct Candidate
        {
            public int poseIndex;
            public Vector2 position; // Playfield-normalized coordinates, independent of Canvas scaling.
            public float shoulderWidth;
        }

        public sealed class Track
        {
            public bool assigned;
            public bool visible;
            public Candidate sample;
            public Vector2 velocity;
            public float lastSeen;
            internal Candidate recoverySample;
            internal float recoveryStarted;
            internal float lastRecoverySeen;
            internal int recoveryFrames;
        }

        public readonly Track Left = new Track();
        public readonly Track Right = new Track();
        public float MaximumStep = 0.35f;
        public float BothLostSeconds = 1.5f;
        public float ResultTimeout = 0.5f;
        public float AmbiguityMargin = 0.04f;

        // A distant detection must remain consistent before it can restore a lost slot.
        private const float RecoveryDelay = 0.5f;
        private const float RecoveryHoldSeconds = 0.35f;
        private const int RecoveryMinimumFrames = 4;

        private float _lastPresence;
        private float _lastObservation;
        private readonly List<Assignment> _assignments = new List<Assignment>();

        private struct Assignment
        {
            public int left, right, count;
            public float cost;
        }

        public void Reset(float now)
        {
            Clear(Left);
            Clear(Right);
            _lastPresence = _lastObservation = now;
        }

        public void Seed(int player, Candidate candidate, float now)
        {
            Accept(player == 0 ? Left : Right, candidate, now);
            _lastPresence = _lastObservation = now;
        }

        public void Tick(float now)
        {
            if (now - _lastObservation > ResultTimeout)
            {
                Left.visible = Right.visible = false;
                Left.recoveryFrames = Right.recoveryFrames = 0;
            }

            // Ambiguous but present shoulders are not evidence that both people left.
            // Only continuous absence (including a stalled result stream) releases IDs.
            if (now - _lastPresence >= Mathf.Max(BothLostSeconds, ResultTimeout))
            {
                Clear(Left);
                Clear(Right);
            }
        }

        public void Update(IReadOnlyList<Candidate> candidates, float now)
        {
            Tick(now);
            _lastObservation = now;
            if (candidates.Count > 0) _lastPresence = now;
            Left.visible = Right.visible = false;

            _assignments.Clear();
            Assignment best = new Assignment { left = -1, right = -1, count = 0, cost = 0f };
            // Include unmatched slots, so one detection is compared against BOTH players.
            for (int l = -1; l < candidates.Count; l++)
            {
                float leftCost = l < 0 ? 0f : Cost(Left, candidates[l], now);
                if (float.IsPositiveInfinity(leftCost)) continue;
                for (int r = -1; r < candidates.Count; r++)
                {
                    if (l >= 0 && l == r) continue;
                    float rightCost = r < 0 ? 0f : Cost(Right, candidates[r], now);
                    if (float.IsPositiveInfinity(rightCost)) continue;
                    var option = new Assignment
                    {
                        left = l, right = r,
                        count = (l >= 0 ? 1 : 0) + (r >= 0 ? 1 : 0),
                        cost = leftCost + rightCost
                    };
                    _assignments.Add(option);
                    if (option.count > best.count || (option.count == best.count && option.cost < best.cost))
                        best = option;
                }
            }

            bool leftCertain = best.left >= 0;
            bool rightCertain = best.right >= 0;
            foreach (var option in _assignments)
            {
                if (option.count != best.count || option.cost - best.cost > AmbiguityMargin) continue;
                if (option.left != best.left) leftCertain = false;
                if (option.right != best.right) rightCertain = false;
            }
            if (leftCertain) Accept(Left, candidates[best.left], now);
            if (rightCertain) Accept(Right, candidates[best.right], now);

            TryRecover(Left, Right, candidates, now);
            TryRecover(Right, Left, candidates, now);

            // A free slot may be filled only after every surviving identity has been
            // confidently accounted for. Never turn an uncertain survivor into a new player.
            if ((Left.assigned && !Left.visible) || (Right.assigned && !Right.visible)) return;
            FillFreeSlot(Left, 0, candidates, now);
            FillFreeSlot(Right, 1, candidates, now);
        }

        private void TryRecover(Track track, Track other, IReadOnlyList<Candidate> candidates, float now)
        {
            // Account for the surviving player first. A lone ambiguous body must never
            // restore either of two missing identities or be reassigned by screen half.
            if (!track.assigned || track.visible || now - track.lastSeen < RecoveryDelay ||
                (other.assigned && !other.visible))
            {
                track.recoveryFrames = 0;
                return;
            }

            int remaining = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (other.visible && candidates[i].poseIndex == other.sample.poseIndex) continue;
                if (remaining >= 0) { track.recoveryFrames = 0; return; }
                remaining = i;
            }
            if (remaining < 0) { track.recoveryFrames = 0; return; }

            Candidate candidate = candidates[remaining];
            Vector2 position = candidate.position;
            float widthRatio = candidate.shoulderWidth / Mathf.Max(0.02f, track.sample.shoulderWidth);
            // Avoid claiming a duplicate/overlapping skeleton near the surviving player.
            float separation = other.visible ? Vector2.Distance(position, other.sample.position) : 1f;
            float minimumSeparation = Mathf.Max(0.1f,
                (candidate.shoulderWidth + other.sample.shoulderWidth) * 0.5f);
            if (position.x < 0f || position.x > 1f || position.y < 0f || position.y > 1f ||
                widthRatio < 0.5f || widthRatio > 2f || (other.visible && separation < minimumSeparation))
            {
                track.recoveryFrames = 0;
                return;
            }

            float dt = now - track.lastRecoverySeen;
            bool continuous = track.recoveryFrames > 0 && dt > 0f &&
                dt <= Mathf.Min(ResultTimeout, 0.25f) &&
                Vector2.Distance(candidate.position, track.recoverySample.position) <=
                    Mathf.Min(0.18f, 0.04f + dt * 0.8f);
            if (!continuous)
            {
                track.recoveryStarted = now;
                track.recoveryFrames = 0;
            }
            track.recoverySample = candidate;
            track.lastRecoverySeen = now;
            track.recoveryFrames++;
            if (track.recoveryFrames < RecoveryMinimumFrames ||
                now - track.recoveryStarted < RecoveryHoldSeconds) return;

            // Restore this slot, regardless of the candidate's current screen half.
            // Accept clears velocity after the loss; callers start a fresh hand sample.
            Accept(track, candidate, now);
        }

        private float Cost(Track track, Candidate candidate, float now)
        {
            if (!track.assigned) return float.PositiveInfinity;
            float elapsed = Mathf.Max(0f, now - track.lastSeen);
            Vector2 predicted = track.sample.position + track.velocity * Mathf.Min(elapsed, 0.25f);
            float distance = Vector2.Distance(predicted, candidate.position);
            // Tolerate motion over a result gap, while limiting jumps to another body.
            float gate = Mathf.Min(MaximumStep, 0.06f + elapsed * 0.8f);
            if (distance > gate) return float.PositiveInfinity;
            float widthDifference = Mathf.Abs(candidate.shoulderWidth - track.sample.shoulderWidth) /
                Mathf.Max(0.02f, Mathf.Max(candidate.shoulderWidth, track.sample.shoulderWidth));
            // Width is a soft cue: a person turning sideways must remain trackable.
            return distance + widthDifference * 0.05f + Mathf.Min(elapsed, 1f) * 0.02f;
        }

        private void FillFreeSlot(Track track, int player, IReadOnlyList<Candidate> candidates, float now)
        {
            if (track.assigned) return;
            int best = -1;
            float distance = float.PositiveInfinity;
            Vector2 home = new Vector2(player == 0 ? 0.25f : 0.75f, 0.5f);
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if ((Left.visible && Left.sample.poseIndex == candidate.poseIndex) ||
                    (Right.visible && Right.sample.poseIndex == candidate.poseIndex)) continue;
                Vector2 p = candidate.position;
                if (p.x < 0f || p.x > 1f || p.y < 0f || p.y > 1f || (p.x < 0.5f ? 0 : 1) != player) continue;
                float d = (p - home).sqrMagnitude;
                if (d >= distance) continue;
                best = i;
                distance = d;
            }
            if (best >= 0) Accept(track, candidates[best], now);
        }

        private static void Accept(Track track, Candidate candidate, float now)
        {
            float dt = now - track.lastSeen;
            if (track.assigned && dt > 0.0001f && dt <= 0.5f)
            {
                Vector2 velocity = Vector2.ClampMagnitude((candidate.position - track.sample.position) / dt, 1.5f);
                track.velocity = Vector2.Lerp(track.velocity, velocity, 0.5f);
            }
            else track.velocity = Vector2.zero;
            track.sample = candidate;
            track.lastSeen = now;
            track.assigned = track.visible = true;
            track.recoveryFrames = 0;
        }

        private static void Clear(Track track)
        {
            track.assigned = track.visible = false;
            track.velocity = Vector2.zero;
            track.sample = default;
            track.lastSeen = float.NegativeInfinity;
            track.recoveryFrames = 0;
        }
    }
}
