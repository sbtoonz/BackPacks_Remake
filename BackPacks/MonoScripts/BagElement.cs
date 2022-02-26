using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagElement : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] internal Image m_ItemIcon;
    [SerializeField] internal Image m_Bkg;
    [SerializeField] internal Text m_ItemName;
    [SerializeField] internal GameObject m_ElementGO;
    [SerializeField] internal Text m_Quality;
    [SerializeField] internal ItemDrop.ItemData m_itemData;
#pragma warning restore CS8618
}
