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

    public BagTier tier = BagTier.UnKnown;
    public int fixedWidth = 0;
    public int fixedHeight = 0;

#if UNITY_COMPILEFLAG
    private bool IsActive => gameObject.activeInHierarchy;
    internal static BagTier StaticTier;
    private Text? text;
    
    private static BackPack? m_instance;
    public static BackPack? instance => m_instance;
    public float Result { get; private set; }
    public void CloseBag() => InventoryGui.instance.CloseContainer();

#endif



    #region HelperMethods

     internal void SetupInventory()
     { 
         if (Player.m_localPlayer.IsDead()) return;
         if(Player.m_localPlayer.m_shoulderItem == null) return;
         var item = Player.m_localPlayer.m_shoulderItem;
         m_inventory = new Inventory(m_name, m_bkg, fixedWidth+item.m_quality/2, fixedHeight+item.m_quality/2);
         m_width = fixedWidth+item.m_quality/2;
         m_height = fixedHeight+item.m_quality/2;
         Inventory inventory = m_inventory;
         inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(OnBackPackChange)); 
    }

     internal void ApplyConfigToInventory()
     {
#if UNITY_COMPILEFLAG
         switch (tier)
         {
             case BagTier.Leather:
                 fixedHeight = Mathf.CeilToInt(BackPacks.BackPacks.LeatherBagSize!.Value.y);
                 fixedWidth = Mathf.CeilToInt(BackPacks.BackPacks.LeatherBagSize.Value.x);
                 break;
             case BagTier.Iron:
                 fixedHeight = Mathf.CeilToInt(BackPacks.BackPacks.IronBagSize!.Value.y);
                 fixedWidth = Mathf.CeilToInt(BackPacks.BackPacks.IronBagSize.Value.x);
                 break;
             case BagTier.Silver:
                 fixedHeight = Mathf.CeilToInt(BackPacks.BackPacks.SilverBagSize!.Value.y);
                 fixedWidth = Mathf.CeilToInt(BackPacks.BackPacks.SilverBagSize.Value.x);
                 break;
             case BagTier.BlackMetal:
                 break;
             case BagTier.UnKnown:
                 fixedHeight = Mathf.CeilToInt(BackPacks.BackPacks.UnknownBagSize!.Value.y);
                 fixedWidth = Mathf.CeilToInt(BackPacks.BackPacks.UnknownBagSize.Value.x);
                 break;
         }
#endif
     }
    internal void AssignInventory(Inventory inv)
    {
        
        m_inventory = inv;
        Inventory inventory = m_inventory;
        inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(OnBackPackChange)); 

    }

    internal void AssignContainerSize(int m_quality)
    {
        m_width = fixedWidth+m_quality/2;
        m_height = fixedHeight+m_quality/2;
        m_inventory.m_width = m_width;
        m_inventory.m_height = m_height;
    }
    
    internal void RegisterRPC()
    {
        if ((bool)m_nview)
        {
            m_nview.Register<long>("RequestOpen", RPC_RequestOpen);
            m_nview.Register<bool>("OpenRespons", RPC_OpenRespons);
            m_nview.Register<long>("RequestTakeAll", RPC_RequestTakeAll);
            m_nview.Register<bool>("TakeAllRespons", RPC_TakeAllRespons);
            m_nview.Register<string>("AdminInspectReq", RPC_AdminPeekContentsReq);
            m_nview.Register<string>("AdminInspectRespons", RPC_AdminPeekContentsResponse);
        }
    }
    
    internal void DeRegisterRPC()
    {
        m_nview.Unregister("RequestOpen");
        m_nview.Unregister("OpenRespons");
        m_nview.Unregister("RequestTakeAll");
        m_nview.Unregister("TakeAllRespons");
        m_nview.Unregister("AdminInspectReq");
        m_nview.Unregister("AdminInspectRespons"); 
    }

    internal void StopCoroutines()
    {
        StopCoroutine(BagContentsChanged(0f));
        StopCoroutine(AddInvWeightToItemWeight(0f));
        if(BackPacks.BackPacks.AlterCarryWeight!.Value)StopCoroutine(WeightOffsetRoutine(0f)); 
    }
    internal void DisableHelpText()
    {
        if(text) text!.gameObject.SetActive(false);   
    }

    internal void StartCoroutines()
    {
        StartCoroutine(BagContentsChanged(0f));
        if(BackPacks.BackPacks.AlterCarryWeight!.Value)StartCoroutine(WeightOffsetRoutine(0f));
        StartCoroutine(AddInvWeightToItemWeight(0f));  
    }

    #endregion

    #region  UnityEvents

    internal new void Awake()
    {
        m_instance = this;
#if UNITY_COMPILEFLAG
        if (Player.m_localPlayer == null)
        {
            return;
        }
        if (Player.m_localPlayer != null) m_nview = Player.m_localPlayer.m_nview;
        ApplyConfigToInventory();
        SetupInventory();
        RegisterRPC();
        StartCoroutines();
        LoadBagContents();
        if(InventoryGui.instance.m_container.gameObject.activeInHierarchy) CloseBag();
        Player.m_localPlayer?.m_shoulderItem.Extended()?.Save();
        StaticTier = tier;
#endif
    }
    internal void OnDestroy()
    {
#if UNITY_COMPILEFLAG
        if (m_instance == this)
        {
            m_instance = null;

        }
#endif
    }
    internal void OnDisable()
    {
        #if UNITY_COMPILEFLAG
        if(Player.m_localPlayer == null) return;
        DeRegisterRPC();
        StopCoroutines();
        DisableHelpText();
#endif
    }
    private void Update()
    {
        #if UNITY_COMPILEFLAG
        if(Player.m_localPlayer == null) return;
        if (!Player.m_localPlayer.IsOnGround()) return;
        if (Player.m_localPlayer.IsDead()) return;
        
        if (!InventoryGui.IsVisible()) return;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(BackPacks.BackPacks.OpenInventoryKey!.Value))
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
        if (!InventoryGui.instance.isActiveAndEnabled) return;
        text = InventoryGui.instance.gameObject.transform.Find("root/Player/help_Text").gameObject
            .GetComponent<Text>();
        if (Player.m_localPlayer.m_shoulderItem == null)
        {
            if(text)text.gameObject.SetActive(false);
            return;
        }
                     
        var flag = Player.m_localPlayer.m_shoulderItem.m_shared.m_name.Contains("ackpack");
        if (flag)
        {
            if(!text) return;
            text.gameObject.SetActive(true);
            text.fontSize = 16;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = InventoryGui.instance.gameObject.transform.Find("root/Player/Weight/weight_text").gameObject.GetComponent<Text>()
                .font;
            text.text = $"[<color=yellow>Left Shift & {BackPacks.BackPacks.OpenInventoryKey!.Value.ToString()}</color>] Opens BackPack Inventory";
                        
        }
