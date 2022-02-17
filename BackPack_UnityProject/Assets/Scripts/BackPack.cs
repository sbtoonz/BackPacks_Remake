#define UNITY_COMPILEFLAG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_COMPILEFLAG
using ExtendedItemDataFramework;
using UnityEngine.UI;
#endif
using UnityEngine;

    
public class BackPack : Container
{
    [Serializable]
    public enum BagTier
    {
        Leather,
        Iron,
        Silver,
        BlackMetal,
        UnKnown
    }

    [SerializeField] internal BagTier tier = BagTier.UnKnown;

#if UNITY_COMPILEFLAG
    private bool IsActive => gameObject.activeInHierarchy;
    private float TotalWeight => m_inventory.GetTotalWeight();

    internal static bool StaticActive;
    internal static BagTier StaticTier;
    private Text? text;

#endif
    

    private new void Awake()
    {
        #if UNITY_COMPILEFLAG
        if (Player.m_localPlayer == null)
        {
            return;
        }
        if (Player.m_localPlayer != null) m_nview = Player.m_localPlayer.m_nview;
        m_inventory = new Inventory(m_name, m_bkg, m_width += Player.m_localPlayer!.m_shoulderItem.m_quality/2, m_height+= Player.m_localPlayer.m_shoulderItem.m_quality/2);
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
        StaticTier = tier;
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
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.E))
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
        text = InventoryGui.instance.gameObject.transform.Find("root/Player/help_Text").gameObject
            .GetComponent<Text>();
        if (StaticActive)
        {
            
            if (Player.m_localPlayer.m_shoulderItem == null)
            {
                text.gameObject.SetActive(false);
                return;
            }
                     
            var flag = Player.m_localPlayer.m_shoulderItem.m_shared.m_name.Contains("ackpack");
            if (flag)
            {
                text.gameObject.SetActive(true);
                text.fontSize = 16;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.font = InventoryGui.instance.gameObject.transform.Find("root/Player/Weight/weight_text").gameObject.GetComponent<Text>()
                    .font;
                text.text = "[<color=yellow>Left Shift & E</color>] Opens BackPack Inventory";
                        
            }
        }
        else
        {
            text.gameObject.SetActive(false);
        }
        #endif
    }

    private void OnBackPackChange()
    {
#if UNITY_COMPILEFLAG
        if (!m_loading && IsOwner())
        {
            List<ItemDrop.ItemData> items = m_inventory.GetAllItems();
            var player = Player.m_localPlayer;
            foreach (var item in items.Where(item => item.m_shared.m_name.Contains("ackpack")))
            {
                BackPacks.Patches.EjectBackpack(item, player, m_inventory);
                break;
            }
            SavePack();
        }
#endif
    }
    private IEnumerator BagContentsChanged(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);
            if (!m_nview.IsValid()) break;
            LoadBagContents();
        }
    }

    private void LoadBagContents()
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
        tier = backPackData.Tier;
#endif

    }

    private void SavePack()
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
        public const string dataID = "BackPack";
        public string packData = "";
        public Inventory? inventory;
        public BagTier Tier;
        public BackPackData(ExtendedItemData parent) : base(typeof(BackPackData).AssemblyQualifiedName, parent) { }

        public void SetInventory(Inventory Inv)
        {
            inventory = Inv;
            Save();
        }
        public Inventory GetInventory()
        {
            return inventory!;
        }
        public override string Serialize()
        {
            ZPackage zPackage = new ZPackage();
            inventory?.Save(zPackage);
            string inv64 = zPackage.GetBase64();
            return inv64;
        }

        public override void Deserialize(string data)
        {
            packData = data;
            inventory ??= new Inventory("", null, 100, 100);
            if (data != "")
            {
                var pkg = new ZPackage(data);
                inventory.Load(pkg);
            }
            Save();
        }
        
        public override BaseExtendedItemComponent? Clone()
        {
            return MemberwiseClone() as BaseExtendedItemComponent;
        }
    }
#endif
}
