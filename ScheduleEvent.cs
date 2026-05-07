namespace CaseIH8940MS;

/// <summary>
/// Scheduled time on the app timeline (same units as <see cref="CurrentHoursSettings"/>).
/// </summary>
public sealed class ScheduleEvent
{
	public ScheduleEvent(string description, string activity, double scheduledHourFromMidnight, int page)
	{
		Description = description;
		Activity = activity;
		ScheduledHourFromMidnight = scheduledHourFromMidnight;
		Page = page;
	}

	public string Description { get; }
	public string Activity { get; }
	public double ScheduledHourFromMidnight { get; }
	public int Page { get; }
}
