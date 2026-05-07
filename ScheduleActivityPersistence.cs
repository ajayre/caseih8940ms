using System.Text.Json;
using Microsoft.Maui.Storage;

namespace CaseIH8940MS;

/// <summary>Loads and saves generated schedule rows; append-only growth, never bulk re-generated.</summary>
public static class ScheduleActivityPersistence
{
	const int StorageSchemaVersion = 6;
	const string SchemaPreferenceKey = "schedule_storage_schema_v1";
	const string FileName = "schedule_activities_v2.json";
	const string LegacyV1FileName = "schedule_activities_v1.json";

	static string FilePath => Path.Combine(FileSystem.AppDataDirectory, FileName);
	static string LegacyV1Path => Path.Combine(FileSystem.AppDataDirectory, LegacyV1FileName);

	public static List<ScheduleEvent> Load()
	{
		var storedSchema = Preferences.Default.Get(SchemaPreferenceKey, 0);
		if (storedSchema < StorageSchemaVersion)
		{
			TryDelete(LegacyV1Path);
			TryDelete(FilePath);
			Preferences.Default.Set(SchemaPreferenceKey, StorageSchemaVersion);
			return new List<ScheduleEvent>();
		}

		try
		{
			if (!File.Exists(FilePath))
				return new List<ScheduleEvent>();
			var json = File.ReadAllText(FilePath);
			var dto = JsonSerializer.Deserialize<ScheduleFileDto>(json);
			if (dto?.Items is not { Count: > 0 })
				return new List<ScheduleEvent>();
			return dto.Items
				.Select(static i => new ScheduleEvent(
					i.Description,
					i.Activity,
					Math.Round(i.ScheduledHour, 1, MidpointRounding.AwayFromZero),
					i.Page))
				.ToList();
		}
		catch
		{
			return new List<ScheduleEvent>();
		}
	}

	public static void Save(IReadOnlyList<ScheduleEvent> items)
	{
		var ordered = items
			.OrderBy(static e => e.ScheduledHourFromMidnight)
			.Select(static e => new ScheduleFileItemDto
			{
				Description = e.Description,
				Activity = e.Activity,
				ScheduledHour = Math.Round(e.ScheduledHourFromMidnight, 1, MidpointRounding.AwayFromZero),
				Page = e.Page
			})
			.ToList();
		var dto = new ScheduleFileDto { Format = 2, Items = ordered };
		var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(FilePath, json);
	}

	static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
				File.Delete(path);
		}
		catch
		{
			/* ignore */
		}
	}

	sealed class ScheduleFileDto
	{
		public int Format { get; set; } = 2;
		public List<ScheduleFileItemDto> Items { get; set; } = new();
	}

	sealed class ScheduleFileItemDto
	{
		public string Description { get; set; } = string.Empty;
		public string Activity { get; set; } = string.Empty;
		public double ScheduledHour { get; set; }
		public int Page { get; set; }
	}
}
