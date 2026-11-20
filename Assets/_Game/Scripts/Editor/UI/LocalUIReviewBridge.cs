using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

[InitializeOnLoad]
internal static class LocalUIReviewBridge
{
    static LocalUIReviewBridge()
    {
        EditorApplication.delayCall += StartConnection;
    }

    static void StartConnection()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("MCPForUnity.Editor.Services.MCPServiceLocator"))
            .FirstOrDefault(value => value != null);
        var bridge = type?.GetProperty("Bridge", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        bridge?.GetType().GetMethod("StartAsync", Type.EmptyTypes)?.Invoke(bridge, null);
    }
}
