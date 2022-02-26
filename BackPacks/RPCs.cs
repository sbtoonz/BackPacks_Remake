using System.Collections;
using System.Collections.Generic;
using BepInEx;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BackPacks
{
    public class RPCs : MonoBehaviour
    { [HarmonyPatch(typeof(Game), nameof(Game.Start))]
        public static class AddRPC_Patch
        {
            public static void Postfix()
            {
                ZRoutedRpc.instance.Register<long>("AdminRequestBag", AdminRequestBag);
                ZRoutedRpc.instance.Register<ZPackage, long, long>("SendBagContents", SendBagContents);
                ZRoutedRpc.instance.Register<ZPackage, long>("AdminReceiveBag", AdminRecieveBagContents);
                ZRoutedRpc.instance.Register<ZPackage, long, long>("GatherBagList", GatherListOfBags);
                ZRoutedRpc.instance.Register<ZPackage, long, long>("ReceiveBagList", RcvListOfBags);
                ZRoutedRpc.instance.Register<ZPackage, long, long>("RequestSpecificBag", RequestSpecificBag);
                ZRoutedRpc.instance.Register<ZPackage, long, long>("RemoveSpecificItemReq", RemoveItemReq);
                ZRoutedRpc.instance.Register<ZPackage>("RemoveItemResp", RemoveItemResp);
            }
        }


        private static void AdminRequestBag(long senderUid, long targetUID)
        {
                ZPackage myInventory = new ZPackage();
                if (!BackPacks.ServerConfigLocked.Value)
                {
                    Player.m_localPlayer.GetInventory().Save(myInventory);
                    
                    ZRoutedRpc.instance.InvokeRoutedRPC(targetUID,"GatherBagList", myInventory, senderUid, ZDOMan.instance.GetMyID());
                }
                else
                {
                    ZLog.Log("Not an admin");
                }
                
        }

        private static void SendBagContents(long senderUid, ZPackage package, long adminUID, long ownerUID)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(adminUID,"AdminReceiveBag", package, ownerUID);
        }

        private static void AdminRecieveBagContents(long uid, ZPackage package, long ownerUID)
        {
            Inventory TempInventory = new Inventory("test", null, 8, 10);
            TempInventory.Load(package);
            BackPacks.backpackAdmin!.SetActive(true);
            BackPacks.backpackAdmin.GetComponent<BagAdminPanel>().PopulateList();
            var interfacepanel = BackPacks.backpackAdmin.GetComponent<BagAdminPanel>();
            foreach (Transform transform in interfacepanel.elementroot)
            {
                Destroy(transform.gameObject);
            }
            foreach (var VARIABLE in TempInventory.GetAllItems())
            {
                var elementGO = Object.Instantiate(interfacepanel._element.m_ElementGO, interfacepanel.elementroot);
                var element = elementGO.GetComponent<BagElement>();
                element.m_Bkg.sprite = InventoryGui.instance.transform.Find("root/Player/PlayerGrid").gameObject
                    .GetComponent<InventoryGrid>().m_elementPrefab.transform.Find("selected").gameObject
                    .GetComponent<Image>().sprite;
                element.m_ItemIcon.sprite = VARIABLE.GetIcon();
                element.m_ItemName.text = Localization.instance.Localize(VARIABLE.m_shared.m_name);
                element.m_Quality.text = VARIABLE.m_quality.ToString();
                element.m_ItemName.font = InventoryGui.instance.transform.Find("root/Player/help_Text").gameObject
                    .GetComponent<Text>().font;
                element.m_ItemName.fontSize = InventoryGui.instance.transform.Find("root/Player/help_Text").gameObject
                    .GetComponent<Text>().fontSize;
                element.m_ItemName.color = Color.white;
                var button = elementGO.AddComponent<Button>();
                element.m_itemData = VARIABLE;
                button.onClick.AddListener(new UnityAction(delegate
                {
                    Debug.Log(element.m_ItemName);
                    Debug.Log(element.m_itemData.m_shared.m_description);
                }));
            }
        }
        
        private static void GatherListOfBags(long senderUID, ZPackage zPackage, long adminUID, long ownerUID)
        {
            Inventory temp = new Inventory("temp", null, 8, 10);
            temp.Load(zPackage);
            Dictionary<string, string> tempDict = new Dictionary<string, string>();
            ZPackage package = new ZPackage();
                foreach (var ID in temp.m_inventory)
                {
                    if (ID.IsBackpack())
                    {
                        tempDict.Add(ID.m_shared.m_name, ID.Extended().GetUniqueId());
                    }
                }
                package.Write(tempDict.Count);
                foreach (var VARIABLE in tempDict)
                {
                    package.Write(VARIABLE.Key);
                    package.Write(VARIABLE.Value);
                }
                ZRoutedRpc.instance.InvokeRoutedRPC("ReceiveBagList", package, adminUID, ownerUID);
            
        }

        private static void RcvListOfBags(long senderUID, ZPackage zPackage, long adminUID, long ownerUID)
        {
            int num = zPackage.ReadInt();
            for (int i = 0; i < num; i++)
            {
                string name = zPackage.ReadString();
                string UID = zPackage.ReadString();
                if (name.Contains("ackpack"))
                {
                    BagAdminPanel.instance?.m_bagList.ClearOptions();
                    BagAdminPanel.instance?.m_bagList.AddOptions(new List<string>
                    {
                        UID
                    });
                    BagAdminPanel.instance?.m_bagList.gameObject.SetActive(true);
                    BagAdminPanel.instance?.baglist();
                }
            }
            
        }

        private static void RequestSpecificBag(long senderUID, ZPackage zPackage, long adminUID, long ownerUID)
        {
            string bagUID = zPackage.ReadString();
            foreach (var Idata in Player.m_localPlayer.GetInventory().GetAllItems())
            {
                if (Idata.Extended().GetUniqueId() == bagUID)
                {
                    ZPackage bagInvpkg = new ZPackage();
                    Inventory? inv = new Inventory("temp", null, 8, 10);
                    if (!Idata.HasInventory()) return;
                    inv = Idata.GetBagInv();
                    inv?.Save(bagInvpkg);
                    ZRoutedRpc.instance.InvokeRoutedRPC(senderUID, "AdminReceiveBag", bagInvpkg, senderUID);
                }
            }
        }


        private static void RemoveItemReq(long senderUID, ZPackage package, long adminUID, long targetUID)
        {
            
        }

        private static void RemoveItemResp(long senderUID, ZPackage package)
        {
            
        }
        
        
        //add on click to each item that takes in the owner UID, I also need to assign itemdrop/itemdata to the item I am trying to manipulate
        //Or I need to save this inventory locally and send modified inventory back to player
        

        
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.InputText))]
        public static class testpatch
        {
            public static bool Prefix(Terminal __instance)
            {
                string phrase = __instance.m_input.text;
                string[] words = phrase.Split(' ');
                if (words[0] == "backpacktest")
                {
                    if (words[1].IsNullOrWhiteSpace()) return true;
                    long test = long.Parse(words[1]);
                    ZRoutedRpc.instance.InvokeRoutedRPC("AdminRequestBag", test);
                    return false;
                }

                if (words[0] == "inspectbag")
                {
                    string UID = words[1];
                    ZPackage package = new ZPackage();
                    package.Write(UID);
                    long playerID = long.Parse(words[2]);
                    ZRoutedRpc.instance.InvokeRoutedRPC(playerID,
                        "RequestSpecificBag", 
                        package, 
                        ZDOMan.instance.GetMyID(),
                        playerID);
                    return false;
                }
                return true;
            }
        }
    }
}