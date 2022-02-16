using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace BackPacks
{
    public class Patches
    {
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalWeight))]
        [HarmonyPriority(Priority.Last)]
        private static class GetTotalWeightPatch
        {
            private static void Postfix(Inventory __instance, ref float __result)
            {
                
                if (!Player.m_localPlayer) return;
                if (__instance.m_name == BackPack.StaticInventory?.m_name)
                {
                    List<ItemDrop.ItemData> items = __instance.GetAllItems();
                    var player = Player.m_localPlayer;
                    foreach (var item in items.Where(item => item.m_shared.m_name.Contains("backpack")))
                    {
                        EjectBackpack(item, player, __instance);
                        break;
                    }

                    items = __instance.GetAllItems();
                    foreach (var item in items.Where(item => item.m_shared.m_name.Contains("Backpack")))
                    {
                        EjectBackpack(item, player, __instance);
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
        [HarmonyPriority(Priority.Last)]
        private static class GetWeightPatch
        {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (__instance.Extended()?.GetComponent<BackPack.BackPackData>() is { } backPackData)
                {
                    __result += backPackData.inventory!.GetAllItems().Sum(backPackItem => backPackItem.GetWeight());
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
                    }

                    if (simpleBag)
                    {
                        var simplebackpack = simpleBag.transform.Find("attach_skin/simpleBackpack").gameObject
                            .AddComponent<BackPack>();


                        simplebackpack.m_width = 3;
                        simplebackpack.m_height = 2;
                        simplebackpack.m_name = "Backpack";
                    }
                    
                }
            }
        }
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        [HarmonyAfter(new string[]{"GoldenJude_JudesEquipment"})]
        public static class JudeBagOtherPatch
        {
            public static void Postfix(ObjectDB __instance)
            {
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

                    }

                    if (simpleBag)
                    {
                        var simplebackpack = simpleBag.transform.Find("attach_skin/simpleBackpack").gameObject
                            .AddComponent<BackPack>();


                        simplebackpack.m_width = 3;
                        simplebackpack.m_height = 2;
                    }

                }
            }
        }
        


        private static void EjectBackpack(ItemDrop.ItemData item, Player player, Inventory backpackInventory)
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