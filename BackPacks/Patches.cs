using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
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
                        newQuality);

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
					BackPacks._logSource.LogDebug("Running Equip Patch");
                    if (Player.m_localPlayer == null) return;
                    if(__instance != Player.m_localPlayer) return;
                    if(__instance.gameObject.GetComponent<Player>().GetPlayerName() != Player.m_localPlayer.GetPlayerName())return;
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
	                            BackPacks._logSource.Log(LogLevel.Debug,"Running UnEquip Patch on" + Player.m_localPlayer.GetPlayerName());
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
                    var image = BackPack.AuguaTrashThing.transform.Find("Image").gameObject;
                    image.gameObject.SetActive(false);
                    Object.DestroyImmediate(BackPack.AuguaTrashThing.GetComponent<Button>());
                    Object.DestroyImmediate(BackPack.AuguaTrashThing.GetComponent<AugaUnity.AugaTrasher>());
                    Object.DestroyImmediate(BackPack.AuguaTrashThing.GetComponent<ContentSizeFitter>());
                    Object.DestroyImmediate(BackPack.AuguaTrashThing.GetComponent<HorizontalLayoutGroup>());
                    Object.DestroyImmediate(BackPack.AuguaTrashThing.GetComponent<ButtonSfx>());
                    
                }
                
                /*
                BackPacks.backpackAdmin = Object.Instantiate(BackPacks.backpackAdmin, InventoryGui.instance.gameObject.transform.Find("root/Player/").transform);
                BackPacks.backpackAdmin!.SetActive(false);
                var panel = BackPacks.backpackAdmin.GetComponent<BagAdminPanel>();
                panel.m_Bkg.sprite = __instance.gameObject.transform.Find("root/Player/Bkg").gameObject.GetComponent<Image>().sprite;
                panel.m_Bkg.material = __instance.gameObject.transform.Find("root/Player/Bkg").gameObject
                    .GetComponent<Image>().material;*/
                    
                
            }
        }

        

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.Load))]
        public static class ZinputPatch
        {
            public static void Postfix(ZInput __instance)
            {
                __instance.AddButton("Backpack",
                    BackPacks.OpenInventoryKey!.Value, 0, 0, true, false);
            }
        }
        
        [HarmonyPatch]  
		public static class Player_HaveRequirements_Patch
		{
			[UsedImplicitly]
			public static MethodBase TargetMethod()
			{
				return AccessTools.DeclaredMethod(typeof(Player), "HaveRequirements", new Type[3]
				{
					typeof(Piece.Requirement[]),
					typeof(bool),
					typeof(int)
				}, null!);
		}

		[UsedImplicitly]
		[HarmonyPostfix]
		public static void Postfix(Player __instance, ref bool __result, Piece.Requirement[] resources, int qualityLevel)
		{
			if (Player.m_localPlayer == null || Player.m_localPlayer.m_shoulderItem == null || !Player.m_localPlayer.m_shoulderItem.IsBackpack())
			{
				return;
			}
			Inventory bagInv = __instance.m_shoulderItem.GetBagInv()!;
			if (__result || bagInv == null)
			{
				return;
			}
			foreach (Piece.Requirement requirement in resources)
			{
				if (requirement.m_resItem != null)
				{
					string name = requirement.m_resItem.m_itemData.m_shared.m_name;
					int amount = requirement.GetAmount(qualityLevel);
					int num = __instance.m_inventory.CountItems(name) + bagInv.CountItems(name);
					if (num < amount)
					{
						return;
					}
				}
			}
			__result = true;
		}
	}

	[HarmonyPatch(typeof(Player), "HaveRequirements", new Type[]
	{
		typeof(Piece),
		typeof(Player.RequirementMode)
	})]
	public static class CraftFromBagPiecePatch
	{
		public static void Postfix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
		{
			if(!__result)
			{
				if(__instance.m_shoulderItem == null) return;
				if(!__instance.m_shoulderItem.IsBackpack()) return;
				if ((bool)piece.m_craftingStation)
				{
					if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
					{
						if (!__instance.m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
						{
							return;
						}
					}
					else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, __instance.transform.position))
					{
						return;
					}
				}
				if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
				{
					return;
				}
				Piece.Requirement[] resources = piece.m_resources;
				Piece.Requirement[] array = resources;
				bool hasItem = false;
				foreach (Piece.Requirement requirement in array)
				{
					if (requirement.m_resItem && requirement.m_amount > 0)
					{

						switch (mode)
						{
							case Player.RequirementMode.IsKnown when !__instance.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name):
								return;
							case Player.RequirementMode.CanAlmostBuild:
							{
								if (!__instance.GetInventory().HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
								{
									if (!__instance.m_shoulderItem.IsBackpack()) return;
									if (!__instance.m_shoulderItem.GetBagInv()!.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
									{
										return;
									}
								}
								break;
							}
							case Player.RequirementMode.CanBuild when __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount:
							{
								int hasItems = __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
								hasItems += __instance.m_shoulderItem.GetBagInv()!.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
								if (hasItems > requirement.m_amount)
									hasItem = true;
								if (!hasItem)
									return;
								break;
							}
						}
					}
				}
				__result = hasItem;
			}
			
		}
	}

	[HarmonyPatch(typeof(Inventory), "RemoveItem", new Type[] { typeof(ItemDrop.ItemData) })]
	public static class Inventory_RemoveItem_Patch
	{
		public static void Postfix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
		{
			if (!__result && __instance == Player.m_localPlayer?.m_inventory)
			{
				Inventory bagInv = Player.m_localPlayer.m_shoulderItem.GetBagInv()!;
				if (bagInv != null)
				{
					__result = bagInv.RemoveItem(item);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), "ConsumeResources")]
	public static class Player_ConsumeResources_Patch
	{
		public static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel)
		{
			if (__instance == null)
			{
				return true;
			}
			Inventory bagInv = Player.m_localPlayer.m_shoulderItem.GetBagInv()!;
			if (bagInv == null)
			{
				return true;
			}
			foreach (Piece.Requirement requirement in requirements)
			{
				if (!(requirement.m_resItem != null))
				{
					continue;
				}
				string name = requirement.m_resItem.m_itemData.m_shared.m_name;
				int amount = requirement.GetAmount(qualityLevel);
				if (amount > 0)
				{
					int num = __instance.m_inventory.CountItems(name);
					int num2 = ((num < amount) ? (amount - num) : 0);
					if (num2 > 0)
					{
						bagInv.RemoveItem(name, num2);
					}
				}
			}
			return true;
		}
	}

	[HarmonyPriority(200)]
	[HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
	public static class InventoryGui_SetupRequirement_Patch
	{
		public static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, int quality)
		{
			Text component = elementRoot.transform.Find("res_amount").GetComponent<Text>();
			if (req.m_resItem != null)
			{
				if (!player.HaveRequirements(new Piece.Requirement[1] { req }, false, quality))
				{
					component.color = ((Mathf.Sin(Time.time * 10f) > 0.0) ? Color.red : Color.white);
				}
				else
				{
					component.color = Color.white;
				}
			}
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