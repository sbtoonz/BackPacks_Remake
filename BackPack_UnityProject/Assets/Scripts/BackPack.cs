#define UNITY_COMPILEFLAG
using System;
using System.Collections;
#if UNITY_COMPILEFLAG
using ExtendedItemDataFramework;
#endif
using UnityEngine;


public class BackPack : Container
{
    [Serializable]
    internal enum BagTier
    {
        Leather,
        Iron,
        Silver,
        BlackMetal,
        UnKnown
    }

    [SerializeField] internal BagTier tier;
    internal static BagTier StaticTier;
    [SerializeField] internal ItemDrop ItemDataref;
    [SerializeField] internal GameObject originalDrop;
    public string MUID;
#if UNITY_COMPILEFLAG
    private bool IsActive => gameObject.activeInHierarchy;
    private float TotalWeight => m_inventory.GetTotalWeight();
    
    internal static bool StaticActive;
    internal static Inventory? StaticInventory;

#endif
    

    private new void Awake()
    {
        #if UNITY_COMPILEFLAG
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
#endif
    }

    private void OnEnable()
    {
        if(Player.m_localPlayer ==null) return;
        
        StartCoroutine(BagContentsChanged(0f));
    }

    private void OnDisable()
    {
        #if UNITY_COMPILEFLAG
        if(Player.m_localPlayer == null) return;
        m_nview.Unregister("RequestOpen");
        m_nview.Unregister("OpenRespons");
        m_nview.Unregister("RequestTakeAll");
        m_nview.Unregister("TakeAllRespons");
        StopCoroutine(BagContentsChanged(0f));
        #endif
    }

    private void Update()
    {
        #if UNITY_COMPILEFLAG
        if(Player.m_localPlayer == null) return;
        if (!InventoryGui.IsVisible()) return;
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.T))
        {
            try
            {
                Interact(Player.m_localPlayer, false, false);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        StaticActive = IsActive;
        StaticInventory = m_inventory;
        StaticTier = tier;
        #endif
    }

    private void OnBackPackChange()
    {
#if UNITY_COMPILEFLAG
        if (!m_loading && IsOwner())
        {
            
            SavePack();
        }
#endif
    }
    /*private void BagContentsChanged()
    {
        if (!m_nview.IsValid()) return;
        LoadBagContents();
        UpdateUseVisual();
    }*/

    private IEnumerator BagContentsChanged(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);
            if (!m_nview.IsValid()) break;
            LoadBagContents();
            UpdateUseVisual();
        }
    }

    internal void LoadBagContents()
    {
#if UNITY_COMPILEFLAG
        if (Player.m_localPlayer.m_shoulderItem?.Extended().GetComponent<BackPackData>() is not {} backPackData)
        {
            return;
        }

        if (string.IsNullOrEmpty(backPackData.packData) || backPackData.packData == m_lastDataString)
        {
            return;
        }
        var pkg = new ZPackage(backPackData.packData);
        m_loading = true;
        m_inventory.Load(pkg);
        m_loading = false;
        m_lastRevision = m_nview.GetZDO().m_dataRevision;
        m_lastDataString = backPackData.packData;
#endif
        
    }

    internal void SavePack()
    {
#if UNITY_COMPILEFLAG
        if (Player.m_localPlayer.m_shoulderItem?.Extended().GetComponent<BackPackData>() is not {} backPackData)
        {
            if (Player.m_localPlayer.m_shoulderItem is null)
            {
                return;
            }

            backPackData = Player.m_localPlayer.m_shoulderItem.Extended().AddComponent<BackPackData>();
        }

        var zPackage = new ZPackage();
        m_inventory.Save(zPackage);
        
        m_lastRevision = m_nview.GetZDO().m_dataRevision;
        m_lastDataString = backPackData.packData = zPackage.GetBase64();
        backPackData.inventory = m_inventory;
        
        Player.m_localPlayer.m_shoulderItem.Extended().Save();
#endif
    }
#if UNITY_COMPILEFLAG
    public class BackPackData : BaseExtendedItemComponent
    {
        public string packData = "";
        public Inventory? inventory;

        public BackPackData(ExtendedItemData parent) : base(typeof(BackPackData).AssemblyQualifiedName, parent) { }

        public override string Serialize() => packData;
        public override void Deserialize(string data)
        {
            packData = data;

            inventory = new Inventory("", null, 100, 100);
            if (data != "")
            {
                var pkg = new ZPackage(data);
                inventory.Load(pkg);
            }
        }

        public override BaseExtendedItemComponent Clone() => (BaseExtendedItemComponent)MemberwiseClone();
    }
#endif
}
