using System.Reflection;
using BepInEx;
using HarmonyLib;
using ItemManager;
using UnityEngine;

namespace BackPacks
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class BackPacks : BaseUnityPlugin
    {
        internal const string ModName = "BackPacks";
        internal const string ModVersion = "0.0.1";
        private const string ModGUID = "com.zarboz.backpacks";
        private static Harmony harmony = null!;
        
#pragma warning disable CS0649
        internal static AssetBundle? eviesBackPacks;
        internal static AssetBundle? backpackDrops;
#pragma warning restore CS0649
        
        internal static Item? IronBag;
        internal static Item? SilverBag;


        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);

            SetupIronBag();
            SetupSilverBag();
        }

        private void SetupIronBag()
        {
            IronBag = new Item("backpacks", "CapeIronBackpack", "Assets");
            //Localization
            IronBag.Name.English("Rugged Backpack");
            IronBag.Description.English("A Rugged backpack, complete with buckles and fine leather straps.");
            //Crafting
            IronBag.Crafting.Add(CraftingTable.Workbench, 1);
            IronBag.RequiredItems.Add("Iron", 10);
            IronBag.RequiredItems.Add("LeatherScraps", 15);
            IronBag.RequiredItems.Add("LinenThread", 5);
            IronBag.CraftAmount = 1;
            //Upgrades
            IronBag.RequiredUpgradeItems.Add("Iron", 20);
            IronBag.RequiredUpgradeItems.Add("LinenThread", 5);
            
        }

        private void SetupSilverBag()
        {
            SilverBag = new Item("backpacks", "CapeSilverBackpack", "Assets");
            //Localization
            SilverBag.Name.English("Fine Backpack");
            SilverBag.Description.English("A Fine backpack, complete with hand made straps");
            //Crafting
            SilverBag.Crafting.Add(CraftingTable.Workbench, 1);
            SilverBag.RequiredItems.Add("Iron", 10);
            SilverBag.RequiredItems.Add("LeatherScraps", 15);
            SilverBag.RequiredItems.Add("LinenThread", 5);
            SilverBag.CraftAmount = 1;
            //Upgrades
            SilverBag.RequiredUpgradeItems.Add("Iron", 20);
            SilverBag.RequiredUpgradeItems.Add("LinenThread", 5);
        }
        
        
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}
