using System.Globalization;

namespace CaseIH8940MS;

public partial class SettingsPage : ContentPage
{
	bool _suppress;

	public SettingsPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		ApplyStoredValueToField();
	}

	void ApplyStoredValueToField()
	{
		_suppress = true;
		HoursEntry.Text = FormatOneDecimal(StartingHoursSettings.Get());
		_suppress = false;
	}

	static string FormatOneDecimal(double value) =>
		Math.Round(value, 1, MidpointRounding.AwayFromZero)
			.ToString("0.0", CultureInfo.CurrentCulture);

	void OnHoursEntryCompleted(object? sender, EventArgs e) => NormalizeAndSave();

	void OnHoursEntryUnfocused(object? sender, FocusEventArgs e) => NormalizeAndSave();

	async void OnBackTapped(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}

	void NormalizeAndSave()
	{
		if (_suppress)
			return;

		var text = HoursEntry.Text?.Trim() ?? string.Empty;
		double value = 0;
		if (!string.IsNullOrEmpty(text))
		{
			if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
			    && !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				ApplyStoredValueToField();
				return;
			}
		}

		StartingHoursSettings.Set(value);
		_suppress = true;
		HoursEntry.Text = FormatOneDecimal(StartingHoursSettings.Get());
		_suppress = false;
	}
}
