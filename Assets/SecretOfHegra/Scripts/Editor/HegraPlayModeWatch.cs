using UnityEditor;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Diagnostics: logs who leaves or pauses Play Mode (stack trace + focus state), so unexpected exits during
    /// automated play tests can be traced. Editor only; writes one log line per transition.
    /// </summary>
    [InitializeOnLoad]
    static class HegraPlayModeWatch
    {
        static HegraPlayModeWatch()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingPlayMode)
                    Debug.Log("[HegraWatch] Exiting play mode. focused=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive + "\n" + System.Environment.StackTrace);
            };
            EditorApplication.pauseStateChanged += s =>
                Debug.Log("[HegraWatch] Pause state " + s + ". focused=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive + "\n" + System.Environment.StackTrace);
        }
    }
}
