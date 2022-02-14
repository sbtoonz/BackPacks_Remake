using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BackPacks
{
    public class Patches
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetKnownTexts))]
        private static class FixCompendium
        {
            private static void Postfix(ref List<KeyValuePair<string, string>> __result)
            {
                __result = __result.Where(p => !p.Key.StartsWith("backpacks.")).ToList();
            }
        }

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
                }
                if (__instance != Player.m_localPlayer.GetInventory()) return;
                if (new StackFrame(2).ToString().IndexOf("OverrideGetTotalWeight", StringComparison.Ordinal) > -1) return;
                if(BackPack.StaticActive) __result += BackPack.StaticWeight;
                else
                {
                    __result = 0;
                }
                
            }
        }

        [HarmonyPatch(typeof(Player), "OnDeath")]
        private static class OnDeath_Patch
        {
            private static void Prefix(Player __instance)
            {
                //__instance.GetInventory().m_inventory.FirstOrDefault(i => i.m_shared.m_name.Contains("backpack"))
                foreach (BackPack backPack in from Idata in __instance.GetInventory().m_inventory where Idata.m_shared.m_name.Contains("leather_backpack") select Idata.m_dropPrefab.transform.Find("attach_skin").gameObject
                             .GetComponentInChildren<BackPack>() into backPack where backPack select backPack)
                {
                    backPack.m_inventory = new Inventory(backPack.m_name, backPack.m_bkg, backPack.m_width, backPack.m_height);
                    backPack.m_nview = Player.m_localPlayer.m_nview;
                    backPack.LoadBagContents();
                    if (backPack.m_inventory.m_inventory.Count <= 0)
                    {
                        return;
                    }
                    var bagStone = GameObject.Instantiate(BackPacks.bagTombStone,
                        __instance.GetCenterPoint() + Vector3.forward, __instance.transform.rotation);
                    var tombStone = bagStone?.GetComponent<TombStone>();
                    var PlayerProfile = Game.instance.GetPlayerProfile();
                    tombStone?.Setup(PlayerProfile.GetName(), PlayerProfile.GetPlayerID());
                    bagStone?.GetComponent<Container>().GetInventory().MoveInventoryToGrave(backPack.m_inventory);
                    backPack.m_inventory.RemoveAll();
                    backPack.SavePack();
                    break;
                }
                foreach (BackPack backPack in from Idata in __instance.GetInventory().m_inventory where Idata.m_shared.m_name.Contains("iron_backpack") select Idata.m_dropPrefab.transform.Find("attach_skin").gameObject
                             .GetComponentInChildren<BackPack>() into backPack where backPack select backPack)
                {
                    backPack.m_inventory = new Inventory(backPack.m_name, backPack.m_bkg, backPack.m_width, backPack.m_height);
                    backPack.m_nview = Player.m_localPlayer.m_nview;
                    backPack.LoadBagContents();
                    if (backPack.m_inventory.m_inventory.Count <= 0)
                    {
                        return;
                    }
                    var bagStone = GameObject.Instantiate(BackPacks.bagTombStone,
                        __instance.GetCenterPoint() + Vector3.forward, __instance.transform.rotation);
                    var tombStone = bagStone?.GetComponent<TombStone>();
                    var PlayerProfile = Game.instance.GetPlayerProfile();
                    tombStone?.Setup(PlayerProfile.GetName(), PlayerProfile.GetPlayerID());
                    bagStone?.GetComponent<Container>().GetInventory().MoveInventoryToGrave(backPack.m_inventory);
                    backPack.m_inventory.RemoveAll();
                    backPack.SavePack();
                    break;
                }
                foreach (BackPack backPack in from Idata in __instance.GetInventory().m_inventory where Idata.m_shared.m_name.Contains("silver_backpack") select Idata.m_dropPrefab.transform.Find("attach_skin").gameObject
                             .GetComponentInChildren<BackPack>() into backPack where backPack select backPack)
                {
                    backPack.m_inventory = new Inventory(backPack.m_name, backPack.m_bkg, backPack.m_width, backPack.m_height);
                    backPack.m_nview = Player.m_localPlayer.m_nview;
                    backPack.LoadBagContents();
                    if (backPack.m_inventory.m_inventory.Count <= 0)
                    {
                        return;
                    }
                    var bagStone = GameObject.Instantiate(BackPacks.bagTombStone,
                        __instance.GetCenterPoint() + Vector3.forward, __instance.transform.rotation);
                    var tombStone = bagStone?.GetComponent<TombStone>();
                    var PlayerProfile = Game.instance.GetPlayerProfile();
                    tombStone?.Setup(PlayerProfile.GetName(), PlayerProfile.GetPlayerID());
                    bagStone?.GetComponent<Container>().GetInventory().MoveInventoryToGrave(backPack.m_inventory);
                    backPack.m_inventory.RemoveAll();
                    backPack.SavePack();
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
        private static class PatchTeleportable
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                if (BackPack.StaticActive)
                {
                    if(BackPack.StaticInventory!.m_inventory.Any(i => i.m_shared.m_teleportable == false)) __result = false;
                }

                if (!Player.m_localPlayer.GetInventory()
                        .ContainsItem(BackPacks.IronBag?.Prefab.GetComponent<ItemDrop>().m_itemData) && !Player
                        .m_localPlayer.GetInventory()
                        .ContainsItem(BackPacks.SilverBag?.Prefab.GetComponent<ItemDrop>().m_itemData)) return;
                {
                    if(BackPack.StaticInventory!.m_inventory.Any(i => i.m_shared.m_teleportable == false)) __result = false;
                }
                if (Player.m_localPlayer.GetInventory().m_inventory.Any(i=>i.m_shared.m_teleportable == false))
                {
                    __result = false;
                }
            }
        }


        private static void EjectBackpack(ItemDrop.ItemData item, Player player, Inventory backpackInventory)
        {
            var playerInventory = player.GetInventory();

            // Move the backpack to the player's Inventory if there's room.
            if (playerInventory.HaveEmptySlot())
            {
                playerInventory.MoveItemToThis(backpackInventory, item);
            }

            // Otherwise drop the backpack.
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