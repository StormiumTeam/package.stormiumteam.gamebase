using System;
using UnityEngine;

namespace StormiumTeam.GameBase
{
    public static class GameDebug
    {
        static System.IO.StreamWriter logFile = null;
        static bool forwardToDebug = true;

        public static event Action<string, LogType> onLogUpdate;
        
        public static void Init(string logfilePath, string logBaseName)
        {
            forwardToDebug                 =  Application.isEditor;
            Application.logMessageReceived += LogCallback;

            // Try creating logName; attempt a number of suffixxes
            string name = "";
            for (var i = 0; i < 10; i++)
            {
                name = logBaseName + (i == 0 ? "" : "_" + i) + ".log";
                try
                {
                    logFile           = System.IO.File.CreateText(logfilePath + "/" + name);
                    logFile.AutoFlush = true;
                    break;
                }
                catch
                {
                    name = "<none>";
                }
            }

            GameDebug.Log("GameDebug initialized. Logging to " + logfilePath + "/" + name);
        }

        public static void Shutdown()
        {
            Application.logMessageReceived -= LogCallback;
            if (logFile != null)
                logFile.Close();
            logFile = null;
        }

        static void LogCallback(string message, string stack, LogType logtype)
        {
            switch (logtype)
            {
                default:
                case LogType.Log:
                    GameDebug._Log(message);
                    break;
                case LogType.Warning:
                    GameDebug._LogWarning(message);
                    break;
                case LogType.Error:
                    GameDebug._LogError(message);
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

        static void _Log(string message)
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

        static void _LogError(string message)
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

        static void _LogWarning(string message)
        {
            Console.Write(0 + ": [WARN] " + message);
            if (logFile != null)
                logFile.WriteLine("[WARN] " + message + "\n");
            
            onLogUpdate?.Invoke(message, LogType.Warning);
        }
    }
}