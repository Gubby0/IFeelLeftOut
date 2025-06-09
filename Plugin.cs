/*
 * Plugin code referenced from Toaster's public github https://github.com/ckhawks/ToasterCameras/blob/main/src/Plugin.cs
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using UnityEngine.Rendering;

namespace IFeelLeftOut
{
    public class Plugin : IPuckMod
    {
        
        public static string MOD_NAME = "IFeelLeftOut";
        public static string MOD_VERSION = "2.0.0";
        public static string MOD_GUID = "com.gubby.ifeelleftout";

        static readonly Harmony harmony = new Harmony(MOD_GUID);
        public bool OnEnable()
        {
            Plugin.Log($"Enabling...");
            try
            {
                if (IsDedicatedServer())
                {
                    Plugin.Log("Environment: dedicated server.");
                }
                else
                {
                    Plugin.Log("Environment: client.");
                    harmony.PatchAll();
                    LogAllPatchedMethods();
                }


                Plugin.Log($"Enabled!");
                return true;
            }
            catch (Exception e)
            {
                Plugin.LogError($"Failed to Enable: {e.Message}!");
                return false;
            }
        }

        public bool OnDisable()
        {
            try
            {
                Plugin.Log($"Disabling...");
                harmony.UnpatchSelf();

                Plugin.Log($"Disabled! Goodbye!");
                return true;
            }
            catch (Exception e)
            {
                Plugin.LogError($"Failed to disable: {e.Message}!");
                return false;
            }
        }

        public static bool IsDedicatedServer()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }

        public static void LogAllPatchedMethods()
        {
            var allPatchedMethods = harmony.GetPatchedMethods();
            var pluginId = harmony.Id;

            var mine = allPatchedMethods
                .Select(m => new { method = m, info = Harmony.GetPatchInfo(m) })
                .Where(x =>
                    x.info.Prefixes.Any(p => p.owner == pluginId) ||
                    x.info.Postfixes.Any(p => p.owner == pluginId) ||
                    x.info.Transpilers.Any(p => p.owner == pluginId) ||
                    x.info.Finalizers.Any(p => p.owner == pluginId)
                )
                .Select(x => x.method);

            foreach (var m in mine)
                Plugin.Log($" - {m.DeclaringType.FullName}.{m.Name}");
        }

        public static void Log(string message)
        {
            Debug.Log($"[{MOD_NAME}] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[{MOD_NAME}] {message}");
        }
    }
}
