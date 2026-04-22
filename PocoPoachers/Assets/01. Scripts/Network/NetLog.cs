using System;
using UnityEngine;

public static class NetLog
{
    static void Write(string prefix, string msg, Action<string> sink)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        var line = string.IsNullOrEmpty(msg)
            ? $"[{ts}] {prefix}"
            : $"[{ts}] {prefix} {msg}";
        sink(line);
    }

    public static void LOG(string msg = "") => Write("[Net]", msg, Debug.Log);
    public static void LOG_W(string msg = "") => Write("[Net]", msg, Debug.LogWarning);
    public static void LOG_E(string msg = "") => Write("[Net]", msg, Debug.LogError);
}
