using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;

namespace BetterAutoRun
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        #region[Declarations]

        public const string
            MODNAME = "BetterAutoRun",
            AUTHOR = "nearbear",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0";

        public static ManualLogSource log;
        public static Harmony harmony;
        public static Assembly assembly;
        public readonly string modFolder;

        #endregion

        #region[Configurations]

        public static ConfigEntry<float> sprintUntil;

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
            sprintUntil = Config.Bind("General", "sprintUntil", .5f, "The 0 to 1 percentage of your stamina that you will stop auto sprinting at. Set to 1 to disable. (float)");
        }

        public void Start()
        {
            harmony.PatchAll(assembly);
        }
    }
}
