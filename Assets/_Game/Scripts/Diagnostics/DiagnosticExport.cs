using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IanGame
{
    internal static class DiagnosticExport
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        [return: MarshalAs(UnmanagedType.I1)]
        static extern bool IanPresentReport(string path);
#endif
        internal static void OpenFolder(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException();
            }
            Application.OpenURL(new Uri(Path.GetFullPath(directory)).AbsoluteUri);
        }

        internal static void Confirm(string id, string preview, bool share)
        {
            var store = LocalDiagnostics.Current.Store;
            if (!Directory.Exists(Path.Combine(store.Errors, id)))
            {
                throw new FileNotFoundException();
            }
            string name = id + ".txt";
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var host = unity.GetStatic<AndroidJavaObject>("currentActivity");
            using var bridge = new AndroidJavaClass("com.iangrowing.diagnostics.ExportReport");
            bridge.CallStatic("open", host, name, preview, share);
#else
            string directory = LocalDiagnostics.Current.Store.Exports;
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, name);
            File.WriteAllText(file, preview);
#if UNITY_IOS && !UNITY_EDITOR
            if (!IanPresentReport(file))
            {
                throw new InvalidOperationException();
            }
#else
            OpenFolder(directory);
#endif
#endif
        }
    }
}
