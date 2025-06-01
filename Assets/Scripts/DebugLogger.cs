using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class DebugLogger : MonoBehaviour
{
    public Text debugText;               // UI Text组件用于显示日志
    public int fontSize = 24;            // 字体大小
    public int maxMessages = 20;         // 最多显示的消息数量
    public bool showTimestamp = true;    // 是否显示时间戳
    public Color normalColor = Color.white;      // 普通日志颜色
    public Color warningColor = Color.yellow;    // 警告日志颜色
    public Color errorColor = Color.red;         // 错误日志颜色

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
            logMessages = new Queue<LogMessage>();

            // 设置文本组件的初始属性
            if (debugText != null)
            {
                debugText.fontSize = fontSize;
                debugText.color = normalColor;
                debugText.supportRichText = true;
                debugText.alignment = TextAnchor.UpperLeft;
            }
            
            // 添加初始化信息
            AddLog("调试日志系统已启动");
            AddLog($"屏幕分辨率: {Screen.width}x{Screen.height}");
            AddLog($"系统信息: {SystemInfo.operatingSystem}");
            AddLog($"设备型号: {SystemInfo.deviceModel}");
        }
        else
        {
            Destroy(gameObject);
        }

        // 注册Unity的日志回调
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
    }

    // 清除日志
    public void ClearLogs()
    {
        logMessages.Clear();
        UpdateDebugText();
    }
    
    // 添加分隔线
    public void AddSeparator()
    {
        AddLog("----------------------------------------", LogType.Log);
    }
    
    // 添加警告信息
    public void AddWarning(string message)
    {
        AddLog(message, LogType.Warning);
    }
    
    // 添加错误信息
    public void AddError(string message)
    {
        AddLog(message, LogType.Error);
    }
}
