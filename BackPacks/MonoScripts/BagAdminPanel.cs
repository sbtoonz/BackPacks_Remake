using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BagAdminPanel : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] internal Dropdown playerlist;
    [SerializeField] internal Dropdown m_bagList;
    [SerializeField] internal RectTransform elementroot;
    [SerializeField] internal BagElement _element;
    [SerializeField] internal Image m_Bkg;
    [SerializeField] internal Button m_InspectBagButton;
    
    
    [SerializeField] internal string SelectedPlayerName;
    [SerializeField] internal List<string> internalplayerlist = new();
    internal string SelectedUID;
    internal long selectedLongID;
    internal List<string> External_list = new();
#pragma warning restore CS8618
    private static BagAdminPanel? m_instance;
    public static BagAdminPanel? instance => m_instance;
    private void OnEnable()
    {
        if (instance == null)
        {
            m_instance = this;
        }
        m_Bkg.sprite = InventoryGui.instance.gameObject.transform.Find("root/Player/Bkg").gameObject.GetComponent<Image>().sprite;
        m_Bkg.material = InventoryGui.instance.gameObject.transform.Find("root/Player/Bkg").gameObject.GetComponent<Image>().material;
       
        
        var settingsbutton =InventoryGui.instance.gameObject.transform.Find("root/Crafting/RepairButton").gameObject
            .GetComponent<Button>();
        m_InspectBagButton.transition = settingsbutton.transition;
        var buttonimag = m_InspectBagButton.GetComponent<Image>();
        var settingsbuttonimage = InventoryGui.instance.gameObject.transform.Find("root/Crafting/RepairButton").gameObject
            .GetComponent<Image>();
        buttonimag.sprite = settingsbuttonimage.sprite;
        buttonimag.material = settingsbuttonimage.material;
        m_InspectBagButton.GetComponentInChildren<Text>().text = "Inspect Bag";
        m_InspectBagButton.GetComponentInChildren<Text>().color = Color.white;

        playerlist.onValueChanged.AddListener(delegate
        {
            ReturnPlayerUID(playerlist);
            SelectedPlayerName = playerlist.options[playerlist.value].text;
        });
        
        m_InspectBagButton.onClick.AddListener(delegate
        {
            PopulateList();
        });
        
        m_bagList.onValueChanged.AddListener(delegate
        {
            baglist();
        });
    }

    internal void baglist()
    {
        SelectedUID = m_bagList.options[m_bagList.value].text;
        m_InspectBagButton.onClick.RemoveAllListeners();
        m_InspectBagButton.onClick.AddListener(delegate
        {
            ZPackage package = new ZPackage();
            package.Write(SelectedUID);
            ZRoutedRpc.instance.InvokeRoutedRPC(selectedLongID,"RequestSpecificBag", 
                package,ZDOMan.instance.GetMyID(), selectedLongID);

        });
    }

    private void OnDisable()
    {
        if (instance != null)
        {
            m_instance = null;
        }
    }

    internal void ReturnPlayerUID(Dropdown PlayerName)
    {
        SelectedPlayerName = PlayerName.options[PlayerName.value].text;
        var temp = ZNet.instance.GetPlayerList();
        foreach (var VARIABLE in temp)
        {
            if (VARIABLE.m_name == SelectedPlayerName)
            {
                
                m_InspectBagButton.onClick.RemoveAllListeners();
                m_InspectBagButton.onClick.AddListener(delegate
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZDOMan.instance.GetMyID(),"AdminRequestBag", VARIABLE.m_characterID.m_userID);
                    FindPlayerID(SelectedPlayerName);
                });
            }
        }

        
    }
    internal void PopulateList()
    {
        if (Player.m_localPlayer == null)
            return;
        var playerList = ZNet.instance.GetPlayerList();
        foreach (var playerInfo in playerList) External_list.Add(playerInfo.m_name);

        internalplayerlist = External_list;
        playerlist.ClearOptions();
        playerlist.AddOptions(External_list);
    }

    internal void FindPlayerID(string PlayerName)
    {
        var temp = ZNet.instance.GetPlayerList();
        foreach (var VARIABLE in temp.Where(VARIABLE => VARIABLE.m_name == PlayerName))
        {
            selectedLongID = VARIABLE.m_characterID.m_userID;
        }
    }
}
