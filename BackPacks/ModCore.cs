using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
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
        internal const string ModVersion = "0.3.2";
        private const string ModGUID = "com.zarboz.backpacks";
        private static Harmony harmony = null!;
        internal static ManualLogSource _logSource = new ManualLogSource(ModName);
        
#pragma warning disable CS0649
        internal static AssetBundle? EviesBackPacks;
        internal static AssetBundle? backpackUI;
        internal static GameObject? backpackAdmin;
#pragma warning restore CS0649

        internal static Item? IronBag;
        internal static Item? SilverBag;
        internal static Item? LeatherBag;
        internal static bool UseJudesBags;
        
        ConfigSync configSync = new(ModGUID) 
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        
        internal static ConfigEntry<bool> ServerConfigLocked = null!;
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
        private static ConfigEntry<float>? _moveModifierLeather;
        private static ConfigEntry<float>? _moveModifierIron;
        private static ConfigEntry<float>? _moveModifierSilver;
        internal static ConfigEntry<float>? MoveModifierUnKnown;
        internal static ConfigEntry<Vector3>? SilverBagSize;
        internal static ConfigEntry<Vector3>? IronBagSize;
        internal static ConfigEntry<Vector3>? LeatherBagSize;
        internal static ConfigEntry<Vector3>? UnknownBagSize;
        internal static ConfigEntry<KeyCode>? OpenInventoryKey;
        internal static ConfigEntry<bool>? DropallOnUnEquip;
        internal static ConfigEntry<bool>? ShowToolTipText;
        private static bool _defsidefileflag;



        internal static SE_Stats? CarryStat;
        public void Awake()
        {
            _logSource = Logger;
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            LoadConfigs();
            CarryStat = ScriptableObject.CreateInstance<SeCarryWeight>();
            if (File.Exists(Paths.ConfigPath + Path.DirectorySeparatorChar + "defside"))
            {
                _defsidefileflag = true;
            }
            SetupIronBag();
            SetupSilverBag();
            SetupLeatherBag();
            ExtendedItemData.NewExtendedItemData += SaveEIDF;
            ExtendedItemData.LoadExtendedItemData += LoadEIDF;
            ExtendedItemData.RegisterCustomTypeID(BackPack.BackPackData.DataID, typeof(BackPack.BackPackData));
            backpackUI = LoadAssetBundle("backpackui");
            backpackAdmin = backpackUI!.LoadAsset<GameObject>("BackPack_Admin");
            backpackUI.Unload(false);
        }

        private void LoadEIDF(ExtendedItemData itemdata)
        {
            if(itemdata == null) return;
            if (itemdata.IsBackpack())
            {
                if (FejdStartup.instance != null) return;
                itemdata = itemdata.ExtendedClone();
                if(!itemdata.HasInventory()) return;
                var inv = itemdata.GetBagInv();
                if(inv == null) return;
                itemdata.m_shared.m_teleportable = inv!.IsTeleportable();
                itemdata.Extended().m_shared.m_teleportable = inv!.IsTeleportable();
                itemdata.m_shared.m_weight = 4f;
                itemdata.Extended().m_shared.m_weight = 4f;
                var temp = inv!.m_totalWeight;
                itemdata.m_shared.m_weight += temp;
                itemdata.Extended().m_shared.m_weight += temp;
                if (Player.m_localPlayer == null) return;
                Player.m_localPlayer.m_inventory.UpdateTotalWeight();
            }
        }
        private void SaveEIDF(ExtendedItemData itemdata)
        {
            var itemName = itemdata.m_shared.m_name;
            if (!itemName.IsNullOrWhiteSpace())
            {
                if (itemName.Contains("ackpack"))
                {
                    Inventory tempInv = new Inventory("", null, 100, 100);
                    var data = itemdata.AddComponent<BackPack.BackPackData>();
                    data.SetInventory(tempInv);
                    if(Player.m_localPlayer!=null) Player.m_localPlayer.m_inventory.Changed();
                }
            }
        }
        private void Start()
        {
            try
            {
                var judeEquip = Chainloader.PluginInfos.First(p => p.Key == "GoldenJude_JudesEquipment");
                if (judeEquip.Value.Instance != null)
                {
                    UseJudesBags = true;
                }
            }
            catch (Exception)
            {
                // ignored
            }

        }
        private void LoadConfigs()
        {
            ServerConfigLocked = config("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");

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
            _moveModifierIron = config("Move Modifiers", "Iron Tier Modifeir", 0.00f,
                "Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed");

            _moveModifierLeather = config("Move Modifiers", "Leather Tier Modifeir", 0.00f,
                "Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed");

            _moveModifierSilver = config("Move Modifiers", "Silver Tier Modifeir", 0.00f,
                "Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed");

            MoveModifierUnKnown = config("Move Modifiers", "UnKnown Tier Modifeir", 0.00f,
                new ConfigDescription("Set this to a negative number to slow movement when wearing. Set to positive to increase movement when wearing. IE -0.15 would be a negative 15% movment speed", null, new ConfigurationManagerAttributes{IsAdvanced = true}));

            #endregion

            #region BagSizes

            
            LeatherBagSize = config("Bag Sizes", "Leather Bag size", new Vector3(2, 1, 0),
                "This is the dimensions used for the inventory layout in leather bag");
            IronBagSize = config("Bag Sizes", "Iron Bag size", new Vector3(4, 2, 0),
                "This is the dimensions used for the inventory layout in leather bag");
            SilverBagSize = config("Bag Sizes", "Silver Bag size", new Vector3(6, 4, 0),
                "This is the dimensions used for the inventory layout in leather bag");
            UnknownBagSize = config("Bag Sizes", "Unknown Bag size", new Vector3(4, 4, 0),
                "This is the dimensions used for the inventory layout in leather bag");

            #endregion

            #region keyconfig

            OpenInventoryKey = config("General", "Key to open bag", KeyCode.E,
                "Modifier key to use when opening bag contents");

            #endregion

            DropallOnUnEquip = config("General", "Drop all bag contents on unequip", false,
                "If set to true the bag will drop all its items when coming off player shoulders");


            ShowToolTipText = config("General", "Show tooltip for equip key", false,
                "Set this to true to disable the inventory tooltip for opening bag");
            
            
            configSync.AddLockingConfigEntry(ServerConfigLocked);
        }
        private void SetupIronBag()
        {
            IronBag = new Item("backpacks", "CapeIronBackpackZ", "Assets");
            //Localization
            IronBag.Name.English("Rugged Backpack");
            IronBag.Description.English("A Rugged backpack, complete with buckles and fine leather straps, The more you level this bag up the more your storage will increase!"
                                        +$" \n \n Weight reduction: <color=orange> {String.Format("{0:P2}.", CarryModifierIron!.Value)} </color>"
                                        +$"\n Max carry increase: <color=orange> {CarryBonusIron!.Value}</color>");
            IronBag.Name.German("Rauer Rucksack");
            IronBag.Description.German("Ein rauer Rucksack, inklusive Schnallen und feinen Lederriemen. Je höher die Stufe des Rucksacks, desto mehr kann in ihm verstaut werden."
                                       +$" \n \n Gewichtsreduktion: <color=orange> {String.Format("{0:P2}.", CarryModifierIron!.Value)} </color>"
                                       +$"\n Erhöhung des max. Tragegewichts: <color=orange> {CarryBonusIron!.Value}</color>");
            IronBag.Name.Russian("Прочный рюкзак");
            IronBag.Description.Russian("Обычный рюкзак может хранить различные предметы. Чем больше вы повышаете уровень этого рюкзака, тем больше места!"+
                                        $" \n \n Снижение веса: <color=orange>{String.Format("{0:P2}.", CarryModifierIron!.Value)} </color>"
                                        +$"Максимальное увеличение переноса: <color=orange> {CarryBonusIron!.Value}</color>" );
            //Crafting
            IronBag.Crafting.Add(CraftingTable.Workbench, 1);
            IronBag.RequiredItems.Add("Iron", 8);
            IronBag.RequiredItems.Add("LeatherScraps", 60);
            IronBag.RequiredItems.Add("DeerHide", 5);
            IronBag.CraftAmount = 1;
            //Upgrades
            IronBag.RequiredUpgradeItems.Add("Iron", 20);
            IronBag.RequiredUpgradeItems.Add("LeatherScraps", 5);
            var id = IronBag.Prefab.gameObject.GetComponent<ItemDrop>();
            if (HaveMoveModifier!.Value) id.m_itemData.m_shared.m_movementModifier = _moveModifierIron!.Value;
            if (_defsidefileflag)
            {
                var frostResistance = new HitData.DamageModPair() { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant};
                List<HitData.DamageModPair> damageModPairs = new List<HitData.DamageModPair>
                {
                    frostResistance
                };
                id.m_itemData.m_shared.m_damageModifiers = damageModPairs;
            }
            
        }
        private void SetupSilverBag()
        {
            SilverBag = new Item("backpacks", "CapeSilverBackpackZ", "Assets");
            //Localization
            SilverBag.Name.English("Fine Backpack");
            SilverBag.Description.English("A Fine backpack, complete with hand made straps, The more you level this bag up the more your storage will increase!"
                                          +$" \n \n Weight reduction: <color=orange> {String.Format("{0:P2}.", CarryModifierSilver!.Value)} </color>"
                                          +$"\n Max carry increase: <color=orange> {CarryBonusSilver!.Value}</color>");
            SilverBag.Name.German("Guter Rucksack");
            SilverBag.Description.German("Ein guter Rucksack, mit in Handarbeit hergestellten Riemen. Je höher die Stufe des Rucksacks, desto mehr kann in ihm verstaut werden."
                                          +$" \n \n Gewichtsreduktion: <color=orange> {String.Format("{0:P2}.", CarryModifierSilver!.Value)} </color>"
                                          +$"\n Erhöhung des max. Tragegewichts: <color=orange> {CarryBonusSilver!.Value}</color>");
            SilverBag.Name.Russian("Хороший рюкзак");
            SilverBag.Description.Russian("Хороший рюкзак с ремешками ручной работы. Чем выше уровень этой сумки, тем больше места!"+
                                          $"\n \n Снижение веса: <color=orange> {CarryBonusSilver!.Value}</color>"+
                                          "Максимальное увеличение переноса: <color=orange> {CarryBonusSilver!.Value}</color>");
            //Crafting
            SilverBag.Crafting.Add(CraftingTable.Workbench, 1);
            SilverBag.RequiredItems.Add("Silver", 20);
            SilverBag.RequiredItems.Add("LeatherScraps", 60);
            SilverBag.RequiredItems.Add("WolfPelt", 7);
            SilverBag.CraftAmount = 1;
            //Upgrades
            SilverBag.RequiredUpgradeItems.Add("Silver", 50);
            SilverBag.RequiredUpgradeItems.Add("WolfPelt", 5);
            var id = SilverBag.Prefab.gameObject.GetComponent<ItemDrop>();
            if (HaveMoveModifier!.Value) id.m_itemData.m_shared.m_movementModifier = _moveModifierSilver!.Value;
            if (_defsidefileflag)
            {
                var frostResistance = new HitData.DamageModPair() { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant};
                List<HitData.DamageModPair> damageModPairs = new List<HitData.DamageModPair>
                {
                    frostResistance
                };
                id.m_itemData.m_shared.m_damageModifiers = damageModPairs;
            }
        }
        private void SetupLeatherBag()
        {
            LeatherBag = new Item("backpacks", "CapeLeatherBackpackZ", "Assets");
            //Localization
            LeatherBag.Name.English("Normal Backpack");
            LeatherBag.Description.English("An ordinary backpack, can store various items, The more you level this bag up the more your storage will increase!"
                                           +$" \n \n Weight reduction: <color=orange> {String.Format("{0:P2}.", CarryModifierLeather!.Value)} </color>"
                                           +$"\n Max carry increase: <color=orange> {CarryBonusLeather!.Value}</color>");
            LeatherBag.Name.German("Gewöhnlicher Rucksack");
            LeatherBag.Description.German("Ein gewöhnlicher Rucksack, in dem verschiedene Gegenstände verstaut werden können. Je höher die Stufe des Rucksacks, desto mehr kann in ihm verstaut werden."
                                           +$" \n \n Gewichtsreduktion: <color=orange> {String.Format("{0:P2}.", CarryModifierLeather!.Value)} </color>"
                                           +$"\n Erhöhung des max. Tragegewichts: <color=orange> {CarryBonusLeather!.Value}</color>");
            LeatherBag.Name.Russian("Обычный рюкзак");
            LeatherBag.Description.Russian(
                "Обычный рюкзак может хранить различные предметы. Чем больше вы повышаете уровень этого рюкзака, тем больше места!" +
                $" \n \n Снижение веса:<color=orange> {String.Format("{0:P2}.", CarryModifierLeather!.Value)} </color>"
                +$"\n Максимальное увеличение переноса: <color=orange> {CarryBonusLeather!.Value}</color>");
            //Crafting
            LeatherBag.Crafting.Add(CraftingTable.Workbench, 1);
            LeatherBag.RequiredItems.Add("LeatherScraps", 30);
            LeatherBag.RequiredItems.Add("DeerHide", 5);
            LeatherBag.CraftAmount = 1;
            //Upgrades
            LeatherBag.RequiredUpgradeItems.Add("LeatherScraps", 20);
            LeatherBag.RequiredUpgradeItems.Add("DeerHide", 5);
            var id = LeatherBag.Prefab.gameObject.GetComponent<ItemDrop>();
            if (HaveMoveModifier!.Value) id.m_itemData.m_shared.m_movementModifier = _moveModifierLeather!.Value;
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
