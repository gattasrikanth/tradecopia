namespace TradeCopia.Analytics;

public sealed record ReliabilityStats(
    int ActionsAttempted,
    int ActionsAcknowledged,
    int Rejects,
    int Divergences,
    double? DecisionLatencyP50Ms,
    double? DecisionLatencyP95Ms,
    double? DecisionLatencyP99Ms,
    int SampleCount);

public static class ReliabilityCalculator
{
    public static ReliabilityStats FromSamples(
        int attempted,
        int acknowledged,
        int rejects,
        int divergences,
        IReadOnlyList<double> decisionMs)
    {
        var sorted = decisionMs.OrderBy(x => x).ToArray();
        return new ReliabilityStats(
            attempted,
            acknowledged,
            rejects,
            divergences,
            Percentile(sorted, 0.50),
            Percentile(sorted, 0.95),
            Percentile(sorted, 0.99),
            sorted.Length);
    }

    public static double? Percentile(IReadOnlyList<double> sortedAscending, double p)
    {
        if (sortedAscending.Count == 0)
        {
            return null;
        }

        if (p <= 0)
        {
            return sortedAscending[0];
        }

        if (p >= 1)
        {
            return sortedAscending[^1];
        }

        var index = (int)Math.Ceiling(p * sortedAscending.Count) - 1;
        if (index < 0)
        {
            index = 0;
        }

        if (index >= sortedAscending.Count)
        {
            index = sortedAscending.Count - 1;
        }

        return sortedAscending[index];
    }
}
