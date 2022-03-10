#define UNITY_COMPILEFLAG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_COMPILEFLAG
using BackPacks;
using ExtendedItemDataFramework;
using UnityEngine.UI;
#endif
using UnityEngine;
using Random = UnityEngine.Random;


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
    public ExtendedItemData? bagdata;
    private bool IsActive => gameObject.activeInHierarchy;
    internal static BagTier StaticTier;
    private static Text? text;
    
    internal static GameObject? AuguaTrashThing;
    internal static GameObject? AugaBackPackTip;
    public float Result { get; private set; }
    public void CloseBag() => InventoryGui.instance.CloseContainer();

#endif



    #region HelperMethods
#if UNITY_COMPILEFLAG
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
     
     private new void DropAllItems()
     {
         if(bagdata == null) return;
         if (!bagdata.HasInventory()) return;
         List<ItemDrop.ItemData> allItems = bagdata!.GetBagInv()!.GetAllItems();
         int num = 1;
         foreach (ItemDrop.ItemData item in allItems)
         {
             Vector3 position = base.transform.position + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f;
             Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
             ItemDrop.DropItem(item, 0, position, rotation);
             num++;
         }
         bagdata!.GetBagInv()!.RemoveAll();
     }

     internal void ApplyConfigToInventory()
     {

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
        if ((bool)m_nview && m_nview.IsOwner())
        {
            string checkstring = "RequestBagOpen";
            int checkhash = checkstring.GetStableHashCode();
            if (m_nview.m_functions.ContainsKey(checkhash)) return;
            m_nview.Register<long>("RequestBagOpen", RPC_RequestOpenBag);
            m_nview.Register<bool>("OpenBagResponse", RPC_OpenBagResponse);
            m_nview.Register<long>("RequestBagTakeAll", RPC_RequestTakeAllFromBag);
            m_nview.Register<bool>("TakeAllBagResponse", RPC_TakeBagAllResponse);
        }
    }
    
    internal void DeRegisterRPC()
    {
        try
        {
            if(m_nview == null) return;
            if (!(bool)m_nview || !m_nview.IsOwner()) return;
            m_nview.Unregister("RequestBagOpen");
            m_nview.Unregister("OpenBagResponse");
            m_nview.Unregister("RequestBagTakeAll");
            m_nview.Unregister("TakeAllBagResponse");
        }
        catch (Exception)
        {
            //ignored
        }
        
    }

    internal void StopCoroutines()
    {
        StopCoroutine(BagContentsChanged(0f));
        StopCoroutine(AddInvWeightToItemWeight(0f));
        if(BackPacks.BackPacks.AlterCarryWeight!.Value)StopCoroutine(WeightOffsetRoutine(0f)); 
    }
    internal void DisableHelpText()
    {
        if (Auga.API.IsLoaded())
        {
            if(AugaBackPackTip != null) AugaBackPackTip.SetActive(false);
        }
        else
        {
            if(text) text!.gameObject.SetActive(false); 
        }  
    }

    internal void StartCoroutines()
    {
        StartCoroutine(BagContentsChanged(0f));
        if(BackPacks.BackPacks.AlterCarryWeight!.Value)StartCoroutine(WeightOffsetRoutine(0f));
        StartCoroutine(AddInvWeightToItemWeight(0f));  
    }

