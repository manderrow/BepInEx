using System;
using BepInEx.Configuration;

namespace BepInEx.Logging
{
	/// <summary>
	/// Logs entries to stderr.
	/// </summary>
	public class StandardLogListener : ILogListener
	{
		internal bool WriteUnityLogs { get; set; } = true;

		/// <inheritdoc />
		public void LogEvent(object sender, LogEventArgs eventArgs)
		{
			if (!WriteUnityLogs && sender is UnityLogSource)
				return;
			if ((eventArgs.Level & ConfigDisplayedLevel.Value) == 0)
				return;

			Console.Error.WriteLine(eventArgs.ToString());
		}

		/// <inheritdoc />
		public void Dispose() { }

		private static readonly ConfigEntry<LogLevel> ConfigDisplayedLevel = ConfigFile.CoreConfig.Bind(
			"Logging.Standard","LogLevels",
			LogLevel.All,
			"Which log levels to write to stderr.");

		internal static readonly ConfigEntry<bool> ConfigEnabled = ConfigFile.CoreConfig.Bind(
			"Logging.Standard", "Enabled",
			true,
			"Enables writing log messages to stderr.");
	}
}
