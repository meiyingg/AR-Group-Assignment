using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class DebugLogger : MonoBehaviour
{
    public Text debugText;               // UI Text���������ʾ��־
    public int fontSize = 24;            // �����С
    public int maxMessages = 20;         // �����ʾ����Ϣ����
    public bool showTimestamp = true;    // �Ƿ���ʾʱ���
    public Color normalColor = Color.white;      // ��ͨ��־��ɫ
    public Color warningColor = Color.yellow;    // ������־��ɫ
    public Color errorColor = Color.red;         // ������־��ɫ

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

            // �����ı�����ĳ�ʼ����
            if (debugText != null)
            {
                debugText.fontSize = fontSize;
                debugText.color = normalColor;
                debugText.supportRichText = true;
                debugText.alignment = TextAnchor.UpperLeft;
            }
            
            // ��ӳ�ʼ����Ϣ
            AddLog("������־ϵͳ������");
            AddLog($"��Ļ�ֱ���: {Screen.width}x{Screen.height}");
            AddLog($"ϵͳ��Ϣ: {SystemInfo.operatingSystem}");
            AddLog($"�豸�ͺ�: {SystemInfo.deviceModel}");
        }
        else
        {
            Destroy(gameObject);
        }

        // ע��Unity����־�ص�
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

    // �����־
    public void ClearLogs()
    {
        logMessages.Clear();
        UpdateDebugText();
    }
    
    // ��ӷָ���
    public void AddSeparator()
    {
        AddLog("----------------------------------------", LogType.Log);
    }
    
    // ��Ӿ�����Ϣ
    public void AddWarning(string message)
    {
        AddLog(message, LogType.Warning);
    }
    
    // ��Ӵ�����Ϣ
    public void AddError(string message)
    {
        AddLog(message, LogType.Error);
    }
}
