using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;


namespace IFeelLeftOut;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private readonly Harmony _harmony = new Harmony("plugins.ifeelleftout");

    // plugin managers
    public static new ManualLogSource Log;
    public static PlayerManager playerManager;
    public static bool leftOutCamSelected = false;

    public override void Load()
    {
        HarmonyFileLog.Enabled = true;

        // Plugin startup logic
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Patching methods...");
        _harmony.PatchAll();
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is all patched! Patched methods:");

        var originalMethods = Harmony.GetAllPatchedMethods();
        foreach (var method in originalMethods)
        {
            Log.LogInfo($" - {method.DeclaringType.FullName}.{method.Name}");
        }
    }
}
