#if UNITY_EDITOR
using ParrelSync;
using UnityEditor;
using UnityEngine;

namespace MultiSim
{
    internal static class MultiSimMenu
    {
        private const string EnabledMenu = "Tools/VRChat MultiSim/Enable Multiplayer Simulation";
        private const string VerboseMenu = "Tools/VRChat MultiSim/Verbose Logging";

        [MenuItem(EnabledMenu, false, 0)]
        private static void ToggleEnabled()
        {
            MultiSimPrefs.Enabled = !MultiSimPrefs.Enabled;
            MultiSimLog.Info($"Multiplayer simulation {(MultiSimPrefs.Enabled ? "enabled" : "disabled")}. " +
                             "This setting is shared with ParrelSync clones (EditorPrefs).");
        }

        [MenuItem(EnabledMenu, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(EnabledMenu, MultiSimPrefs.Enabled);
            return !Application.isPlaying;
        }

        [MenuItem(VerboseMenu, false, 1)]
        private static void ToggleVerbose()
        {
            MultiSimPrefs.VerboseLogging = !MultiSimPrefs.VerboseLogging;
        }

        [MenuItem(VerboseMenu, true)]
        private static bool ToggleVerboseValidate()
        {
            Menu.SetChecked(VerboseMenu, MultiSimPrefs.VerboseLogging);
            return true;
        }

        [MenuItem("Tools/VRChat MultiSim/Open ParrelSync Clones Manager", false, 20)]
        private static void OpenClonesManager()
        {
            EditorApplication.ExecuteMenuItem("ParrelSync/Clones Manager");
        }

        [MenuItem("Tools/VRChat MultiSim/Show Current Role", false, 21)]
        private static void ShowRole()
        {
            bool isClone = ClonesManager.IsClone();
            MultiSimLog.Info(isClone
                ? $"This editor is a ParrelSync CLONE, so it will join as a CLIENT (argument: '{ClonesManager.GetArgument()}')."
                : "This editor is the ORIGINAL project, so it will act as the HOST.");
        }
    }
}
#endif
