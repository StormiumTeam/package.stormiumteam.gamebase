using System;
using System.IO;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	public static class GameDebug
	{
		private static StreamWriter logFile;
		private static bool         forwardToDebug = true;

		public static event Action<string, LogType> onLogUpdate;

		public static void Init(string logfilePath, string logBaseName)
		{
			forwardToDebug                 =  Application.isEditor;
			Application.logMessageReceived += LogCallback;

			// Try creating logName; attempt a number of suffixxes
			var name = "";
			for (var i = 0; i < 10; i++)
			{
				name = logBaseName + (i == 0 ? "" : "_" + i) + ".log";
				try
				{
					logFile           = File.CreateText(logfilePath + "/" + name);
					logFile.AutoFlush = true;
					break;
				}
				catch
				{
					name = "<none>";
				}
			}

			Log("GameDebug initialized. Logging to " + logfilePath + "/" + name);
		}

		public static void Shutdown()
		{
			Application.logMessageReceived -= LogCallback;
			if (logFile != null)
				logFile.Close();
			logFile = null;
		}

		private static void LogCallback(string message, string stack, LogType logtype)
		{
			switch (logtype)
			{
				default:
				case LogType.Log:
					_Log(message);
					break;
				case LogType.Warning:
					_LogWarning(message);
					break;
				case LogType.Error:
					_LogError(message);
					break;
			}
		}

		public static void Log(string message)
		{
			if (forwardToDebug)
				Debug.Log(message);
			else
				_Log(message);
		}

		private static void _Log(string message)
		{
			Console.Write(0 + ": " + message);
			if (logFile != null)
				logFile.WriteLine(0 + ": " + message + "\n");

			onLogUpdate?.Invoke(message, LogType.Log);
		}

		public static void LogError(string message)
		{
			if (forwardToDebug)
				Debug.LogError(message);
			else
				_LogError(message);
		}

		private static void _LogError(string message)
		{
			Console.Write(0 + ": [ERR] " + message);
			if (logFile != null)
				logFile.WriteLine("[ERR] " + message + "\n");

			onLogUpdate?.Invoke(message, LogType.Error);
		}

		public static void LogWarning(string message)
		{
			if (forwardToDebug)
				Debug.LogWarning(message);
			else
				_LogWarning(message);
		}

		private static void _LogWarning(string message)
		{
			Console.Write(0 + ": [WARN] " + message);
			if (logFile != null)
				logFile.WriteLine("[WARN] " + message + "\n");

			onLogUpdate?.Invoke(message, LogType.Warning);
		}
	}
}