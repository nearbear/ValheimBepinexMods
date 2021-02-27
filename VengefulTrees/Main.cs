using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;

namespace VengefulTrees
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        #region[Declarations]

        public const string
            MODNAME = "VengefulTrees",
            AUTHOR = "nearbear",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0";

        public static ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;

        #endregion

        #region[Configurations]
        
        public static ConfigEntry<bool> debugMode;

        #endregion

        public Main()
        {
            log = Logger;
            harmony = new Harmony(GUID);
            assembly = Assembly.GetExecutingAssembly();
            modFolder = Path.GetDirectoryName(assembly.Location);
        }

        private void Awake()
        {
            debugMode = Config.Bind("General", "debugMode", false, "Always trigger vengeful trees and show where force is being applied. (bool)");
        }

        public void Start()
        {
            harmony.PatchAll(assembly);
        }
    }
}
