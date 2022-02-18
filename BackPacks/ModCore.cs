using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using ExtendedItemDataFramework;
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
        internal const string ModVersion = "0.1.2";
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
        
        ConfigSync configSync = new(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        
        private static ConfigEntry<bool> serverConfigLocked = null!;
        internal static ConfigEntry<bool>? AlterCarryWeight;
        internal static ConfigEntry<float>? CarryModifierLeather;
        internal static ConfigEntry<float>? CarryModifierIron;
        internal static ConfigEntry<float>? CarryModifierSilver;
        internal static ConfigEntry<float>? CarryModifierUnKnown;
        internal static ConfigEntry<bool>? AddCarryBonus;
        internal static ConfigEntry<float>? CarryBonusLeather;
        internal static ConfigEntry<float>? CarryBonusIron;
        internal static ConfigEntry<float>? CarryBonusSilver;
        internal static ConfigEntry<float>? CarryBonusUnKnown;
        internal static ConfigEntry<bool>? HaveMoveModifier;
        internal static ConfigEntry<float>? MoveModifierLeather;
        internal static ConfigEntry<float>? MoveModifierIron;
        internal static ConfigEntry<float>? MoveModifierSilver;
        internal static ConfigEntry<float>? MoveModifierUnKnown;
        internal static SE_Stats? CarryStat;
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            LoadConfigs();
            CarryStat = ScriptableObject.CreateInstance<SeCarryWeight>();
            SetupIronBag();
            SetupSilverBag();
            SetupLeatherBag();
            ExtendedItemData.NewExtendedItemData += testfunc;
            ExtendedItemData.RegisterCustomTypeID(BackPack.BackPackData.dataID, typeof(BackPack.BackPackData));
            
        }

        private void testfunc(ExtendedItemData itemdata)
        {
            var itemName = itemdata.m_shared.m_name;
            if (!itemName.IsNullOrWhiteSpace())
            {
                if (itemName.Contains("ackpack"))
                {
                    Inventory tempInv = new Inventory("", null, 100, 100);
                    var data = itemdata.AddComponent<BackPack.BackPackData>();
                    data.SetInventory(tempInv);
                    data.Tier = BackPack.BagTier.UnKnown;
                }
            }
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
            serverConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");

            #region  Carry Weight

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



                #endregion

            #region CarryBonus

                
            AddCarryBonus = config("General", "Add Carry Bonus", false,
                    "Set this to true if you wish to get a carry capacity increase while wearing bags");

            CarryBonusLeather = config("Carry Bonus", "Leather", 0.00f,
                "The Volume of carry weight bonus while wearing this bag");
            CarryBonusIron = config("Carry Bonus", "Iron", 0.00f,
                "The Volume of carry weight bonus while wearing this bag");
            CarryBonusSilver = config("Carry Bonus", "Silver", 0.00f,
                "The Volume of carry weight bonus while wearing this bag");
            CarryBonusUnKnown = config("Carry Bonus", "UnKnown", 0.00f,
                "The Volume of carry weight bonus while wearing this bag");

            #endregion

            #region MovementModifier

            HaveMoveModifier = config("General", "Should bags impact movement", false,
                "Set this to true if you wish for bags to impact your movement");
            MoveModifierIron = config("Move Modifiers", "Iron Tier Modifeir", 0.00f,
                "Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed");

            MoveModifierLeather = config("Move Modifiers", "Leather Tier Modifeir", 0.00f,
                "Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed");

            MoveModifierSilver = config("Move Modifiers", "Silver Tier Modifeir", 0.00f,
                "Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed");

            MoveModifierUnKnown = config("Move Modifiers", "UnKnown Tier Modifeir", 0.00f,
                "Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed");

            #endregion
            
            
            configSync.AddLockingConfigEntry(serverConfigLocked);
        }
        
        private void SetupIronBag()
        {
            IronBag = new Item("backpacks", "CapeIronBackpackZ", "Assets");
            //Localization
            IronBag.Name.English("Rugged Backpack");
            IronBag.Description.English("A Rugged backpack, complete with buckles and fine leather straps, The more you level this bag up the more your storage will increase!"
                                        +$" \n \n Weight reduction: <color=orange> {String.Format("{0:P2}.", CarryModifierIron!.Value)} </color>"
                                        +$"\n Max carry increase: <color=orange> {CarryBonusIron!.Value}</color>");
            //Crafting
            IronBag.Crafting.Add(CraftingTable.Workbench, 1);
            IronBag.RequiredItems.Add("Bronze", 8);
            IronBag.RequiredItems.Add("LeatherScraps", 60);
            IronBag.RequiredItems.Add("DeerHide", 5);
            IronBag.CraftAmount = 1;
            //Upgrades
            IronBag.RequiredUpgradeItems.Add("Bronze", 20);
            IronBag.RequiredUpgradeItems.Add("LeatherScraps", 5);
            var ID = IronBag.Prefab.gameObject.GetComponent<ItemDrop>();
            if (HaveMoveModifier!.Value) ID.m_itemData.m_shared.m_movementModifier = MoveModifierIron!.Value;

            
        }

        private void SetupSilverBag()
        {
            SilverBag = new Item("backpacks", "CapeSilverBackpackZ", "Assets");
            //Localization
            SilverBag.Name.English("Fine Backpack");
            SilverBag.Description.English("A Fine backpack, complete with hand made straps, The more you level this bag up the more your storage will increase!"
                                          +$" \n \n Weight reduction: <color=orange> {String.Format("{0:P2}.", CarryModifierSilver!.Value)} </color>"
                                          +$"\n Max carry increase: <color=orange> {CarryBonusSilver!.Value}</color>");
            //Crafting
            SilverBag.Crafting.Add(CraftingTable.Workbench, 1);
            SilverBag.RequiredItems.Add("Silver", 23);
            SilverBag.RequiredItems.Add("LeatherScraps", 60);
            SilverBag.RequiredItems.Add("WolfPelt", 7);
            SilverBag.CraftAmount = 1;
            //Upgrades
            SilverBag.RequiredUpgradeItems.Add("Silver", 20);
            SilverBag.RequiredUpgradeItems.Add("WolfPelt", 5);
            var ID = SilverBag.Prefab.gameObject.GetComponent<ItemDrop>();
            if (HaveMoveModifier!.Value) ID.m_itemData.m_shared.m_movementModifier = MoveModifierSilver!.Value;
        }


        private void SetupLeatherBag()
        {
            LeatherBag = new Item("backpacks", "CapeLeatherBackpackZ", "Assets");
            //Localization
            LeatherBag.Name.English("Normal Backpack");
            LeatherBag.Description.English("An ordinary backpack, can store various items, The more you level this bag up the more your storage will increase!"
                                           +$" \n \n Weight reduction: <color=orange> {String.Format("{0:P2}.", CarryModifierLeather!.Value)} </color>"
                                           +$"\n Max carry increase: <color=orange> {CarryBonusLeather!.Value}</color>");
            //Crafting
            LeatherBag.Crafting.Add(CraftingTable.Workbench, 1);
            LeatherBag.RequiredItems.Add("LeatherScraps", 30);
            LeatherBag.RequiredItems.Add("DeerHide", 5);
            LeatherBag.CraftAmount = 1;
            //Upgrades
            LeatherBag.RequiredUpgradeItems.Add("LeatherScraps", 20);
            LeatherBag.RequiredUpgradeItems.Add("DeerHide", 5);
            var ID = LeatherBag.Prefab.gameObject.GetComponent<ItemDrop>();
            if (HaveMoveModifier!.Value) ID.m_itemData.m_shared.m_movementModifier = MoveModifierLeather!.Value;
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