#endif
    }

    #endregion

    #region Enumerators

    #if UNITY_COMPILEFLAG
    private IEnumerator WeightOffsetRoutine(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);
            if (!m_nview.IsValid()) break;
            if (Player.m_localPlayer.IsDead()) break;
            if(Player.m_localPlayer.m_shoulderItem == null) break;
            Result = 0f;
            Result += tier switch
            {
                BagTier.Iron => m_inventory.GetAllItems()
                    .Sum(backPackItem => backPackItem.GetWeight() * BackPacks.BackPacks.CarryModifierIron!.Value),
                BagTier.Leather => m_inventory!.GetAllItems()
                    .Sum(backPackItem => backPackItem.GetWeight() * BackPacks.BackPacks.CarryModifierLeather!.Value),
                BagTier.Silver => m_inventory!.GetAllItems()
                    .Sum(backPackItem => backPackItem.GetWeight() * BackPacks.BackPacks.CarryModifierSilver!.Value),
                BagTier.BlackMetal => m_inventory!.GetAllItems()
                    .Sum(backPackItem => backPackItem.GetWeight()),
                BagTier.UnKnown => m_inventory!.GetAllItems()
                    .Sum(backPackItem => backPackItem.GetWeight() * BackPacks.BackPacks.CarryModifierUnKnown!.Value),
                _ => m_inventory!.GetAllItems().Sum(backPackItem => backPackItem.GetWeight())
            };
            m_inventory.m_totalWeight = Result;
        }
    }
    
    private IEnumerator AddInvWeightToItemWeight(float time)
    {
        
        while (true)
        {
            yield return new WaitForSeconds(time);
            if(!m_nview.IsValid()) break;
            if (Player.m_localPlayer.IsDead()) break;
            if(Player.m_localPlayer.m_shoulderItem == null) break;
            if (Player.m_localPlayer.m_shoulderItem.IsBackpack())
            {
                var inv = Player.m_localPlayer.m_shoulderItem.GetBagInv();
                ItemDrop.ItemData? itemData = Player.m_localPlayer.m_shoulderItem.Extended();
                itemData.m_shared.m_weight = 4f;
                var temp = inv!.m_totalWeight;
                itemData.m_shared.m_weight += temp;
                Player.m_localPlayer.m_inventory.UpdateTotalWeight();
            }
        }
    }
#endif
    private IEnumerator BagContentsChanged(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);
            if (!m_nview.IsValid()) break;
            LoadBagContents();
        }
    }
    #endregion

    #region BagContentsFunctions

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
            if (player.m_shoulderItem != null)
            {
                player.m_shoulderItem.m_shared.m_teleportable = m_inventory.IsTeleportable();
            }
            
        }
