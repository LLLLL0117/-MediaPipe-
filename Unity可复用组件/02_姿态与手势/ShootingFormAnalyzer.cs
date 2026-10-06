namespace EchoWorkSpace
{
    public enum ShootingPhase
    {
        Unknown,
        GatherPower,
        Release,
    }

    public readonly struct ShootingFormAnalysis
    {
        public ShootingFormAnalysis(
            ShootingPhase phase,
            float? elbowAverage,
            float? rightElbow,
            float? shoulderAverage,
            bool hasIssue,
            string phaseText,
            string issueText)
        {
            Phase = phase;
            ElbowAverage = elbowAverage;
            RightElbow = rightElbow;
            ShoulderAverage = shoulderAverage;
            HasIssue = hasIssue;
            PhaseText = phaseText;
            IssueText = issueText;
        }

        public ShootingPhase Phase { get; }
        public float? ElbowAverage { get; }
        public float? RightElbow { get; }
        public float? ShoulderAverage { get; }
        public bool HasIssue { get; }
        public string PhaseText { get; }
        public string IssueText { get; }
    }

    public static class ShootingFormAnalyzer
    {
        public const float GatherPowerElbowMin = 60f;
        public const float GatherPowerElbowMax = 130f;
        public const float GatherPowerShoulderMin = 70f;
        public const float GatherPowerShoulderMax = 120f;
        public const float ReleaseRightElbowMin = 120f;
        public const float ReleaseRightElbowMax = 180f;

        public const string ShoulderIssueText = "\u80a9\u90e8\u6446\u653e\u4e0d\u5408\u7406";
        public const string ElbowIssueText = "\u51fa\u624b\u644a\u76f4";
        public const string GatherPowerPoseIssueText = "\u8bf7\u4fdd\u6301\u4e3e\u7403\u51c6\u5907\u59ff\u6001";
        public const string StandardText = "\u6807\u51c6";
        public const string UnknownText = "--";

        public static bool IsGatherPowerCorrect(float? elbowAverage, float? shoulderAverage)
        {
            return elbowAverage.HasValue
                && IsInRange(elbowAverage.Value, GatherPowerElbowMin, GatherPowerElbowMax)
                && IsShoulderGatherPowerNormal(shoulderAverage);
        }

        public static bool IsReleaseCorrect(float? rightElbow)
        {
            return rightElbow.HasValue && IsInRange(rightElbow.Value, ReleaseRightElbowMin, ReleaseRightElbowMax);
        }

        public static string GetGatherPowerIssue(float? elbowAverage, float? shoulderAverage)
        {
            if (!elbowAverage.HasValue || !shoulderAverage.HasValue)
                return UnknownText;

            if (!IsInRange(elbowAverage.Value, GatherPowerElbowMin, GatherPowerElbowMax))
                return GatherPowerPoseIssueText;

            return IsShoulderGatherPowerNormal(shoulderAverage)
                ? StandardText
                : ShoulderIssueText;
        }

        public static string GetReleaseIssue(float? rightElbow)
        {
            if (!rightElbow.HasValue)
                return UnknownText;

            return IsReleaseCorrect(rightElbow) ? StandardText : ElbowIssueText;
        }

        public static ShootingFormAnalysis Analyze(
            float? elbowAverage,
            float? rightElbow,
            float? shoulderAverage)
        {
            if (elbowAverage.HasValue && IsInRange(elbowAverage.Value, GatherPowerElbowMin, GatherPowerElbowMax))
            {
                bool shoulderOk = IsShoulderGatherPowerNormal(shoulderAverage);
                return new ShootingFormAnalysis(
                    ShootingPhase.GatherPower,
                    elbowAverage,
                    rightElbow,
                    shoulderAverage,
                    !shoulderOk,
                    "\u4e3e\u7403\u84c4\u529b\u9636\u6bb5",
                    shoulderOk ? StandardText : ShoulderIssueText);
            }

            if (rightElbow.HasValue && IsInRange(rightElbow.Value, ReleaseRightElbowMin, ReleaseRightElbowMax))
            {
                return new ShootingFormAnalysis(
                    ShootingPhase.Release,
                    elbowAverage,
                    rightElbow,
                    shoulderAverage,
                    false,
                    "\u51fa\u624b\u9636\u6bb5",
                    StandardText);
            }

            if (rightElbow.HasValue)
            {
                return new ShootingFormAnalysis(
                    ShootingPhase.Release,
                    elbowAverage,
                    rightElbow,
                    shoulderAverage,
                    true,
                    "\u51fa\u624b\u9636\u6bb5",
                    ElbowIssueText);
            }

            return CreateUnknown(elbowAverage, rightElbow, shoulderAverage);
        }

        private static bool IsShoulderGatherPowerNormal(float? shoulderAverage)
        {
            return shoulderAverage.HasValue
                && IsInRange(shoulderAverage.Value, GatherPowerShoulderMin, GatherPowerShoulderMax);
        }

        private static ShootingFormAnalysis CreateUnknown(
            float? elbowAverage,
            float? rightElbow,
            float? shoulderAverage)
        {
            return new ShootingFormAnalysis(
                ShootingPhase.Unknown,
                elbowAverage,
                rightElbow,
                shoulderAverage,
                false,
                "\u672a\u8bc6\u522b",
                UnknownText);
        }

        private static bool IsInRange(float value, float minInclusive, float maxInclusive)
        {
            return value >= minInclusive && value <= maxInclusive;
        }
    }
}
