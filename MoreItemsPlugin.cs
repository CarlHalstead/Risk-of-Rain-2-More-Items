using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using R2API.Utils;
using TILER2;

using Path = System.IO.Path;

using static TILER2.MiscUtil;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace MoreItems
{
	[BepInPlugin(ModGuid, ModName, ModVersion)]
	[BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(TILER2Plugin.ModGuid, TILER2Plugin.ModVer)]

    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(ItemDropAPI), nameof(ResourcesAPI))]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class MoreItemsPlugin : BaseUnityPlugin
    {
        public const string ModVersion = "1.0.1";
        public const string ModName = "MoreItems";
        public const string ModGuid = "com.TaleOf4Gamers.MoreItems";

        internal static ConfigFile config = null;
        internal static ManualLogSource logger = null;
        internal static FilingDictionary<CatalogBoilerplate> masterItemList = new FilingDictionary<CatalogBoilerplate>();

        private void Awake()
        {
            logger = Logger;

            Logger.LogDebug($"Initialising plugin -- Version {ModVersion}");
            Logger.LogDebug("Loading configuration...");

            config = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{ModGuid}.cfg"), true);

            Logger.LogDebug("Loading assets...");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MoreItems.moreitems")) 
            {
                AssetBundle bundle = AssetBundle.LoadFromStream(stream);
                AssetBundleResourcesProvider provider = new AssetBundleResourcesProvider("@MoreItems", bundle);

                ResourcesAPI.AddProvider(provider);
            }

            masterItemList = T2Module.InitAll<CatalogBoilerplate>(new T2Module.ModInfo
            {
                displayName = "More Items",
                longIdentifier = "MoreItems",
                shortIdentifier = "More",
                mainConfigFile = config
            });

            T2Module.SetupAll_PluginAwake(masterItemList);
        }

        private void Start() 
        {
            T2Module.SetupAll_PluginStart(masterItemList);
            CatalogBoilerplate.ConsoleDump(logger, masterItemList);
        }
    }
}