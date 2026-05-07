using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace CaseIH8940MS;

public partial class MainPage : ContentPage
{
	readonly List<ScheduleEvent> _scheduleActivities = new();
	readonly ObservableCollection<ScheduleRowViewModel> ScheduleRows = new();

	double _currentHours;

	/// <summary>Last row index we auto-centered on; -1 means "not yet this visit" (allows one center after appear).</summary>
	int _lastAutoScrolledToIndex = -1;

	public MainPage()
	{
		InitializeComponent();
		ScheduleCollectionView.ItemsSource = ScheduleRows;

		var wallNow = Math.Round(DateTime.Now.TimeOfDay.TotalHours, 1, MidpointRounding.AwayFromZero);
		_currentHours = Math.Round(CurrentHoursSettings.GetOrDefault(wallNow), 1, MidpointRounding.AwayFromZero);
		ClampCurrentHoursToStartingIfNeeded();

		LoadPersistedScheduleAndEnsureFuture();

		CurrentHoursEntry.Text = FormatOneDecimal(_currentHours);

#if WINDOWS
		ApplyWindowsPhoneChrome();
#else
		ApplyFullBleedPhoneChrome();
#endif
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (ClampCurrentHoursToStartingIfNeeded())
			_lastAutoScrolledToIndex = -1;

		LoadPersistedScheduleAndEnsureFuture();

		Dispatcher.Dispatch(RefreshSchedule);
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), RefreshSchedule);
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_lastAutoScrolledToIndex = -1;
	}

	/// <summary>Load from disk, append via rules if fewer than 20 future rows, save, refresh UI rows.</summary>
	void LoadPersistedScheduleAndEnsureFuture()
	{
		var anchor = Math.Round(StartingHoursSettings.Get(), 1, MidpointRounding.AwayFromZero);
		var cur = Math.Round(_currentHours, 1, MidpointRounding.AwayFromZero);

		_scheduleActivities.Clear();
		_scheduleActivities.AddRange(ScheduleActivityPersistence.Load());

		ScheduleActivityGenerator.EnsureMinimumFutureActivities(
			_scheduleActivities,
			cur,
			anchor,
			ScheduleActivityRuleRegistry.All);

		ScheduleActivityPersistence.Save(_scheduleActivities);
		RebuildScheduleRowsFromTemplate();
	}

	void RebuildScheduleRowsFromTemplate()
	{
		ScheduleRows.Clear();
		foreach (var e in _scheduleActivities)
			ScheduleRows.Add(new ScheduleRowViewModel(e.Description, e.Activity, e.ScheduledHourFromMidnight, e.Page));
		_lastAutoScrolledToIndex = -1;
	}

#if WINDOWS
	private void ApplyWindowsPhoneChrome()
	{
		RootGrid.BackgroundColor = Color.FromArgb("#1C1C1E");
		DeviceBezel.HorizontalOptions = LayoutOptions.Center;
		DeviceBezel.VerticalOptions = LayoutOptions.Center;
		DeviceBezel.WidthRequest = 402;
		DeviceBezel.HeightRequest = 874;
		DeviceBezel.BackgroundColor = Color.FromArgb("#2C2C2E");
		DeviceBezel.Padding = 3;
		DeviceBezel.StrokeShape = new RoundRectangle { CornerRadius = 44 };
		ScreenBorder.StrokeShape = new RoundRectangle { CornerRadius = 41 };
	}
#else
	private void ApplyFullBleedPhoneChrome()
	{
		RootGrid.BackgroundColor = Colors.Transparent;
		DeviceBezel.ClearValue(VisualElement.WidthRequestProperty);
		DeviceBezel.ClearValue(VisualElement.HeightRequestProperty);
		DeviceBezel.Margin = 0;
		DeviceBezel.Padding = 0;
		DeviceBezel.BackgroundColor = Colors.Transparent;
		DeviceBezel.HorizontalOptions = LayoutOptions.Fill;
		DeviceBezel.VerticalOptions = LayoutOptions.Fill;
		DeviceBezel.StrokeShape = new RoundRectangle { CornerRadius = 0 };
		ScreenBorder.StrokeShape = new RoundRectangle { CornerRadius = 0 };
	}
