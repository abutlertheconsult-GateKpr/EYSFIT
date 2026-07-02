using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EYSFIT.Backend
{
    public static class HealthCoachPromptBuilder
    {
        public static string BuildLlmHealthCoachPrompt(UserProfile user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var sb = new StringBuilder();
            sb.AppendLine("Act as an expert clinical sports medicine physician, longevity coach, and certified personal trainer. Analyze my biometric trends, current goals, and training data sequentially using the framework below.");
            sb.AppendLine();
            sb.AppendLine("### 1. User Profile & Core Goals");
            sb.AppendLine($"* Age/Sex: {user.Age} / {user.Sex}");
            sb.AppendLine($"* Body Type: {user.BodyType?.Trim() ?? "Not specified"}");
            sb.AppendLine($"* Primary Health & Fitness Goals: {user.Goals?.Trim() ?? "N/A"}");
            sb.AppendLine();
            sb.AppendLine("### 2. Biometric & Health Metrics (Historical Baseline vs. Current)");

            if (user.Biometrics != null && user.Biometrics.Any())
            {
                foreach (var metric in user.Biometrics)
                {
                    var pastLabel = GetPastLabel(metric);
                    var currentLabel = GetCurrentLabel(metric);
                    sb.AppendLine($"* {metric.Name}: {pastLabel} vs. {currentLabel}");
                }
            }
            else
            {
                sb.AppendLine("* No biometric metrics available yet; starting baseline routine.");
            }

            sb.AppendLine();
            sb.AppendLine("### 3. Logged Workout Performance Averages (Past Days)");

            var workoutLines = BuildWorkoutSummaryLines(user.WorkoutSummaries);
            if (workoutLines.Any())
            {
                foreach (var line in workoutLines)
                {
                    sb.AppendLine(line);
                }
            }
            else
            {
                sb.AppendLine("* No current workout averages logged yet; starting baseline routine.");
            }

            sb.AppendLine();
            sb.AppendLine("### 4. Your Step-by-Step Analysis Framework:");
            sb.AppendLine();
            sb.AppendLine("Step 1: Current Snapshot (\"How I am doing right now\")");
            sb.AppendLine("Evaluate my most recent stats and workout averages. Give a transparent look at my current physical condition relative to my primary goals. Highlight the clear \"Wins\" (where to maintain) and immediate \"Gaps\" (where to improve).");
            sb.AppendLine();
            sb.AppendLine("Step 2: Immediate Comparison (\"How I am doing compared to my last stats\")");
            sb.AppendLine("Compare my current averages directly against my immediate last recorded baseline. Am I trending in the right direction, stalling, or showing signs of systemic fatigue/regression?");
            sb.AppendLine();
            sb.AppendLine("Step 3: Deep Dive Metrics Focus");
            sb.AppendLine("Pay special attention to Blood Pressure and VO2 Max (if present in the data above). Evaluate them against clinical standards and explain how my sleep, stress, and workout metrics are directly impacting them. If other advanced metrics are present (such as body composition or recovery scores), seamlessly integrate them into this analysis.");
            sb.AppendLine();
            sb.AppendLine("Step 4: Prompt for Custom Range (\"Set the user's horizon\")");
            sb.AppendLine("At the very end of your response, explicitly ask me what specific historical timeframe I would like to compare these numbers against next (e.g., \"Would you like to compare these stats against your 3-month baseline, 6-month baseline, or a specific custom date range to see your long-term trajectory?\").");

            return sb.ToString();
        }

        private static string GetPastLabel(BiometricMetric metric)
        {
            if (metric == null)
                return "N/A - Initial Baseline";

            if (metric.PastAverage.HasValue)
                return FormatMetricValue(metric.PastAverage.Value, metric.Name, metric.Unit);

            if (metric.LastRecorded.HasValue)
                return FormatMetricValue(metric.LastRecorded.Value, metric.Name, metric.Unit);

            return "N/A - Initial Baseline";
        }

        private static string GetCurrentLabel(BiometricMetric metric)
        {
            if (metric?.CurrentAverage.HasValue == true)
                return FormatMetricValue(metric.CurrentAverage.Value, metric.Name, metric.Unit);

            return "N/A";
        }

        private static IReadOnlyList<string> BuildWorkoutSummaryLines(List<WorkoutMetric> workouts)
        {
            if (workouts == null || !workouts.Any())
                return Array.Empty<string>();

            var resistanceWorkouts = workouts.Where(w => string.Equals(w.Category, "Resistance Training", StringComparison.OrdinalIgnoreCase)).ToList();
            var cardioWorkouts = workouts.Where(w => string.Equals(w.Category, "Cardio/Conditioning", StringComparison.OrdinalIgnoreCase) || string.Equals(w.Category, "Cardio", StringComparison.OrdinalIgnoreCase) || string.Equals(w.Category, "Conditioning", StringComparison.OrdinalIgnoreCase)).ToList();

            var lines = new List<string>();

            if (resistanceWorkouts.Any())
                lines.Add(BuildResistanceTrainingLine(resistanceWorkouts));

            if (cardioWorkouts.Any())
                lines.Add(BuildCardioConditioningLine(cardioWorkouts));

            return lines;
        }

        private static string BuildResistanceTrainingLine(IEnumerable<WorkoutMetric> workouts)
        {
            var sessions = AggregateValue(workouts.Select(w => w.SessionsPerWeek));
            var duration = AggregateValue(workouts.Select(w => w.AverageDurationMinutes));
            var parts = new List<string>();

            if (sessions.HasValue)
                parts.Add($"{sessions.Value:0.##} sessions/week");

            if (duration.HasValue)
                parts.Add($"avg duration {duration.Value:0.##} mins");

            return parts.Any()
                ? $"* Resistance Training: {string.Join(", ", parts)}"
                : "* Resistance Training: No session summary available";
        }

        private static string BuildCardioConditioningLine(IEnumerable<WorkoutMetric> workouts)
        {
            var sessions = AggregateValue(workouts.Select(w => w.SessionsPerWeek));
            var duration = AggregateValue(workouts.Select(w => w.AverageDurationMinutes));
            var modality = GetPrimaryModality(workouts);
            var parts = new List<string>();

            if (sessions.HasValue)
                parts.Add($"{sessions.Value:0.##} sessions/week");

            if (duration.HasValue)
                parts.Add($"avg duration {duration.Value:0.##} mins");

            var line = parts.Any()
                ? $"* Cardio/Conditioning: {string.Join(", ", parts)}"
                : "* Cardio/Conditioning: No session summary available";

            if (!string.IsNullOrWhiteSpace(modality))
                line += $" (Primary Modality: {modality})";

            return line;
        }

        private static double? AggregateValue(IEnumerable<double?> values)
        {
            var numeric = values.Where(v => v.HasValue).Select(v => v.Value).ToList();
            if (!numeric.Any())
                return null;

            return numeric.Average();
        }

        private static string GetPrimaryModality(IEnumerable<WorkoutMetric> workouts)
        {
            var modalities = workouts
                .Select(w => w.PrimaryModality)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .ToList();

            if (!modalities.Any())
                return string.Empty;

            return modalities
                .GroupBy(m => m, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .First();
        }

        private static string FormatMetricValue(double value, string metricName, string unit)
        {
            var formatted = value.ToString("0.##");
            var resolvedUnit = GetMetricUnit(metricName, unit);
            return string.IsNullOrWhiteSpace(resolvedUnit)
                ? formatted
                : $"{formatted} {resolvedUnit}";
        }

        private static string GetMetricUnit(string metricName, string overrideUnit)
        {
            if (!string.IsNullOrWhiteSpace(overrideUnit))
                return overrideUnit.Trim();

            if (string.IsNullOrWhiteSpace(metricName))
                return string.Empty;

            switch (metricName.Trim().ToLowerInvariant())
            {
                case "blood pressure":
                    return "mmHg";
                case "vo2 max":
                    return "mL/kg/min";
                case "body fat %":
                case "body fat":
                    return "%";
                case "sleep duration":
                case "sleep hours":
                    return "hours";
                case "hrv":
                    return "ms";
                case "rhr":
                case "resting heart rate":
                    return "bpm";
                default:
                    return string.Empty;
            }
        }
    }

    public class UserProfile
    {
        public int Age { get; set; }
        public string Sex { get; set; }
        public string BodyType { get; set; }
        public string Goals { get; set; }
        public List<BiometricMetric> Biometrics { get; set; } = new();
        public List<WorkoutMetric> WorkoutSummaries { get; set; } = new();
    }

    public class BiometricMetric
    {
        public string Name { get; set; }
        public double? LastRecorded { get; set; }
        public double? PastAverage { get; set; }
        public double? CurrentAverage { get; set; }
        public string Unit { get; set; }
    }

    public class WorkoutMetric
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public double? SessionsPerWeek { get; set; }
        public double? AverageDurationMinutes { get; set; }
        public string PrimaryModality { get; set; }
        public double? AverageValue { get; set; }
        public string Unit { get; set; }
        public string Summary { get; set; }
    }
}
