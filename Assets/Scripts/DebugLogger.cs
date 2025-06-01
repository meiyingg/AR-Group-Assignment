using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class DebugLogger : MonoBehaviour
{    public Text debugText;               // UI Text component for displaying logs
    public int fontSize = 24;            // Font size
    public int maxMessages = 20;         // Maximum number of messages to display
    public bool showTimestamp = true;    // Whether to show timestamp
    public Color normalColor = Color.white;      // Normal log color
    public Color warningColor = Color.yellow;    // Warning log color
    public Color errorColor = Color.red;         // Error log color

    private static DebugLogger instance;
    private Queue<LogMessage> logMessages;
    
    [System.Serializable]
    private class LogMessage
    {
        public string message;
        public LogType type;
        public string timestamp;
        public string stackTrace;
    }

    public static DebugLogger Instance
    {
        get { return instance; }
    }    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            logMessages = new Queue<LogMessage>();            // Set initial properties for text component
            if (debugText != null)
            {
                debugText.fontSize = fontSize;
                debugText.color = normalColor;
                debugText.supportRichText = true;
                debugText.alignment = TextAnchor.UpperLeft;
            }
            
            // Add initialization information
            AddLog("Debug log system started");
            AddLog($"Screen resolution: {Screen.width}x{Screen.height}");
            AddLog($"System info: {SystemInfo.operatingSystem}");
            AddLog($"Device model: {SystemInfo.deviceModel}");
        }
        else
        {
            Destroy(gameObject);
        }        // Register Unity's log callback
        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        AddLog(logString, type, stackTrace);
    }

    public void AddLog(string message, LogType type = LogType.Log, string stackTrace = "")
    {
        if (logMessages.Count >= maxMessages)
        {
            logMessages.Dequeue();
        }

        LogMessage logMessage = new LogMessage
        {
            message = message,
            type = type,
            timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
            stackTrace = stackTrace
        };

        logMessages.Enqueue(logMessage);
        UpdateDebugText();
    }    private void UpdateDebugText()
    {
        if (debugText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var log in logMessages)
            {
                string colorTag = GetColorTagForLogType(log.type);
                string entry = "";
                
                if (showTimestamp)
                    entry += $"[{log.timestamp}] ";
                
                entry += $"{log.message}";
                
                if (log.type == LogType.Error || log.type == LogType.Exception)
                    entry += $"\n{log.stackTrace}";
                
                sb.AppendLine($"{colorTag}{entry}</color>");
            }
            debugText.text = sb.ToString();
        }
    }

    private string GetColorTagForLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                return $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>";
            case LogType.Warning:
                return $"<color=#{ColorUtility.ToHtmlStringRGB(warningColor)}>";
            default:
                return $"<color=#{ColorUtility.ToHtmlStringRGB(normalColor)}>";
        }
    }    // Clear logs
    public void ClearLogs()
    {
        logMessages.Clear();
        UpdateDebugText();
    }
    
    // Add separator
    public void AddSeparator()
    {
        AddLog("----------------------------------------", LogType.Log);
    }
    
    // Add warning message
    public void AddWarning(string message)
    {
        AddLog(message, LogType.Warning);
    }
    
    // Add error message
    public void AddError(string message)
    {
        AddLog(message, LogType.Error);
    }
}
