namespace CaseIH8940MS;

/// <summary>Builds new scheduled events from patterns; existing persisted rows are never overwritten.</summary>
public interface IScheduleActivityRule
{
	string Description { get; }
	string Activity { get; }
	int Page { get; }

	/// <summary>Next occurrence hour on the app timeline, or <see langword="null"/> if none can be produced.</summary>
	double? TryComputeNextOccurrence(IReadOnlyList<ScheduleEvent> existing, double anchorStartHours, double currentHours);
}

/// <summary>Registry of rules used when appending activities. Add new rule types here.</summary>
public static class ScheduleActivityRuleRegistry
{
	public static IReadOnlyList<IScheduleActivityRule> All { get; } = new IScheduleActivityRule[]
	{
		// Engine oil: page 217 (manual reference).
		new EveryNHoursFromStartRule("Engine oil level", "Check", 10.0, 217),
		new EveryNHoursFromStartRule("Coolant deaeration tank (sight gauge)", "Check", 10.0, 221),
		new EveryNHoursFromStartRule("Trans oil level", "Check", 10.0, 242),
		new EveryNHoursFromStartRule("Grease fittings", "Grease", 10.0, 207),
		new EveryNHoursFromStartRule("Grille screens, radiator, condenser, oil cooler", "Clean", 50.0, 227),
        new EveryNHoursFromStartRule("Tire pressures", "Check", 50.0, 156),
        new EveryNHoursFromStartRule("Grease fittings", "Grease", 50.0, 208),
        new EveryNHoursFromStartRule("Fuel tank water", "Drain", 50.0, 230),
        new EveryNHoursFromStartRule("Cab air filter", "Clean", 100.0, 257),
        new EveryNHoursFromStartRule("Wheel and hub bolt torques", "Check", 100.0, 179),
        new EveryNHoursFromStartRule("Spark arrestor muffler", "Clean", 250.0, 248),
        new EveryNHoursFromStartRule("Engine oil and filter", "Change", 250.0, 217),
        new EveryNHoursFromStartRule("Coolant hose clamps", "Check", 250.0, 221),
        new EveryNHoursFromStartRule("Air intake hoses", "Check", 250.0, 235),
        new EveryNHoursFromStartRule("MFD diff and planetary oil level", "Check", 250.0, 246),
        new EveryNHoursFromStartRule("Compressor belt adjustment", "Check", 250.0, 250),
        new EveryNHoursFromStartRule("Battery water level", "Check", 250.0, 266),
        new EveryNHoursFromStartRule("Primary fuel filter water", "Drain", 250.0, 230),
        new EveryNHoursFromStartRule("Coolant filter", "Change", 500.0, 226),
        new EveryNHoursFromStartRule("Fuel filters", "Change", 500.0, 231),
        new EveryNHoursFromStartRule("Aspirated precleaner", "Clean", 1000.0, 236),
        new EveryNHoursFromStartRule("Hydraulic suction screen", "Clean", 1000.0, 246),
        new EveryNHoursFromStartRule("Primary and secondary air filters", "Change", 1000.0, 237),
        new EveryNHoursFromStartRule("Transmission filters", "Change", 1000.0, 244),
        new EveryNHoursFromStartRule("Hydraulic breather", "Change", 1000.0, 246),
        new EveryNHoursFromStartRule("MFD diff and planetary oil", "Change", 1000.0, 247),
        new EveryNHoursFromStartRule("Coolant concentration", "Check", 1000.0, 223),
        new EveryNHoursFromStartRule("Engine valve adjustment", "Check", 1000.0, 250),
        new EveryNHoursFromStartRule("Transmission oil", "Change", 1500.0, 242),
        new EveryNHoursFromStartRule("Fuel injection pump and nozzles", "Change", 2000.0, 230),
        new EveryNHoursFromStartRule("Fuel injection pump and nozzles", "Check", 2000.0, 230),
        new EveryNHoursFromStartRule("Crankshaft torsional damper", "Check", 4000.0, 230),
        new EveryNHoursFromStartRule("Primary air filter", "Check/Clean", 10.0, 237),
        new EveryNHoursFromStartRule("Accumulators", "Check", 50.0, 246),
    };
}

/// <summary>Repeats every <see cref="IntervalHours"/> from <paramref name="anchorStartHours"/> (starting hours).</summary>
public sealed class EveryNHoursFromStartRule : IScheduleActivityRule
{
	public string Description { get; }
	public string Activity { get; }
	public double IntervalHours { get; }
	public int Page { get; }

	public EveryNHoursFromStartRule(string description, string activity, double intervalHours, int page)
	{
		Description = description;
		Activity = activity;
		IntervalHours = intervalHours;
		Page = page;
	}

	public double? TryComputeNextOccurrence(IReadOnlyList<ScheduleEvent> existing, double anchorStartHours, double currentHours)
	{
		if (IntervalHours <= 0)
			return null;

		var anchor = Math.Round(anchorStartHours, 1, MidpointRounding.AwayFromZero);
		var cur = Math.Round(currentHours, 1, MidpointRounding.AwayFromZero);

		// Same Description+Activity can appear on multiple intervals (e.g. grease 10h vs 50h) — Page disambiguates the series.
		double? maxForRule = null;
		foreach (var e in existing)
		{
			if (e.Description != Description || e.Activity != Activity || e.Page != Page)
				continue;
			var t = Math.Round(e.ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero);
			if (!maxForRule.HasValue || t > maxForRule.Value)
				maxForRule = t;
		}

		// Due times are start + interval, start + 2×interval, … (first maintenance is interval hours after start reading).
		var firstKnot = Math.Round(anchor + IntervalHours, 1, MidpointRounding.AwayFromZero);

		double candidate;
		if (!maxForRule.HasValue)
			candidate = firstKnot;
		else
		{
			var m = maxForRule.Value;
			if (m < anchor)
				candidate = firstKnot;
			else
				candidate = Math.Round(m + IntervalHours, 1, MidpointRounding.AwayFromZero);
		}

		while (candidate <= cur)
			candidate = Math.Round(candidate + IntervalHours, 1, MidpointRounding.AwayFromZero);

		while (existing.Any(e =>
			       e.Description == Description
			       && e.Activity == Activity
			       && e.Page == Page
			       && Math.Abs(Math.Round(e.ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero) - candidate) <
			       0.0001))
			candidate = Math.Round(candidate + IntervalHours, 1, MidpointRounding.AwayFromZero);

		return candidate;
	}
}
