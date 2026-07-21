using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BossDropRework
{
    public partial class BossDropReworkPlugin : BaseUnityPlugin
    {
        public static ExplicitPickupDropTable hordeDropTable;
        void WakeOfVulturesRework()
        {
            ItemDef wakeItemDef = Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_HeadHunter.HeadHunter_asset).WaitForCompletion();
            wakeItemDef.tier = ItemTier.Boss;
            wakeItemDef.deprecatedTier = ItemTier.Boss;

            ExplicitPickupDropTable.PickupDefEntry pickupEntry = new ExplicitPickupDropTable.PickupDefEntry();
            pickupEntry.pickupDef = wakeItemDef;

            hordeDropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
            hordeDropTable.canDropBeReplaced = true;

            List<ExplicitPickupDropTable.PickupDefEntry> pickupDefEntries = new List<ExplicitPickupDropTable.PickupDefEntry>();
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                {
                    pickupDef = wakeItemDef,
                    pickupWeight = 1f
                }
            );
            hordeDropTable.pickupEntries = pickupDefEntries.ToArray();


            //On.RoR2.DeathRewards.Awake += SetDropTableForHordesOfMany;
            //On.RoR2.TeleporterInteraction.Awake += SetBossDirectorDropTable;
            On.RoR2.BossGroup.OnMemberDiscovered += SetHordeDropTable;
        }

        private void SetHordeDropTable(On.RoR2.BossGroup.orig_OnMemberDiscovered orig, BossGroup self, CharacterMaster memberMaster)
        {
            orig(self, memberMaster);
            CharacterBody body = memberMaster.GetBody();
            if (body && body.isChampion == false)
            {
                DeathRewards deathRewards = body.GetComponent<DeathRewards>();
                if(deathRewards)
                    deathRewards.bossDropTable = hordeDropTable;
            }
        }


        private void SetDropTableForHordesOfMany(On.RoR2.DeathRewards.orig_Awake orig, DeathRewards self)
        {
            orig(self);
            CharacterBody body = self.characterBody;
            if (body && /*!body.isChampion &&*/ body.isBoss)
            {
                //Debug.Log(body.name);
                if (self.bossDropTable == null)
                    self.bossDropTable = hordeDropTable;
            }
        }
    }
}