#endif

    #endregion

    #region  UnityEvents

    internal new void Awake()
    {
#if UNITY_COMPILEFLAG
        if (Player.m_localPlayer == null)
        {
            return;
        }
        if (Player.m_localPlayer != null) m_nview = Player.m_localPlayer.m_nview;
        bagdata = Player.m_localPlayer!.m_shoulderItem.Extended();
        ApplyConfigToInventory();
        SetupInventory();
        RegisterRPC();
        StartCoroutines();
        LoadBagContents();
        if(InventoryGui.instance.m_container.gameObject.activeInHierarchy) CloseBag();
        Player.m_localPlayer?.m_shoulderItem.Extended()?.Save();
        StaticTier = tier;
        if (Auga.API.IsLoaded())
        {
            text = InventoryGui.instance.gameObject.transform.Find("root/Player/BackPackToolTip/Content/Text")
                .gameObject.GetComponent<Text>();
        }
#endif
    }
    
    internal void OnDestroy()
    {
#if UNITY_COMPILEFLAG
        bagdata = null;
#endif
    }
    
    internal void OnDisable()
    {
        #if UNITY_COMPILEFLAG
        if(Player.m_localPlayer == null) return;
        if (BackPacks.BackPacks.DropallOnUnEquip!.Value)
        {
            if (ZNetScene.instance == null) return;
            DropAllItems();
            var zPackage = new ZPackage();
            bagdata!.GetBagInv()!.Save(zPackage);
            bagdata!.GetComponent<BackPackData>().PackData = zPackage.GetBase64();
            bagdata.Extended().m_shared.m_teleportable = bagdata.GetBagInv()!.IsTeleportable();
            bagdata.Extended().Save();
        }
        DeRegisterRPC();
        StopCoroutines();
        DisableHelpText();
#endif
    }
    private void Update()
    {
        #if UNITY_COMPILEFLAG
        if(Player.m_localPlayer == null) return;
        if (Player.m_localPlayer.IsDead()) return;
        if (!InventoryGui.IsVisible()) return;
        if (ZInput.GetButton("Backpack") && ZInput.GetButton("AltPlace"))
        { 
            try
            {
                InventoryGui.instance.Show(this);
                //Player.m_localPlayer.m_nview.InvokeRPC("RequestBagOpen", Game.instance.GetPlayerProfile().GetPlayerID());
            }
            catch (Exception)
            {
                // ignored
            }
        }
        if (!InventoryGui.instance.isActiveAndEnabled) return;
        if (Auga.API.IsLoaded())
        {
            if(BackPacks.BackPacks.ShowToolTipText?.Value == false) return;
            AugaBackPackTip = InventoryGui.instance.gameObject.transform.Find("root/Player/BackPackToolTip").gameObject;
        }
        else
        {
            if(BackPacks.BackPacks.ShowToolTipText?.Value == false) return;
            text = InventoryGui.instance.gameObject.transform.Find("root/Player/help_Text").gameObject
                .GetComponent<Text>();
        }

        if (Player.m_localPlayer.m_shoulderItem == null)
        {
            if(BackPacks.BackPacks.ShowToolTipText?.Value == false) return;
            if(text)text!.gameObject.SetActive(false);
            return;
        }
        if (Auga.API.IsLoaded())
        {
            if(BackPacks.BackPacks.ShowToolTipText?.Value == false) return;
            var flag = Player.m_localPlayer.m_shoulderItem.m_shared.m_name.Contains("ackpack");
            if (!flag) return;
            if (!text) return;
            AugaBackPackTip!.SetActive(true);
            
            text!.text =
                $"[<color=yellow>{ZInput.instance.GetButtonDef("AltPlace").m_key.ToString()} & {BackPacks.BackPacks.OpenInventoryKey!.Value.ToString()}</color>] Opens BackPack Inventory";
        }
        else
        {
            if(BackPacks.BackPacks.ShowToolTipText?.Value == false) return;
            var flag = Player.m_localPlayer.m_shoulderItem.m_shared.m_name.Contains("ackpack");
            if (!flag) return;
            if (!text) return;
            text!.gameObject.SetActive(true);
            text.fontSize = 16;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = InventoryGui.instance.gameObject.transform.Find("root/Player/Weight/weight_text").gameObject
                .GetComponent<Text>()
                .font;
            text.text =
                $"[<color=yellow>{ZInput.instance.GetButtonDef("AltPlace").m_key.ToString()} & {BackPacks.BackPacks.OpenInventoryKey!.Value.ToString()}</color>] Opens BackPack Inventory";
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
        if (m_nview == null) return;
        if (!m_loading && m_nview.IsOwner())
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

    private void RPC_RequestOpenBag(long uid, long playerID)
	{
		ZLog.Log("Player " + uid + " wants to open " + base.gameObject.name + "   im: " + ZDOMan.instance.GetMyID());
		if (!m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
		}
		else if ((IsInUse() || ((bool)m_wagon && m_wagon.InUse())) && uid != ZNet.instance.GetUID())
		{
			ZLog.Log("  in use");
			m_nview.InvokeRPC(uid, "OpenBagResponse", false);
		}
		else if (!CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			m_nview.InvokeRPC(uid, "OpenBagResponse", false);
		}
		else
		{
			m_nview.InvokeRPC(uid, "OpenBagResponse", true);
		}
	}

	private void RPC_OpenBagResponse(long uid, bool granted)
	{
		if ((bool)Player.m_localPlayer)
		{
			if (granted)
			{
				InventoryGui.instance.Show(this);
			}
			else
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
			}
		}
	}
    
	private void RPC_RequestTakeAllFromBag(long uid, long playerID)
	{
		ZLog.Log("Player " + uid + " wants to takeall from " + base.gameObject.name + "   im: " + ZDOMan.instance.GetMyID());
		if (!m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
		}
		else if ((IsInUse() || ((bool)m_wagon && m_wagon.InUse())) && uid != ZNet.instance.GetUID())
		{
			ZLog.Log("  in use");
			m_nview.InvokeRPC(uid, "TakeAllBagResponse", false);
		}
		else if (!CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			m_nview.InvokeRPC(uid, "TakeAllBagResponse", false);
		}
		else if (!(Time.time - m_lastTakeAllTime < 2f))
		{
			m_lastTakeAllTime = Time.time;
			m_nview.InvokeRPC(uid, "TakeAllBagResponse", true);
		}
	}
    private void RPC_TakeBagAllResponse(long uid, bool granted)
    {
        if (!Player.m_localPlayer)
        {
            return;
        }
        if (granted)
        {
            Player.m_localPlayer.GetInventory().MoveAll(m_inventory);
            if (m_onTakeAllSuccess != null)
            {
                m_onTakeAllSuccess();
            }
        }
        else
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
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

    public static bool HasInventory(this ItemDrop.ItemData itemdata)
    {
        Inventory? inventory = itemdata.GetBagInv()!;
        return inventory != null;
    }
}
#endif
