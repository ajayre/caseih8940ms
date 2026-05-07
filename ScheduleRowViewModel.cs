using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CaseIH8940MS;

public sealed class ScheduleRowViewModel : INotifyPropertyChanged
{
	public ScheduleRowViewModel(string description, string activity, double scheduledHourFromMidnight, int page)
	{
		Description = description;
		Activity = activity;
		ScheduledHourFromMidnight = scheduledHourFromMidnight;
		Page = page;
	}

	public string Description { get; }
	public string Activity { get; }
	/// <summary>App timeline hour for this row (see <see cref="ScheduleEvent"/>).</summary>
	public double ScheduledHourFromMidnight { get; }
	public int Page { get; }

	string _relativeTimeText = "0.0";
	public string RelativeTimeText
	{
		get => _relativeTimeText;
		set => SetField(ref _relativeTimeText, value);
	}

	Color _textColor = Colors.Black;
	public Color TextColor
	{
		get => _textColor;
		set => SetField(ref _textColor, value);
	}

	Color _rowBackground = Colors.Transparent;
	public Color RowBackground
	{
		get => _rowBackground;
		set => SetField(ref _rowBackground, value);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged([CallerMemberName] string? name = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;
		field = value;
		OnPropertyChanged(name);
		return true;
	}
}
