using System;
using UnityEngine;

namespace BackPacks
{
    public class SeCarryWeight : SE_Stats
    {
        internal SeCarryWeight()
        {
            name = "SECarryWeight";
            m_name = "";
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            if (Player.m_localPlayer != null)
            {
                
                if (Player.m_localPlayer.m_shoulderItem != null)
                {
                    m_icon = Player.m_localPlayer.m_shoulderItem.GetIcon();
                    var bag = Player.m_localPlayer.gameObject.transform.Find("Visual");
                    switch (BackPack.StaticTier)
                    {
                        case BackPack.BagTier.Iron:
                            m_addMaxCarryWeight = BackPacks.CarryBonusIron!.Value;
                            break;
                        case BackPack.BagTier.Leather:
                            m_addMaxCarryWeight = BackPacks.CarryBonusLeather!.Value;
                            break;
                        case BackPack.BagTier.Silver:
                            m_addMaxCarryWeight = BackPacks.CarryBonusSilver!.Value;
                            break;
                        case BackPack.BagTier.BlackMetal:

                            break;
                        case BackPack.BagTier.UnKnown:
                            m_addMaxCarryWeight = BackPacks.CarryBonusUnKnown!.Value;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}