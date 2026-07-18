#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Free-tier Unity MCP allows 1 client. Claude Code often steals Cursor's slot.
/// Auto-reclaims on domain load (retried) + manual menu.
/// </summary>
[InitializeOnLoad]
public static class SpaceFactoryMcpSlot
{
    static int _attempts;

    static SpaceFactoryMcpSlot()
    {
        _attempts = 0;
        EditorApplication.delayCall += Tick;
    }

    [MenuItem("Tools/Space Factory/MCP — Give Slot To Cursor")]
    static void MenuReclaim()
    {
        int n = Reclaim();
        EditorUtility.DisplayDialog("MCP Slot",
            $"Updated {n} connection record(s).\n\nIf Cursor still says revoked, click Allow once in Project Settings → AI → Unity MCP.",
            "OK");
    }

    static void Tick()
    {
        _attempts++;
        int n = Reclaim();
        // Retry a few times — ConnectionStore hydrates after some packages.
        if (_attempts < 8 && n < 0)
            EditorApplication.delayCall += Tick;
        else if (_attempts < 5)
            EditorApplication.delayCall += Tick;
    }

    /// <returns>Records touched, or -1 if store unavailable.</returns>
    static int Reclaim()
    {
        var storeType = FindType("Unity.AI.MCP.Editor.ConnectionStore");
        if (storeType == null) return -1;

        var dictField = storeType.GetField("ConnectionsByIdentity",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var dict = dictField?.GetValue(null);
        if (dict == null) return -1;

        var values = dict.GetType().GetProperty("Values")?.GetValue(dict) as System.Collections.IEnumerable;
        if (values == null) return -1;

        int touched = 0;
        int rejectedClaude = 0;
        int acceptedCursor = 0;

        foreach (var record in values)
        {
            if (record == null) continue;
            var t = record.GetType();
            var statusProp = t.GetProperty("Status");
            var reasonProp = t.GetProperty("ValidationReason");
            var info = t.GetProperty("Info")?.GetValue(record);
            if (statusProp == null || info == null) continue;

            string clientName = "";
            string processName = "";
            var clientInfo = info.GetType().GetProperty("ClientInfo")?.GetValue(info);
            if (clientInfo != null)
                clientName = clientInfo.GetType().GetProperty("Name")?.GetValue(clientInfo) as string ?? "";
            var client = info.GetType().GetProperty("Client")?.GetValue(info);
            if (client != null)
                processName = client.GetType().GetProperty("ProcessName")?.GetValue(client) as string ?? "";

            string who = (clientName + " " + processName).ToLowerInvariant();
            bool isCursor = who.Contains("cursor");
            bool isClaude = who.Contains("claude");
            if (!isCursor && !isClaude) continue;

            object accepted = Enum.Parse(statusProp.PropertyType, "Accepted");
            object rejected = Enum.Parse(statusProp.PropertyType, "Rejected");
            object current = statusProp.GetValue(record);

            if (isClaude)
            {
                if (!Equals(current, rejected))
                {
                    statusProp.SetValue(record, rejected);
                    reasonProp?.SetValue(record, "Revoked: free slot reserved for Cursor");
                    touched++;
                }
                rejectedClaude++;
            }
            else if (isCursor)
            {
                if (!Equals(current, accepted))
                {
                    statusProp.SetValue(record, accepted);
                    reasonProp?.SetValue(record, "Approved by user from settings");
                    touched++;
                }
                acceptedCursor++;
            }
        }

        if (touched > 0)
        {
            var saveField = storeType.GetField("OnSaveRequested",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            (saveField?.GetValue(null) as Action)?.Invoke();
            Debug.Log($"[SpaceFactory MCP] Reclaimed slot. Cursor={acceptedCursor} ClaudeRejected={rejectedClaude} Touched={touched}");
        }

        return touched;
    }

    static Type FindType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            catch { /* dynamic assemblies */ }
        }
        return null;
    }
}
#endif