#endif

	/// <returns><see langword="true"/> if current hours were raised to starting hours (and persisted).</returns>
	bool ClampCurrentHoursToStartingIfNeeded()
	{
		var minH = Math.Round(StartingHoursSettings.Get(), 1, MidpointRounding.AwayFromZero);
		if (_currentHours >= minH)
			return false;

		_currentHours = minH;
		CurrentHoursSettings.Set(_currentHours);
		NormalizeCurrentHoursField();
		return true;
	}

	void RefreshSchedule()
	{
		if (ScheduleRows.Count == 0 || _scheduleActivities.Count != ScheduleRows.Count)
			return;

		var currentH = Math.Round(_currentHours, 1, MidpointRounding.AwayFromZero);

		var activityAbsHours = new double[_scheduleActivities.Count];
		for (var i = 0; i < _scheduleActivities.Count; i++)
			activityAbsHours[i] = Math.Round(_scheduleActivities[i].ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero);

		var displayDeltas = new double[ScheduleRows.Count];
		for (var i = 0; i < ScheduleRows.Count; i++)
			displayDeltas[i] = Math.Round(activityAbsHours[i] - currentH, 1, MidpointRounding.AwayFromZero);

		var currentIdx = FindHighlightedRowIndex(displayDeltas);

		var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
		var pastText = dark ? Color.FromArgb("#98989D") : Color.FromArgb("#6E6E6E");
		var futureText = dark ? Colors.White : Colors.Black;
		var highlightBg = dark ? Color.FromArgb("#2C4A6E") : Color.FromArgb("#D6E8FF");

		for (var i = 0; i < ScheduleRows.Count; i++)
		{
			var row = ScheduleRows[i];
			var deltaH = displayDeltas[i];
			row.RelativeTimeText = deltaH.ToString("0.0", CultureInfo.InvariantCulture);

			var isCurrent = i == currentIdx;

			row.RowBackground = isCurrent ? highlightBg : Colors.Transparent;
			row.TextColor = isCurrent ? futureText : (deltaH < 0 ? pastText : futureText);
		}

		if (currentIdx < 0 || currentIdx >= ScheduleRows.Count)
			return;

		// Re-center only when the "current" activity changes (or first layout this visit).
		if (currentIdx == _lastAutoScrolledToIndex)
			return;

		_lastAutoScrolledToIndex = currentIdx;
		var target = ScheduleRows[currentIdx];
		MainThread.BeginInvokeOnMainThread(() =>
		{
			try
			{
				ScheduleCollectionView.ScrollTo(target, position: ScrollToPosition.Center, animate: false);
			}
			catch
			{
				// Layout not ready yet
			}
		});
	}

	/// <summary>
	/// Prefer a row whose display delta is exactly 0; else the first row with a positive delta;
	/// if all deltas are negative, the last row (everything is in the past).
	/// </summary>
	static int FindHighlightedRowIndex(IReadOnlyList<double> roundedDisplayDeltas)
	{
		if (roundedDisplayDeltas.Count == 0)
			return 0;

		for (var i = 0; i < roundedDisplayDeltas.Count; i++)
		{
			if (roundedDisplayDeltas[i] == 0.0)
				return i;
		}

		for (var i = 0; i < roundedDisplayDeltas.Count; i++)
		{
			if (roundedDisplayDeltas[i] > 0.0)
				return i;
		}

		return roundedDisplayDeltas.Count - 1;
	}

	void OnCurrentHoursCompleted(object? sender, EventArgs e) => OnApplyCurrentHoursClicked(sender, e);

	async void OnApplyCurrentHoursClicked(object? sender, EventArgs e)
	{
		var text = CurrentHoursEntry.Text?.Trim() ?? string.Empty;
		var minH = Math.Round(StartingHoursSettings.Get(), 1, MidpointRounding.AwayFromZero);
		if (string.IsNullOrEmpty(text))
		{
			_currentHours = minH;
			CurrentHoursSettings.Set(_currentHours);
			NormalizeCurrentHoursField();
			_lastAutoScrolledToIndex = -1;
			LoadPersistedScheduleAndEnsureFuture();
			RefreshSchedule();
			return;
		}

		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
		    && !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
		{
			await DisplayAlertAsync("Invalid value", "Enter a valid number.", "OK");
			NormalizeCurrentHoursField();
			return;
		}

		var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
		if (rounded < minH)
		{
			await DisplayAlertAsync(
				"Too low",
				$"Current hours cannot be less than starting hours ({FormatOneDecimal(minH)}).",
				"OK");
			NormalizeCurrentHoursField();
			return;
		}

		_currentHours = rounded;
		CurrentHoursSettings.Set(_currentHours);
		NormalizeCurrentHoursField();
		_lastAutoScrolledToIndex = -1;
		LoadPersistedScheduleAndEnsureFuture();
		RefreshSchedule();
	}

	void NormalizeCurrentHoursField()
	{
		CurrentHoursEntry.Text = FormatOneDecimal(_currentHours);
	}

	static string FormatOneDecimal(double value) =>
		Math.Round(value, 1, MidpointRounding.AwayFromZero)
			.ToString("0.0", CultureInfo.CurrentCulture);

	async void OnSettingsClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("settings");
	}
}
