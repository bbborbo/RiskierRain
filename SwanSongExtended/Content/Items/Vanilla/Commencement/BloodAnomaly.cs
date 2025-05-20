using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using SwanSongExtended.Modules;
using SwanSongExtended;
using System.Linq;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class BloodAnomaly : ItemBase<BloodAnomaly>
    {
        public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;
        #region config
        public override string ConfigName => "Items : Commencement : Relic of Blood";

        [AutoConfig("Heal Fraction On Kill Base", 0.08f)]
        public static float healFractionOnKillBase = 0.08f;
        [AutoConfig("Heal Fraction On Kill Stack", 0.08f)]
        public static float healFractionOnKillStack = 0.08f;

        [AutoConfig("On-Kill Force Triggers Base", 4)]
        public static int onKillForceTriggersBase = 4;
        [AutoConfig("On-Kill Force Triggers Stack", 2)]
        public static int onKillForceTriggersStack = 2;
		#endregion
		public static BuffDef hiddenForceTriggerCount;
        public override string ItemName => "Relic of Blood";

        public override string ItemLangTokenName => "BLOODANOMALY";

        public override string ItemPickupDesc => "Heal on kill. Damaging powerful enemies force-triggers on-kill effects.";

        public override string ItemFullDescription => 
			$"On killing an enemy, immediately heal for " +
			$"{HealingColor(Tools.ConvertDecimal(healFractionOnKillBase))} {StackText($"+{Tools.ConvertDecimal(healFractionOnKillStack)}")} " +
			$"of {HealingColor("maximum health")}. Dealing damage to {UtilityColor("Champions")} will " +
			$"force-trigger {DamageColor("On-Kill")} effects up to " +
			$"{DamageColor($"{onKillForceTriggersBase}")} {StackText($"+{onKillForceTriggersStack}")} times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Boss;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist , ItemTag.WorldUnique, ItemTag.CannotSteal, ItemTag.AIBlacklist, ItemTag.OnKillEffect };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
		}

		public override void Init()
		{
			hiddenForceTriggerCount = Content.CreateAndAddBuff(
				"bdHiddenRelicForceTriggerCount",
				null, Color.black, true, false);
			hiddenForceTriggerCount.isHidden = true;
			base.Init();
		}

		public override void Hooks()
        {
			On.RoR2.HealthComponent.TakeDamage += BloodRelicOnDamageDealt;
            GlobalEventManager.onCharacterDeathGlobal += BloodRelicOnKill;
        }

        private void BloodRelicOnDamageDealt(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
			orig(self, damageInfo);
			if (!NetworkServer.active)
				return;

			GameObject attacker = damageInfo.attacker;
			if (!attacker)
				return;

			CharacterBody victimBody = self.body;
			if (attacker.TryGetComponent(out CharacterBody attackerBody) && victimBody && victimBody.isChampion)
			{
				int itemCount = GetCount(attackerBody);
				int itemCountTotal = attackerBody.teamComponent ? itemCount : Util.GetItemCountForTeam(attackerBody.teamComponent.teamIndex, ItemsDef.itemIndex, false, false);
				int buffCount = victimBody.GetBuffCount(hiddenForceTriggerCount);
				if(itemCountTotal > 0)
                {
					int maxTriggers = onKillForceTriggersBase + onKillForceTriggersStack * (itemCountTotal - 1);
					float thresholdPerTrigger = 1 / ((float)maxTriggers + 1);
					float nextThreshold = thresholdPerTrigger * (buffCount + 1);

					HealthComponent victimHealthComponent = victimBody.healthComponent;
					if (victimHealthComponent.combinedHealthFraction <= 1 - nextThreshold)
                    {
						victimBody.AddBuff(hiddenForceTriggerCount);
						List<CharacterBody> list = (from master in CharacterMaster.instancesList
													select master.GetBody() into body
													where body && body.teamComponent.teamIndex == TeamIndex.Player && base.GetCount(body) > 0
													select body).ToList<CharacterBody>();
						MakeFakeDeath(victimHealthComponent, damageInfo, list);
                    }
				}
			}
        }

        private void BloodRelicOnKill(DamageReport damageReport)
        {
            CharacterBody attackerBody = damageReport.attackerBody;
            if(attackerBody != null)
            {
                int count = GetCount(attackerBody);
                if(count > 0)
                {
					float healFraction = Util.ConvertAmplificationPercentageIntoReductionNormalized(healFractionOnKillBase + healFractionOnKillStack * (count - 1));
                    attackerBody.healthComponent.HealFraction(healFraction, new ProcChainMask());
                }
            }
		}
		private void MakeFakeDeath(HealthComponent self, DamageInfo damageInfo, List<CharacterBody> attackers)
		{
			foreach (CharacterBody characterBody in attackers)
			{
				DamageInfo damageInfo2 = new DamageInfo
				{
					attacker = ((characterBody != null) ? characterBody.gameObject : null),
					crit = false,
					damage = damageInfo.damage,
					position = damageInfo.position,
					procCoefficient = damageInfo.procCoefficient,
					damageType = damageInfo.damageType,
					damageColorIndex = damageInfo.damageColorIndex
				};
				DamageReport damageReport = new DamageReport(damageInfo2, self, damageInfo.damage, self.combinedHealth);
				GlobalEventManager.instance.OnCharacterDeath(damageReport);
			}
		}
    }
}
