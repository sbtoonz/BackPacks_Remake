using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BackPacks
{
    public class Patches
    {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
        [HarmonyPriority(Priority.Last)]
        private static class GetWeightPatch
        {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (__instance.Extended()?.GetComponent<BackPack.BackPackData>() is { } backPackData)
                {
                    if (BackPacks.AlterCarryWeight!.Value)
                    {
                        switch (backPackData.Tier)
                        {
                            case BackPack.BagTier.Iron:
                                __result += backPackData.inventory!.GetAllItems().Sum(backPackItem =>
                                    backPackItem.GetWeight() * BackPacks.CarryModifierIron!.Value);
                                break;
                            case BackPack.BagTier.Leather:
                                __result += backPackData.inventory!.GetAllItems().Sum(backPackItem =>
                                    backPackItem.GetWeight() * BackPacks.CarryModifierLeather!.Value);
                                break;
                            case BackPack.BagTier.Silver:
                                __result += backPackData.inventory!.GetAllItems().Sum(backPackItem =>
                                    backPackItem.GetWeight() * BackPacks.CarryModifierSilver!.Value);
                                break;
                            case BackPack.BagTier.BlackMetal:
                                __result += backPackData.inventory!.GetAllItems().Sum(backPackItem =>
                                    backPackItem.GetWeight());
                                break;
                            case BackPack.BagTier.UnKnown:
                                __result += backPackData.inventory!.GetAllItems().Sum(backPackItem =>
                                    backPackItem.GetWeight() * BackPacks.CarryModifierUnKnown!.Value);
                                break;
                            default:
                                __result += backPackData.inventory!.GetAllItems().Sum(backPackItem =>
                                    backPackItem.GetWeight());
                                break;
                        }
                    }
                    else
                    {
                        __result += backPackData.inventory!.GetAllItems().Sum(backPackItem => 
                            backPackItem.GetWeight());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
        private static class PatchTeleportable
        {
            [HarmonyPostfix]
            public static void Postfix(Inventory __instance, ref bool __result)
            {
                foreach (ItemDrop.ItemData item in __instance.GetAllItems())
                {
                    if (item.Extended()?.GetComponent<BackPack.BackPackData>() is { } backPackData)
                    {
                        if (backPackData.inventory!.GetAllItems().Any(i => !i.m_shared.m_teleportable))
                        {
                            __result = false;
                        }
                    }
                }

            }
        }


        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyAfter(new string[]{"GoldenJude_JudesEquipment"})]
        public static class JudeBagPatch
        {
            public static void Postfix(ObjectDB __instance)
            {
                if (__instance.m_StatusEffects.Count <= 0) return;
                __instance.m_StatusEffects.Add(BackPacks.CarryStat);
                var SE = __instance.GetStatusEffect(BackPacks.CarryStat?.name);
                if (BackPacks.useJudesBags)
                {
                    if (__instance.m_items.Count <= 0 || !__instance.GetItemPrefab("Wood")) return;
                    var HeavyBag = __instance.GetItemPrefab("BackpackHeavy");
                    var simpleBag = __instance.GetItemPrefab("BackpackSimple");

                    if (HeavyBag)
                    {
                        var heavybackpack = HeavyBag.transform.Find("attach_skin/heavyBackpack").gameObject
                            .AddComponent<BackPack>();
                        heavybackpack.m_width = 5;
                        heavybackpack.m_height = 3;
                        heavybackpack.m_name = "Backpack";
                        
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


                        simplebackpack.m_width = 3;
                        simplebackpack.m_height = 2;
                        simplebackpack.m_name = "Backpack";
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
        [HarmonyAfter(new string[]{"GoldenJude_JudesEquipment"})]
        public static class JudeBagOtherPatch
        {
            public static void Postfix(ObjectDB __instance)
            {
                if (__instance.m_StatusEffects.Count <= 0) return;
                __instance.m_StatusEffects.Add(BackPacks.CarryStat);
                var SE = __instance.GetStatusEffect(BackPacks.CarryStat?.name);
                if (BackPacks.useJudesBags)
                { 
                    if (__instance.m_items.Count <= 0 || !__instance.GetItemPrefab("Wood")) return;
                    var HeavyBag = __instance.GetItemPrefab("BackpackHeavy");
                    var simpleBag = __instance.GetItemPrefab("BackpackSimple");

                    if (HeavyBag)
                    {
                        var heavybackpack =HeavyBag.transform.Find("attach_skin/heavyBackpack").gameObject.AddComponent<BackPack>();
                        heavybackpack.m_width = 5;
                        heavybackpack.m_height = 3;
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


                        simplebackpack.m_width = 3;
                        simplebackpack.m_height = 2;
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