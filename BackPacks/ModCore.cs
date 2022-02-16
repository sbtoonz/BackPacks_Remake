using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using ItemManager;
using ServerSync;
using UnityEngine;

namespace BackPacks
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("randyknapp.mods.extendeditemdataframework")]
    [BepInDependency("GoldenJude_JudesEquipment", BepInDependency.DependencyFlags.SoftDependency)]
    public class BackPacks : BaseUnityPlugin
    {
        internal const string ModName = "BackPacks_Remake";
        internal const string ModVersion = "0.0.7";
        private const string ModGUID = "com.zarboz.backpacks";
        private static Harmony harmony = null!;
        
#pragma warning disable CS0649
        internal static AssetBundle? eviesBackPacks;
        internal static AssetBundle? backpackDrops;
#pragma warning restore CS0649

        internal static Item? IronBag;
        internal static Item? SilverBag;
        internal static Item? LeatherBag;
        internal static bool useJudesBags;
        
        ServerSync.ConfigSync configSync = new ServerSync.ConfigSync(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        
        internal static ConfigEntry<bool>? AlterCarryWeight;
        internal static ConfigEntry<float>? CarryModifierLeather;
        internal static ConfigEntry<float>? CarryModifierIron;
        internal static ConfigEntry<float>? CarryModifierSilver;
        internal static ConfigEntry<float>? CarryModifierUnKnown;
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);

            SetupIronBag();
            SetupSilverBag();
            SetupLeatherBag();
        }

        private void Start()
        {
            try
            {
                var JudeEquip = Chainloader.PluginInfos.First(p => p.Key == "GoldenJude_JudesEquipment");
                if (JudeEquip.Value.Instance != null)
                {
                    useJudesBags = true;
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LoadConfigs()
        {
            AlterCarryWeight = config("General", "Alter Carry Weight", false,
                "Set this to true to edit the carry weight of the bags by a multiplier");
            CarryModifierLeather = config("Carry Modifiers", "Leather Modifier", 0.00f,
                "Set this value to impact the weight of your bag setting to .5 will make your bag weight half as much." +
                "Setting to 1.5 will make your bag weight 150% more than normal");
            CarryModifierIron = config("Carry Modifiers", "Iron Modifier", 0.00f,
                "Set this value to impact the weight of your bag setting to .5 will make your bag weight half as much." +
                "Setting to 1.5 will make your bag weight 150% more than normal");
            CarryModifierSilver = config("Carry Modifiers", "Silver Modifier", 0.00f,
                "Set this value to impact the weight of your bag setting to .5 will make your bag weight half as much." +
                "Setting to 1.5 will make your bag weight 150% more than normal");
            CarryModifierUnKnown = config("Carry Modifiers", "UnKnown Modifier", 0.00f,
                "Set this value to impact the weight of your bag setting to .5 will make your bag weight half as much." +
                "Setting to 1.5 will make your bag weight 150% more than normal, This is the value used on Jude's bags if you have his mod installed");


        }
        
        private void SetupIronBag()
        {
            IronBag = new Item("backpacks", "CapeIronBackpackZ", "Assets");
            //Localization
            IronBag.Name.English("Rugged Backpack");
            IronBag.Description.English("A Rugged backpack, complete with buckles and fine leather straps.");
            //Crafting
            IronBag.Crafting.Add(CraftingTable.Workbench, 1);
            IronBag.RequiredItems.Add("Bronze", 8);
            IronBag.RequiredItems.Add("LeatherScraps", 60);
            IronBag.RequiredItems.Add("DeerHide", 5);
            IronBag.CraftAmount = 1;
            //Upgrades
            IronBag.RequiredUpgradeItems.Add("Bronze", 20);
            IronBag.RequiredUpgradeItems.Add("LeatherScraps", 5);
            
        }

        private void SetupSilverBag()
        {
            SilverBag = new Item("backpacks", "CapeSilverBackpackZ", "Assets");
            //Localization
            SilverBag.Name.English("Fine Backpack");
            SilverBag.Description.English("A Fine backpack, complete with hand made straps");
            //Crafting
            SilverBag.Crafting.Add(CraftingTable.Workbench, 1);
            SilverBag.RequiredItems.Add("Silver", 23);
            SilverBag.RequiredItems.Add("LeatherScraps", 60);
            SilverBag.RequiredItems.Add("WolfPelt", 7);
            SilverBag.CraftAmount = 1;
            //Upgrades
            SilverBag.RequiredUpgradeItems.Add("Silver", 20);
            SilverBag.RequiredUpgradeItems.Add("WolfPelt", 5);
        }


        private void SetupLeatherBag()
        {
            LeatherBag = new Item("backpacks", "CapeLeatherBackpackZ", "Assets");
            //Localization
            LeatherBag.Name.English("Normal Backpack");
            LeatherBag.Description.English("An ordinary backpack, can store various items");
            //Crafting
            LeatherBag.Crafting.Add(CraftingTable.Workbench, 1);
            LeatherBag.RequiredItems.Add("LeatherScraps", 30);
            LeatherBag.RequiredItems.Add("DeerHide", 5);
            LeatherBag.CraftAmount = 1;
            //Upgrades
            LeatherBag.RequiredUpgradeItems.Add("LeatherScraps", 20);
            LeatherBag.RequiredUpgradeItems.Add("DeerHide", 5);
        }
        
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
        
        internal static AssetBundle? LoadAssetBundle(string bundleName)
        {
            var resource = typeof(BackPacks).Assembly.GetManifestResourceNames().Single
                (s => s.EndsWith(bundleName));
            using var stream = typeof(BackPacks).Assembly.GetManifestResourceStream(resource);
            return AssetBundle.LoadFromStream(stream);
        }
        
        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        
    }
}
