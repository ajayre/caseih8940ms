namespace CaseIH8940MS;

/// <summary>Ensures each recurring rule has upcoming instances within a bounded planning horizon.</summary>
public static class ScheduleActivityGenerator
{
	/// <summary>Target count of strictly-future occurrences per rule series inside the horizon (shorter intervals fill more slots).</summary>
	public const int MinimumFutureOccurrencesPerRule = 15;

	/// <summary>No generated (or retained) schedule time may exceed current hours + this value (~62 days at 1500).</summary>
	public const double MaxFutureHorizonHours = 1500.0;

	public static int CountStrictlyFuture(IReadOnlyList<ScheduleEvent> all, double currentHours)
	{
		var cur = Math.Round(currentHours, 1, MidpointRounding.AwayFromZero);
		return all.Count(e =>
			Math.Round(e.ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero) > cur);
	}

	static bool SameRuleSeries(ScheduleEvent e, IScheduleActivityRule rule) =>
		e.Description == rule.Description && e.Activity == rule.Activity && e.Page == rule.Page;

	static int CountFutureForRule(IReadOnlyList<ScheduleEvent> all, double currentHours, IScheduleActivityRule rule)
	{
		var cur = Math.Round(currentHours, 1, MidpointRounding.AwayFromZero);
		return all.Count(e =>
			SameRuleSeries(e, rule)
			&& Math.Round(e.ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero) > cur);
	}

	public static void EnsureMinimumFutureActivities(
		List<ScheduleEvent> all,
		double currentHours,
		double anchorStartHours,
		IReadOnlyList<IScheduleActivityRule> rules)
	{
		var cur = Math.Round(currentHours, 1, MidpointRounding.AwayFromZero);
		var anchor = Math.Round(anchorStartHours, 1, MidpointRounding.AwayFromZero);
		var horizonEnd = Math.Round(cur + MaxFutureHorizonHours, 1, MidpointRounding.AwayFromZero);

		// Drop ultra-far-future rows (from older logic or wide intervals) so the list stays practical.
		all.RemoveAll(e =>
			Math.Round(e.ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero) > horizonEnd);

		foreach (var rule in rules)
		{
			for (var guard = 0;
			     guard < 500 && CountFutureForRule(all, cur, rule) < MinimumFutureOccurrencesPerRule;
			     guard++)
			{
				var next = rule.TryComputeNextOccurrence(all, anchor, cur);
				if (!next.HasValue)
					break;

				var t = Math.Round(next.Value, 1, MidpointRounding.AwayFromZero);
				if (t > horizonEnd)
					break;

				if (all.Any(e =>
					    SameRuleSeries(e, rule)
					    && Math.Abs(Math.Round(e.ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero) - t) <
					    0.0001))
					break;

				all.Add(new ScheduleEvent(rule.Description, rule.Activity, t, rule.Page));
			}
		}

		all.Sort(static (a, b) =>
			a.ScheduledHourFromMidnight.CompareTo(b.ScheduledHourFromMidnight));
	}
}
