using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagAdminPanel : MonoBehaviour
{
    [SerializeField] internal Dropdown playerlist;
    [SerializeField] internal RectTransform elementroot;
    [SerializeField] internal BagElement _element;
    [SerializeField] internal Image m_Bkg;
    [SerializeField] internal static string SelectedPlayerName;
    [SerializeField] internal static List<string> internalplayerlist = new();
    internal List<string> External_list = new();

    private void OnEnable()
    {
        m_Bkg.sprite = InventoryGui.instance.gameObject.transform.Find("root/Player/Bkg").gameObject.GetComponent<Image>().sprite;
        m_Bkg.material = InventoryGui.instance.gameObject.transform.Find("root/Player/Bkg").gameObject.GetComponent<Image>().material;
        
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
}
