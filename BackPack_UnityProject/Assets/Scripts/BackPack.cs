#define UNITY_COMPILEFLAG
using System;
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
#if UNITY_COMPILEFLAG
    private bool IsActive => gameObject.activeInHierarchy;
    private float TotalWeight => m_inventory.GetTotalWeight();
    
    internal static float StaticWeight;
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
       InvokeRepeating(nameof(BagContentsChanged), 0f, 1f);
       #endif
    }

    private void OnDisable()
    {
        #if UNITY_COMPILEFLAG
        if(Player.m_localPlayer == null) return;
        m_nview.Unregister("RequestOpen");
        m_nview.Unregister("OpenRespons");
        m_nview.Unregister("RequestTakeAll");
        m_nview.Unregister("TakeAllRespons");
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
        StaticWeight = TotalWeight;
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
    
    public static void EjectBackpack(ItemDrop.ItemData item, Player player, Inventory backpackInventory)
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
            Debug.LogAssertion("Clever... But you're still not gonna cause backpackception!");
            backpackInventory.RemoveItem(item);
            ItemDrop.DropItem(item, 1, player.transform.position 
                                       + player.transform.forward 
                                       + player.transform.up, player.transform.rotation);

        }

    }

    private void BagContentsChanged()
    {
        if (!m_nview.IsValid()) return;
        LoadBagContents();
        UpdateUseVisual();
    }

    internal void LoadBagContents()
    {
#if UNITY_COMPILEFLAG
        string bagAndWorld = gameObject.name + ZNet.m_world.m_uid;
        var bagandworld64 = "backpacks." + EncodeTo64(bagAndWorld);
        string? base64String = null;
        var test = Player.m_localPlayer.m_knownTexts.TryGetValue(bagandworld64, out string temp);
        if (test)
        {
            base64String = temp;
        }
        if (string.IsNullOrEmpty(base64String) && base64String != m_lastDataString) return;
        var pkg = new ZPackage(base64String);
        m_loading = true;
        m_inventory.Load(pkg);
        m_loading = false;
        m_lastRevision = m_nview.GetZDO().m_dataRevision;
        m_lastDataString = base64String;
#endif
        
    }

    internal void SavePack()
    {
#if UNITY_COMPILEFLAG
        var zPackage = new ZPackage();
        m_inventory.Save(zPackage);
        var base64 = zPackage.GetBase64();
        string mUid = gameObject.name + ZNet.m_world.m_uid;
        var to64 = "backpacks." + EncodeTo64(mUid);
        if (Player.m_localPlayer.m_knownTexts.TryGetValue(to64, out string inventory))
        {
            Player.m_localPlayer.m_knownTexts.Remove(to64);
        }
        Player.m_localPlayer.m_knownTexts.Add(to64,base64);
        m_lastRevision = m_nview.GetZDO().m_dataRevision;
        m_lastDataString = base64;
        #endif
    }

    internal static void OnDestruction()
    {
        
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
