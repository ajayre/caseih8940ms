using Microsoft.Maui.Storage;

namespace CaseIH8940MS;

public static class StartingHoursSettings
{
	const string Key = "starting_hours_v1";

	public static double Get() => Preferences.Default.Get(Key, 0.0);

	public static void Set(double value)
	{
		var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
		Preferences.Default.Set(Key, rounded);
	}
}
