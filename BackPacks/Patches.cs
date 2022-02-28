using System;
using System.Collections.Generic;
using System.Linq;
using ExtendedItemDataFramework;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace BackPacks
{
    public static class Patches
    {

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        public static class InventoryGui_DoCrafting_Patch
        {
            [UsedImplicitly]
            public static bool Prefix(InventoryGui __instance, Player player, bool __runOriginal)
            {
                if (!__runOriginal || __instance.m_craftRecipe == null)
                {
                    return false;
                }

                var newQuality = __instance.m_craftUpgradeItem?.m_quality + 1 ?? 1;
                if (newQuality > __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality
                    || !player.HaveRequirements(__instance.m_craftRecipe, false, newQuality) && !player.NoCostCheat()
                    || (__instance.m_craftUpgradeItem != null
                        && !player.GetInventory().ContainsItem(__instance.m_craftUpgradeItem)
                        || __instance.m_craftUpgradeItem == null
                        && !player.GetInventory().HaveEmptySlot()))
                {
                    return false;
                }

                if (__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 &&
                    !DLCMan.instance.IsDLCInstalled(__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
                {
                    return false;
                }

                var upgradeItem = __instance.m_craftUpgradeItem;
                if (upgradeItem != null)
                {
                    upgradeItem.m_quality = newQuality;
                    upgradeItem.m_durability = upgradeItem.GetMaxDurability();
                    if (upgradeItem.IsBackpack())
                    {
                        var bag = Player.m_localPlayer.gameObject.GetComponentInChildren<BackPack>();
                        if (bag != null)
                        {
                            bag.CloseBag();
                            bag.StopCoroutines();
                            bag.ApplyConfigToInventory();
                            bag.AssignContainerSize(upgradeItem.Extended().m_quality);
                            bag.StartCoroutines();
                        }
                    }
                    if (!player.NoCostCheat())
                    {
                        player.ConsumeResources(__instance.m_craftRecipe.m_resources, newQuality);
                    }

                    __instance.UpdateCraftingPanel();

                    var currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                    if (currentCraftingStation != null)
                    {
                        currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position,
                            Quaternion.identity);
                    }
                    else
                    {
                        __instance.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                    }

                    ++Game.instance.GetPlayerProfile().m_playerStats.m_crafts;
                    Gogan.LogEvent("Game", "Crafted", __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_name,
                        (long)newQuality);

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        [HarmonyPriority(Priority.High)]
        public static class EquipItemPatch
        {

            [UsedImplicitly]
            private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
            {
                try
                {
                    if (Player.m_localPlayer == null) return;
                    if (__instance.m_nview.GetZDO().m_uid != Player.m_localPlayer.m_nview.GetZDO().m_uid) return;
                    if (!item.IsBackpack()) return;
                    switch (item.m_shared.m_itemType)
                    {
                        case ItemDrop.ItemData.ItemType.Shoulder: 
                            if (__instance.IsPlayer() && __instance.IsDead())
                            {
                                return;
                            }

                            if (__instance.IsTeleporting())
                            {
                                return;
                            }
                            if (Player.m_localPlayer.m_shoulderItem == null) return;
                            if (Player.m_localPlayer.m_shoulderItem.Extended().GetUniqueId() == item.Extended().GetUniqueId()) return;
                            if (Player.m_localPlayer.m_shoulderItem.Extended().GetBagInv() != item.GetBagInv())
                            {
                                var bag = Player.m_localPlayer.gameObject.GetComponentInChildren<BackPack>();
                                bag.CloseBag();
                                bag.DeRegisterRPC();
                                bag.StopCoroutines();
                                bag.ApplyConfigToInventory();
                                bag.AssignInventory(item.GetBagInv()!);
                                bag.LoadBagContents();
                                bag.AssignContainerSize(item.Extended().m_quality);
                                bag.StartCoroutines();
                                bag.RegisterRPC();
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    //ignored
                }
                
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyAfter("GoldenJude_JudesEquipment")]
        public static class JudeBagPatch
        {
            [UsedImplicitly]
            public static void Postfix(ObjectDB __instance)
            {
                if (__instance.m_StatusEffects.Count <= 0) return;
                __instance.m_StatusEffects.Add(BackPacks.CarryStat);
                var SE = __instance.GetStatusEffect(BackPacks.CarryStat?.name);
                if (BackPacks.UseJudesBags)
                {
                    if (__instance.m_items.Count <= 0 || !__instance.GetItemPrefab("Wood")) return;
                    var HeavyBag = __instance.GetItemPrefab("BackpackHeavy");
                    var simpleBag = __instance.GetItemPrefab("BackpackSimple");

                    if (HeavyBag)
                    {
                        var heavybackpack = HeavyBag.transform.Find("attach_skin/heavyBackpack").gameObject
                            .AddComponent<BackPack>();
                        heavybackpack.fixedWidth = 5;
                        heavybackpack.fixedHeight = 3;
                        heavybackpack.ApplyConfigToInventory();
                        heavybackpack.m_name = "Backpack";
                        heavybackpack.tier = BackPack.BagTier.Silver;
                        
                        var ID = HeavyBag.gameObject.GetComponent<ItemDrop>();
                        if (BackPacks.AddCarryBonus!.Value)
                        {
                            ID.m_itemData.m_shared.m_equipStatusEffect = SE;
                        }
                        if (BackPacks.HaveMoveModifier!.Value) ID.m_itemData.m_shared.m_movementModifier = BackPacks.MoveModifierUnKnown!.Value;
                    }

                    if (simpleBag)
                    {
                        var simplebackpack = simpleBag.transform.Find("attach_skin/simpleBackpack").gameObject
                            .AddComponent<BackPack>();


                        simplebackpack.fixedWidth = 3;
                        simplebackpack.fixedHeight = 2;
                        simplebackpack.ApplyConfigToInventory();
                        simplebackpack.m_name = "Backpack";
                        simplebackpack.tier = BackPack.BagTier.Iron;
                        var ID = simpleBag.gameObject.GetComponent<ItemDrop>();
                        if (BackPacks.AddCarryBonus!.Value)
                        {
                            ID.m_itemData.m_shared.m_equipStatusEffect = SE;
                        }
                        if (BackPacks.HaveMoveModifier!.Value) ID.m_itemData.m_shared.m_movementModifier = BackPacks.MoveModifierUnKnown!.Value;
                    }
                    
                }

                if (!BackPacks.AddCarryBonus!.Value) return;
                var IronBag = __instance.GetItemPrefab("CapeIronBackpackZ");
                var LeatherBag = __instance.GetItemPrefab("CapeSilverBackpackZ");
                var SilverBag = __instance.GetItemPrefab("CapeLeatherBackpackZ");
                IronBag.gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = SE;
                LeatherBag.gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = SE;
                SilverBag.gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = SE;
            }
        }
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        [HarmonyAfter("GoldenJude_JudesEquipment")]
        public static class JudeBagOtherPatch
        {
            [UsedImplicitly]
            public static void Postfix(ObjectDB __instance)
            {
                if (__instance.m_StatusEffects.Count <= 0) return;
                __instance.m_StatusEffects.Add(BackPacks.CarryStat);
                var SE = __instance.GetStatusEffect(BackPacks.CarryStat?.name);
                if (BackPacks.UseJudesBags)
                { 
                    if (__instance.m_items.Count <= 0 || !__instance.GetItemPrefab("Wood")) return;
                    var HeavyBag = __instance.GetItemPrefab("BackpackHeavy");
                    var simpleBag = __instance.GetItemPrefab("BackpackSimple");

                    if (HeavyBag)
                    {
                        var heavybackpack =HeavyBag.transform.Find("attach_skin/heavyBackpack").gameObject.AddComponent<BackPack>();
                        heavybackpack.fixedWidth = 5;
                        heavybackpack.fixedHeight = 3;
                        heavybackpack.ApplyConfigToInventory();
                        heavybackpack.m_width = heavybackpack.fixedWidth;
                        heavybackpack.m_height = heavybackpack.fixedHeight;
                        heavybackpack.tier = BackPack.BagTier.Silver;
                        var ID = HeavyBag.gameObject.GetComponent<ItemDrop>();
                        if (BackPacks.AddCarryBonus!.Value)
                        {
                            ID.m_itemData.m_shared.m_equipStatusEffect = SE;
                        }
                        if (BackPacks.HaveMoveModifier!.Value) ID.m_itemData.m_shared.m_movementModifier = BackPacks.MoveModifierUnKnown!.Value;

                    }

                    if (simpleBag)
                    {
                        var simplebackpack = simpleBag.transform.Find("attach_skin/simpleBackpack").gameObject
                            .AddComponent<BackPack>();


                        simplebackpack.fixedWidth = 3;
                        simplebackpack.fixedHeight = 2;
                        simplebackpack.tier = BackPack.BagTier.Iron;
                        simplebackpack.ApplyConfigToInventory();
                        var ID = simpleBag.gameObject.GetComponent<ItemDrop>();
                        if (BackPacks.AddCarryBonus!.Value)
                        {
                            ID.m_itemData.m_shared.m_equipStatusEffect = SE;
                        }
                        if (BackPacks.HaveMoveModifier!.Value) ID.m_itemData.m_shared.m_movementModifier = BackPacks.MoveModifierUnKnown!.Value;
                    }

                }
                
                
                
                if (!BackPacks.AddCarryBonus!.Value) return;
                var IronBag = __instance.GetItemPrefab("CapeIronBackpackZ");
                var LeatherBag = __instance.GetItemPrefab("CapeSilverBackpackZ");
                var SilverBag = __instance.GetItemPrefab("CapeLeatherBackpackZ");
                IronBag.gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = SE;
                LeatherBag.gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = SE;
                SilverBag.gameObject.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = SE;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class AssignAuga
        {
            [UsedImplicitly]
            public static void Postfix(InventoryGui __instance)
            {
                if (Auga.API.IsLoaded())
                {
                    if (BackPack.AugaBackPackTip != null) return;
                    BackPack.AuguaTrashThing = InventoryGui.instance.gameObject.transform
                        .Find("root/Player/TrashDivider").gameObject;
                    BackPack.AugaBackPackTip = InstantiatePrefab.Instantiate(BackPack.AuguaTrashThing,
                        InventoryGui.instance.gameObject.transform.Find("root/Player/").transform);
                    BackPack.AugaBackPackTip.gameObject.name = "BackPackToolTip";
                    var localPosition = BackPack.AugaBackPackTip.transform.localPosition;
                    var ypos = localPosition.y;
                    ypos -= 25;
                    localPosition =
                        new Vector3(localPosition.x, ypos, localPosition.z);
                    BackPack.AugaBackPackTip.transform.localPosition = localPosition;
                    BackPack.AuguaTrashThing = InventoryGui.instance.gameObject.transform
                        .Find("root/Player/BackPackToolTip/Content").gameObject;
                    var icon = BackPack.AuguaTrashThing.transform.Find("Icon").gameObject;
                    icon.gameObject.SetActive(false);
                }
                
                BackPacks.backpackAdmin = Object.Instantiate(BackPacks.backpackAdmin, InventoryGui.instance.gameObject.transform.Find("root/Player/").transform);
                BackPacks.backpackAdmin!.SetActive(false);
                var panel = BackPacks.backpackAdmin.GetComponent<BagAdminPanel>();
                panel.m_Bkg.sprite = __instance.gameObject.transform.Find("root/Player/Bkg").gameObject.GetComponent<Image>().sprite;
                panel.m_Bkg.material = __instance.gameObject.transform.Find("root/Player/Bkg").gameObject
                    .GetComponent<Image>().material;
                    
                
            }
        }

        

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.Reset))]
        public static class ZinputPatch
        {
            public static void Postfix(ZInput __instance)
            {
                __instance.AddButton("Backpack",
                    BackPacks.OpenInventoryKey!.Value, 0, 0, true, false);
            }
        }
        

        internal static void EjectBackpack(ItemDrop.ItemData item, Player player, Inventory backpackInventory)
        {
	        var playerInventory = player.GetInventory();
	        if (playerInventory.HaveEmptySlot())
	        {
		        playerInventory.MoveItemToThis(backpackInventory, item);
	        }
	        else
	        {
		        backpackInventory.RemoveItem(item);
		        ItemDrop.DropItem(item, 1, player.transform.position 
		                                   + player.transform.forward 
		                                   + player.transform.up, player.transform.rotation);
	        }
        }
    }
}