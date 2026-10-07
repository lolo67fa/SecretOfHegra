using UnityEngine;
using UnityEngine.Rendering;

namespace SecretOfHegra
{
    /// <summary>
    /// Start-up safety for headset builds. Development builds enable URP's runtime Rendering Debugger, which opens
    /// when both thumbsticks are clicked (the game uses a stick click for sprint) and then captures controller input.
    /// Play tests should use the release APK (Tools ▸ Secret of Hegra ▸ Build Release APK); this also turns the
    /// debugger UI off in any player build, development or not. The Editor keeps it (Window ▸ Analysis).
    /// </summary>
    static class HegraBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void DisableRuntimeDebugUI()
        {
#if !UNITY_EDITOR
            DebugManager.instance.enableRuntimeUI = false;
#endif
        }
    }
}
