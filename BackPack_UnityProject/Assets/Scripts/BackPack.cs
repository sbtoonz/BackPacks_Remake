using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class BackPack : Container
{
    private Inventory _mInventory;

    public BackPack(Inventory mInventory)
    {
        _mInventory = mInventory;
    }
    
    
    private new void Awake()
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }
        if (Player.m_localPlayer != null) m_nview = Player.m_localPlayer.m_nview;
       m_inventory = new Inventory(m_name, m_bkg, m_width, m_height);
       Inventory inventory = m_inventory;
       inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(OnBackPackChange));
       m_piece = GetComponent<Piece>();
       if ((bool)m_nview)
       {
           m_nview.Register<long>("RequestOpen", RPC_RequestOpen);
           m_nview.Register<bool>("OpenRespons", RPC_OpenRespons);
           m_nview.Register<long>("RequestTakeAll", RPC_RequestTakeAll);
           m_nview.Register<bool>("TakeAllRespons", RPC_TakeAllRespons);
       }
       WearNTear wearNTear = (m_rootObjectOverride ? m_rootObjectOverride.GetComponent<WearNTear>() : GetComponent<WearNTear>());
       if ((bool)wearNTear)
       {
           wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(OnDestroyed));
       }
       Destructible destructible = (m_rootObjectOverride ? m_rootObjectOverride.GetComponent<Destructible>() : GetComponent<Destructible>());
       if ((bool)destructible)
       {
           destructible.m_onDestroyed = (Action)Delegate.Combine(destructible.m_onDestroyed, new Action(OnDestroyed));
       }
       InvokeRepeating(nameof(BagContentsChanged), 0f, 1f);
    }

    private void OnDisable()
    {
        if(Player.m_localPlayer == null) return;
        m_nview.Unregister("RequestOpen");
        m_nview.Unregister("OpenRespons");
        m_nview.Unregister("RequestTakeAll");
        m_nview.Unregister("TakeAllRespons");
    }

    private void Update()
    {
        if(Player.m_localPlayer == null) return;
        if (!InventoryGui.IsVisible()) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact(Player.m_localPlayer, false, false);
        }
    }

    private void OnBackPackChange()
    {
        if (!m_loading && IsOwner())
        {
            SavePack();
        }
    }
    private void BagContentsChanged()
    {
        if (!m_nview.IsValid()) return;
        LoadBagContents();
        UpdateUseVisual();
    }
    
    private void LoadBagContents()
    {
        string bagAndWorld = ObjectDB.instance.GetPrefabHash(gameObject).ToString() + ZNet.m_world.m_uid;
        var bagandworld64 = EncodeTo64(bagAndWorld);
        var base64String = m_nview.GetZDO().GetString(bagandworld64);
        if (string.IsNullOrEmpty(base64String) && base64String != m_lastDataString) return;
        var pkg = new ZPackage(base64String);
        m_loading = true;
        m_inventory.Load(pkg);
        m_loading = false;
        m_lastRevision = m_nview.GetZDO().m_dataRevision;
        m_lastDataString = base64String;
        
        
    }

    private void SavePack()
    {
        var zPackage = new ZPackage();
        m_inventory.Save(zPackage);
        var base64 = zPackage.GetBase64();
        string mUid = ObjectDB.instance.GetPrefabHash(gameObject).ToString() + ZNet.m_world.m_uid;
        var to64 = EncodeTo64(mUid);
        m_nview.GetZDO().Set(to64, base64);
        m_lastRevision = m_nview.GetZDO().m_dataRevision;
        m_lastDataString = base64;
    }
    
    static public string EncodeTo64(string toEncode)

    {

        byte[] toEncodeAsBytes

            = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);

        string returnValue

            = System.Convert.ToBase64String(toEncodeAsBytes);

        return returnValue;

    }
}
