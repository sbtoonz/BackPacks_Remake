using System.Collections;
using System.Collections.Generic;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BackPacks
{
    public class RPCs : MonoBehaviour
    {
        private static Inventory TempInventory;
        
        
        [HarmonyPatch(typeof(Game), nameof(Game.Start))]
        public static class AddRPC_Patch
        {
            public static void Postfix()
            {
                ZRoutedRpc.instance.Register<string>("AdminRequestBag", AdminRequestBag);
                ZRoutedRpc.instance.Register<ZPackage, long, long>("SendBagContents", SendBagContents);
                ZRoutedRpc.instance.Register<ZPackage, long>("AdminReceiveBag", AdminRecieveBagContents);
            }
        }


        private static void AdminRequestBag(long senderUid, string s)
        {
                ZPackage myInventory = new ZPackage();
                if (Player.m_localPlayer.m_shoulderItem != null)
                {
                    var id = Player.m_localPlayer.m_shoulderItem.Extended();
                    var bag =id.GetComponent<BackPack.BackPackData>();
                    bag.Inventory.Save(myInventory);
                    ZRoutedRpc.instance.InvokeRoutedRPC(senderUid,"SendBagContents", myInventory, senderUid, ZDOMan.instance.GetMyID());
                }
                else
                {
                    ZLog.Log("Player not wearing bag");
                }
                
        }

        private static void SendBagContents(long senderUid, ZPackage package, long adminUID, long ownerUID)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(adminUID,"AdminReceiveBag", package, ownerUID);
        }

        private static void AdminRecieveBagContents(long uid, ZPackage package, long ownerUID)
        {
            TempInventory = new Inventory("test", null, 8, 10);
            TempInventory.Load(package);
            BackPacks.backpackAdmin.SetActive(true);
            BackPacks.backpackAdmin.GetComponent<BagAdminPanel>().PopulateList();
            var interfacepanel = BackPacks.backpackAdmin.GetComponent<BagAdminPanel>();
            foreach (var VARIABLE in TempInventory.m_inventory)
            {
                var elementGO = Object.Instantiate(interfacepanel._element.m_ElementGO, interfacepanel.elementroot);
                var element = elementGO.GetComponent<BagElement>();
                element.m_Bkg.sprite = InventoryGui.instance.transform.Find("root/Player/PlayerGrid").gameObject
                    .GetComponent<InventoryGrid>().m_elementPrefab.transform.Find("selected").gameObject
                    .GetComponent<Image>().sprite;
                element.m_ItemIcon.sprite = VARIABLE.GetIcon();
                element.m_ItemName.text = Localization.instance.Localize(VARIABLE.m_shared.m_name);
                element.m_Quality.text = VARIABLE.m_quality.ToString();
            }
        }


        private static void SendListOfBags(long senderUID, ZPackage zPackage, long adminUID, long ownerUID)
        {
            
        }

        private static void RequestSpecificBag(long senderUID, string bagName, long adminUID, long ownerUID)
        {
            
        }
        //add on click to each item that takes in the owner UID, I also need to assign itemdrop/itemdata to the item I am trying to manipulate
        //Or I need to save this inventory locally and send modified inventory back to player
        

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.InputText))]
        public static class testpatch
        {
            public static bool Prefix(Terminal __instance)
            {
                string lower = __instance.m_input.text.ToLower();
                if (lower.Equals("backpacktest"))
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC("AdminRequestBag", "");
                    return false;
                }

                return true;
            }
        }
    }
}