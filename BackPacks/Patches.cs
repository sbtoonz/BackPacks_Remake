using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace BackPacks
{
    public class Patches
    {

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        public static class InventoryGui_DoCrafting_Patch
        {
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
        public static class EquipItemPatch
        {
            public static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool __runOriginal,
	            bool triggerEquipEffects = true)
            {
	            if (!__runOriginal)
	            {
		            return false;
	            }
                if (__instance == Player.m_localPlayer)
                {
	                return EquipItem(item, __instance, triggerEquipEffects);
                }

                return EquipItem(item, __instance, triggerEquipEffects);
            }

            private static bool EquipItem(ItemDrop.ItemData item, Humanoid humanoid, bool triggerEquipEffects = true)
            {
                if (humanoid.IsItemEquiped(item.Extended()))
                {
	                return false;
                }
                if (!humanoid.m_inventory.ContainsItem(item))
                {
	                return false;
                }
                if (humanoid.InAttack() || humanoid.InDodge())
                {
	                return false;
                }
                if (humanoid.IsPlayer() && !humanoid.IsDead() && humanoid.IsSwiming() && !humanoid.IsOnGround())
                {
	                return false;
                }
                if (item.m_shared.m_useDurability && item.m_durability <= 0f)
                {
	                return false;
                }
                if (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc))
                {
	                humanoid.Message(MessageHud.MessageType.Center, "$msg_dlcrequired");
	                return false;
                }
                if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool)
                {
	                humanoid.UnequipItem(humanoid.m_rightItem, triggerEquipEffects);
	                humanoid.UnequipItem(humanoid.m_leftItem, triggerEquipEffects);
	                humanoid.m_rightItem = item.Extended().ExtendedClone();
	                humanoid.m_hiddenRightItem = null;
	                humanoid.m_hiddenLeftItem = null;
	                humanoid.m_rightItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
                {
	                if (humanoid.m_rightItem != null && humanoid.m_leftItem == null && humanoid.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
	                {
		                humanoid.m_leftItem = item.Extended().ExtendedClone();
	                }
	                else
	                {
		                humanoid.UnequipItem(humanoid.m_rightItem, triggerEquipEffects);
		                if (humanoid.m_leftItem != null && humanoid.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
		                {
			                humanoid.UnequipItem(humanoid.m_leftItem, triggerEquipEffects);
		                }
		                humanoid.m_rightItem = item.Extended().ExtendedClone();
	                }
	                humanoid.m_hiddenRightItem = null;
	                humanoid.m_hiddenLeftItem = null;
	                humanoid.m_rightItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
                {
	                if (humanoid.m_rightItem != null && humanoid.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch && humanoid.m_leftItem == null)
	                {
		                ItemDrop.ItemData rightItem = humanoid.m_rightItem.Extended().ExtendedClone();
		                humanoid.UnequipItem(humanoid.m_rightItem, triggerEquipEffects);
		                humanoid.m_leftItem = rightItem;
		                humanoid.m_leftItem.m_equiped = true;
		                humanoid.m_leftItem.Extended().Save();
	                }
	                humanoid.UnequipItem(humanoid.m_rightItem, triggerEquipEffects);
	                if (humanoid.m_leftItem != null && humanoid.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield && humanoid.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
	                {
		                humanoid.UnequipItem(humanoid.m_leftItem, triggerEquipEffects);
	                }
	                humanoid.m_rightItem = item.Extended().ExtendedClone();
	                humanoid.m_hiddenRightItem = null;
	                humanoid.m_hiddenLeftItem = null;
	                humanoid.m_rightItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                {
	                humanoid.UnequipItem(humanoid.m_leftItem, triggerEquipEffects);
	                if (humanoid.m_rightItem != null && humanoid.m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.OneHandedWeapon && humanoid.m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
	                {
		                humanoid.UnequipItem(humanoid.m_rightItem, triggerEquipEffects);
	                }
	                humanoid.m_leftItem = item.Extended().ExtendedClone();
	                humanoid.m_hiddenRightItem = null;
	                humanoid.m_hiddenLeftItem = null;
	                humanoid.m_leftItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
                {
	                humanoid.UnequipItem(humanoid.m_leftItem, triggerEquipEffects);
	                humanoid.UnequipItem(humanoid.m_rightItem, triggerEquipEffects);
	                humanoid.m_leftItem = item.Extended().ExtendedClone();
	                humanoid.m_hiddenRightItem = null;
	                humanoid.m_hiddenLeftItem = null;
	                humanoid.m_leftItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
                {
	                humanoid.UnequipItem(humanoid.m_leftItem, triggerEquipEffects);
	                humanoid.UnequipItem(humanoid.m_rightItem, triggerEquipEffects);
	                humanoid.m_rightItem = item.Extended().ExtendedClone();
	                humanoid.m_hiddenRightItem = null;
	                humanoid.m_hiddenLeftItem = null;
	                humanoid.m_rightItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
                {
	                humanoid.UnequipItem(humanoid.m_chestItem, triggerEquipEffects);
	                humanoid.m_chestItem = item.Extended().ExtendedClone();
	                humanoid.m_chestItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
                {
	                humanoid.UnequipItem(humanoid.m_legItem, triggerEquipEffects);
	                humanoid.m_legItem = item.Extended().ExtendedClone();
	                humanoid.m_legItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
                {
	                humanoid.UnequipItem(humanoid.m_ammoItem, triggerEquipEffects);
	                humanoid.m_ammoItem = item.Extended().ExtendedClone();
	                humanoid.m_ammoItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet)
                {
	                humanoid.UnequipItem(humanoid.m_helmetItem, triggerEquipEffects);
	                humanoid.m_helmetItem = item.Extended().ExtendedClone();
	                humanoid.m_helmetItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
                {
	                humanoid.UnequipItem(humanoid.m_shoulderItem, triggerEquipEffects);
	                humanoid.m_shoulderItem = item.Extended().ExtendedClone();
	                if (item.IsBackpack())
	                {
		                if(BackPack.instance != null)
		                {
			                if (BackPack.instance.m_inventory != item.GetBagInv())
			                {
				                BackPack.instance.CloseBag();
				                BackPack.instance.DeRegisterRPC();
				                BackPack.instance.StopCoroutines();
				                BackPack.instance.SetupInventory();
				                BackPack.instance.AssignInventory(item.GetBagInv()!);
				                BackPack.instance.AssignContainerSize(item.Extended().m_quality);
				                BackPack.instance.StartCoroutines();
				                BackPack.instance.RegisterRPC();
			                }
		                }
	                }
	                humanoid.m_shoulderItem.Extended().Save();
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
                {
	                humanoid.UnequipItem(humanoid.m_utilityItem, triggerEquipEffects);
	                humanoid.m_utilityItem = item.Extended().ExtendedClone();
	                humanoid.m_utilityItem.Extended().Save();
                }
                if (humanoid.IsItemEquiped(item))
                {
	                item.m_equiped = true;
                }
                humanoid.SetupEquipment();
                if (triggerEquipEffects)
                {
	                humanoid.TriggerEquipEffect(item);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        [HarmonyAfter("GoldenJude_JudesEquipment")]
        public static class JudeBagPatch
        {
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
        [HarmonyAfter("GoldenJude_JudesEquipment")]
        public static class JudeBagOtherPatch
        {
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