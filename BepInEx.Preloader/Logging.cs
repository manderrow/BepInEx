using System;

namespace BepInEx.Preloader {
	internal static class Logging {
		private static System.IO.TextWriter _Stderr;

		public static System.IO.TextWriter Stderr
		{
			get {
				if (_Stderr == null) {
					_Stderr = Console.Error;
				}
				return _Stderr;
			}
		}
	}
}