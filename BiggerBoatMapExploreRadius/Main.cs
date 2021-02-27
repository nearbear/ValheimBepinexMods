using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;

namespace BiggerBoatMapExploreRadius
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        #region[Declarations]

        public const string
            MODNAME = "BiggerBoatMapExploreRadius",
            AUTHOR = "nearbear",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0";

        internal readonly ManualLogSource log;
        internal readonly Harmony harmony;
        internal readonly Assembly assembly;
        public readonly string modFolder;

        #endregion

        #region[Configurations]

        public static ConfigEntry<float> boatExploreRadius;

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
            boatExploreRadius = Config.Bind("General", "BoatExploreRadius", 500f, "The radius in meters that your map reveal extends out to while driving a boat. (float)");
        }

        public void Start()
        {
            harmony.PatchAll(assembly);
        }
    }
}
