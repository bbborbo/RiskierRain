using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class WakeOfVultures : ItemBase<WakeOfVultures>
    {
        public override string ItemName => "Wake of Vultures Retier (Horde of Many Trophy)";

        public override string ItemLangTokenName => throw new NotImplementedException();

        public override string ItemPickupDesc => throw new NotImplementedException();

        public override string ItemFullDescription => throw new NotImplementedException();

        public override string ItemLore => throw new NotImplementedException();

        public override ItemTier Tier => ItemTier.Boss;

        public override ItemTag[] ItemTags => throw new NotImplementedException();

        public override BalanceCategory Category => BalanceCategory.StateOfInteraction;

        public override GameObject ItemModel => throw new NotImplementedException();

        public override Sprite ItemIcon => throw new NotImplementedException();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
        }

        public static ExplicitPickupDropTable hordeDropTable;

        public override void Init(ConfigFile config)
        {
            ItemDef wakeItemDef = RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.HeadHunter), ItemTier.Boss);//Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/HeadHunter/HeadHunter.asset").WaitForCompletion();
            //wakeItemDef.tier = ItemTier.Boss;
            //wakeItemDef.deprecatedTier = ItemTier.Boss;

            ExplicitPickupDropTable.PickupDefEntry pickupEntry = new ExplicitPickupDropTable.PickupDefEntry();
            pickupEntry.pickupDef = wakeItemDef;

            hordeDropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
            hordeDropTable.canDropBeReplaced = true;
            hordeDropTable.pickupEntries = new ExplicitPickupDropTable.PickupDefEntry[] { pickupEntry };

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
                deathRewards.bossDropTable = hordeDropTable;
            }
        }


        private void SetDropTableForHordesOfMany(On.RoR2.DeathRewards.orig_Awake orig, DeathRewards self)
        {
            orig(self);
            CharacterBody body = self.characterBody;
            if (body && /*!body.isChampion &&*/ body.isBoss)
            {
                Debug.Log(body.name);
                if (self.bossDropTable == null)
                    self.bossDropTable = hordeDropTable;
            }
        }
    }
}