#endif
    }
    
    internal void LoadBagContents()
    {
#if UNITY_COMPILEFLAG
        if (!InventoryGui.instance.m_player.gameObject.activeInHierarchy) return;
        if (Player.m_localPlayer.m_shoulderItem?.Extended().GetComponent<BackPackData>() is not {} backPackData)
        {
            return;
        }
        if (string.IsNullOrEmpty(backPackData.PackData) || backPackData.PackData == m_lastDataString)
        {
            return;
        }
        var pkg = new ZPackage(backPackData.PackData);
        m_loading = true;
        m_inventory.Load(pkg);
        m_inventory.m_totalWeight = backPackData.Inventory!.m_totalWeight;
        m_loading = false;
        m_lastRevision = m_nview.GetZDO().m_dataRevision;
        m_lastDataString = backPackData.PackData;
        if (Player.m_localPlayer.m_shoulderItem != null)
        {
            if(Player.m_localPlayer.m_shoulderItem.IsExtended()) Player.m_localPlayer.m_shoulderItem.Extended().m_shared.m_teleportable = backPackData.Inventory.IsTeleportable();
            Player.m_localPlayer.m_shoulderItem.m_shared.m_teleportable = backPackData.Inventory.IsTeleportable();
        }
        SavePack();
#endif

    }

    private void SavePack()
    {
#if UNITY_COMPILEFLAG
        if(Player.m_localPlayer.m_shoulderItem == null) return;
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
        m_lastDataString = backPackData.PackData = zPackage.GetBase64();
        backPackData.Inventory = m_inventory;
        if(BackPacks.BackPacks.AlterCarryWeight!.Value)backPackData.Inventory.m_totalWeight = Result;
        backPackData.Tier = tier;
        Player.m_localPlayer.m_shoulderItem.Extended().Save();
#endif
    }

    #endregion

    #region RPCs

#if UNITY_COMPILEFLAG
    private void RPC_AdminPeekContentsResponse(long uid, string s)
    {
        ZLog.Log("Admin " + uid + " Wants to inspect: " + base.gameObject.name + " im: " + ZDOMan.instance.GetMyID());
        InventoryGui.instance.Show(this);

    }

    private void RPC_AdminPeekContentsReq(long uid, string playerName)
    {
        if(ZNet.instance.m_adminList.Contains(playerName))
        {
            m_nview.InvokeRPC(uid, "AdminInspectRespons", "");
        }
        else
        {
            Debug.Log("Non admin invoking inspect command");
        }
    }
#endif

    #endregion

    #region EIDF

#if UNITY_COMPILEFLAG
    public class BackPackData : BaseExtendedItemComponent
    {
        public const string DataID = "BackPack";
        public string PackData = "";
        public Inventory? Inventory;
        public BagTier Tier;
        public BackPackData(ExtendedItemData parent) : base(typeof(BackPackData).AssemblyQualifiedName, parent) { }
        public override string Serialize() => PackData;

        public void SetInventory(Inventory inventoryInstance)
        {
            Inventory = inventoryInstance;
            Save(); 
        }
        public Inventory? ReturnInventory()
        {
            return Inventory!;
        }
        public override void Deserialize(string data)
        {
            PackData = data;
            Inventory ??= new Inventory("", null, 100, 100);
            if (data != "")
            {
                var pkg = new ZPackage(data);
                Inventory.Load(pkg);
            }
        }
        
        public override BaseExtendedItemComponent? Clone()
        {
            return MemberwiseClone() as BaseExtendedItemComponent;
        }
    }
#endif

    #endregion


    
}

#if UNITY_COMPILEFLAG
public static class EIDFHelper
{
    public static bool IsBackpack(this ItemDrop.ItemData itemData)
    {
        return itemData.Extended()?.GetComponent<BackPack.BackPackData>() != null;
    }

    public static bool HasInventory(this ItemDrop.ItemData itemData, out Inventory? inventory)
    {
        inventory = itemData.GetBagInv();
        return inventory != null;
    }

    public static BackPack.BagTier? BagTier(this ItemDrop.ItemData itemData, out BackPack.BagTier? bagTier)
    {
        bagTier =itemData.Extended()?.GetComponent<BackPack.BackPackData>().Tier;
        if (bagTier != null)
        {
            return bagTier;
        }
        else
        {
            return null;
        }
    }

    public static float? BagWeight(this ItemDrop.ItemData itemData)
    {
        var inv = itemData.GetBagInv();
        var weight = inv?.GetTotalWeight();
        return weight;
    }

    public static Inventory? GetBagInv(this ItemDrop.ItemData itemData)
    {
        return itemData.Extended()?.GetComponent<BackPack.BackPackData>()?.ReturnInventory();
        
    }
}
#endif
