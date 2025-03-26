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

			Console.Error.WriteLine($"{eventArgs.Level.GetLowerName()} {eventArgs.Source.SourceName} {eventArgs.Data}");
		}

		/// <inheritdoc />
		public void Dispose() { }

		private static string GetEnv(string variable) => System.Environment.GetEnvironmentVariable(variable);

		internal static readonly bool Enabled = GetEnv("BEPINEX_STANDARD_LOG") != null;
	}
}
