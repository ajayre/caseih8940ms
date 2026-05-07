using Microsoft.Maui.Storage;

namespace CaseIH8940MS;

public static class CurrentHoursSettings
{
	const string Key = "current_hours_v1";

	public static double GetOrDefault(double defaultIfNeverSaved)
	{
		if (!Preferences.Default.ContainsKey(Key))
			return defaultIfNeverSaved;
		return Preferences.Default.Get(Key, 0.0);
	}

	public static void Set(double value)
	{
		var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
		Preferences.Default.Set(Key, rounded);
	}
}
