using BepInEx;
using EntityStates.GoldGat;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.Projectile;
using MonoMod.Cil;
using UnityEngine.Events;
using Mono.Cecil.Cil;
using RiskierRain.Components;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain
{
    partial class RiskierRainPlugin : BaseUnityPlugin
    {

        #region helfire
        void TinctureIgnoreArmor()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += MakeTinctureIgnoreArmor;
        }

        private void MakeTinctureIgnoreArmor(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (damageInfo.dotIndex.HasFlag(DotController.DotIndex.Helfire))
            {
                damageInfo.damageType |= DamageType.BypassArmor;
            }
            orig(self, damageInfo);
        }
        #endregion



        #region blast shower
        public int blastShowerBuffCount = 3; //0
        public void BlastShowerBuff()
        {
            On.RoR2.EquipmentSlot.FireCleanse += BlastShowerProtectionBuffs;
        }

        private bool BlastShowerProtectionBuffs(On.RoR2.EquipmentSlot.orig_FireCleanse orig, EquipmentSlot self)
        {
            if (orig(self))
            {
                for(int i = 0; i < blastShowerBuffCount; i++)
                {
                    self.characterBody.AddBuff(DLC1Content.Buffs.ImmuneToDebuffReady);
                }
                return true;
            }
            return false;
        }
        #endregion
    }
}