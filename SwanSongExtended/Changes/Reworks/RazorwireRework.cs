using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SwanSongExtended.Modules;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended
{
	public partial class SwanSongPlugin
	{
		static float razorwireArmorBase = 8;
		static float razorwireArmorStack = 8;
		private static float razorwireRangeBase = 40; //25
		private static float razorwireRangeStack = 0; //10
		private static float razorwireTargetsBase = 5; //5
		private static float razorwireTargetsStack = 2; //2
		private static float razorwireBleedDuration = 5; //3

		private static float razorwireDamage = 3.6f;
		private static float razorwireProcCoeff = 0.2f;
		private static float razorwireCooldown = 1f;

		public void RazorwireRework()
		{
			GetStatCoefficients += RazorwireArmor;
			On.RoR2.CharacterBody.OnInventoryChanged += AddRazorBehavior;

			IL.RoR2.HealthComponent.TakeDamageProcess += RazorwireBegin;
			IL.RoR2.Orbs.LightningOrb.OnArrival += RazorwireArrival;
            //On.RoR2.Orbs.LightningOrb.Begin += NerfRazorwireOrb;

			LanguageAPI.Add("ITEM_THORNS_PICKUP", 
				"Retaliate in a burst of bleeding razors on taking damage. Recharges over time.");
            LanguageAPI.Add("ITEM_THORNS_DESC",
                $"Increase <style=cIsHealing>armor</style> by <style=cIsHealing>{razorwireArmorBase}</style> <style=cStack>(+{razorwireArmorStack} per stack)</stack>" +
				$"Getting hit causes you to explode in a burst of razors, " +
				$"<style=cIsDamage>bleeding</style> up to <style=cIsDamage>{razorwireTargetsBase}</style> " +
				$"<style=cStack>(+{razorwireTargetsStack} per stack)</style> nearby enemies " +
				$"for <style=cIsDamage>{Tools.ConvertDecimal(razorwireBleedDuration * 0.8f)}</style> base damage " +
				$"per <style=cIsDamage>razor charge</style> expelled. " +
				$"You can hold up to {RazorwireBehavior.baseRazors} <style=cStack>(+{RazorwireBehavior.stackRazors} per stack)</style> " +
				$"razor charges, all reloading over <style=cIsUtility>{RazorwireBehavior.rechargeTime}</style> seconds.");
		}

        private void RazorwireArmor(CharacterBody sender, StatHookEventArgs args)
        {
			if (!sender.inventory)
				return;
			int itemCount = sender.inventory.GetItemCount(RoR2Content.Items.Thorns);
			if(itemCount > 0)
            {
				args.armorAdd += razorwireArmorBase + razorwireArmorStack * (itemCount - 1);
            }
        }

        private void AddRazorBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
		{
			orig(self);
			int razorCount = self.inventory.GetItemCount(RoR2Content.Items.Thorns);
			self.AddItemBehavior<RazorwireBehavior>(razorCount);
		}

		private void RazorwireArrival(ILContext il)
        {
			ILCursor c = new ILCursor(il);

			int healthComponentLoc = 0;
			c.GotoNext(MoveType.After,
				x => x.MatchLdfld<HurtBox>(nameof(HurtBox.healthComponent)),
				x => x.MatchStloc(out healthComponentLoc));

			c.GotoNext(MoveType.After,
				x => x.MatchLdloc(healthComponentLoc));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<HealthComponent, LightningOrb, HealthComponent>>((hc, orb) =>
			{
				if (orb.lightningType == LightningOrb.LightningType.RazorWire && hc != null)
                {
					for(int i = 0; i < orb.procCoefficient; i++)
                    {
						DotController.InflictDot(hc.gameObject, orb.attacker, DotController.DotIndex.Bleed, razorwireBleedDuration, orb.damageValue);
                    }
					return null;
				}
				return hc;
			});
        }

        private void RazorwireBegin(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			int buffCount = 0;

			c.GotoNext(MoveType.After,
				x => x.MatchLdflda<HealthComponent>("itemCounts"),
				x => x.MatchLdfld<HealthComponent.ItemCounts>("thorns")
				);
			c.GotoPrev(MoveType.Before,
				x => x.MatchLdcI4(out _),
				x => x.MatchLdcI4(out _)
				);
			c.Next.Operand = razorwireTargetsBase;
			c.Index++;
			c.Next.Operand = razorwireTargetsStack;


			c.GotoNext(MoveType.After,
				x => x.MatchLdflda<HealthComponent>("itemCounts"),
				x => x.MatchLdfld<HealthComponent.ItemCounts>("thorns"),
				x => x.MatchLdcI4(0)
				);
			c.Index--;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Int32, HealthComponent, Int32>>((itemCount, hc) =>
			{
				CharacterBody body = hc.body;
				buffCount = 0;
				while (body.HasBuff(CommonAssets.razorChargeBuff))
				{
					body.RemoveBuff(CommonAssets.razorChargeBuff);
					buffCount++;
				}
				if (buffCount <= 0)
				{
					return 0;
				}
				return itemCount;
			});

			c.GotoNext(MoveType.After,
				x => x.MatchLdflda<HealthComponent>("itemCounts"),
				x => x.MatchLdfld<HealthComponent.ItemCounts>("thorns")
				);
			c.GotoPrev(MoveType.Before,
				x => x.MatchLdcI4(out _),
				x => x.MatchLdcI4(out _)
				);
			c.Next.Operand = razorwireRangeBase;
			c.Index++;
			c.Next.Operand = razorwireRangeStack;

			c.GotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt<OrbManager>(nameof(OrbManager.AddOrb)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<LightningOrb, HealthComponent, LightningOrb>>((razorwireOrb, hc) =>
			{
				CharacterBody body = hc.body;
				razorwireOrb.procCoefficient = buffCount;
				razorwireOrb.damageValue = body.teamComponent.teamIndex == TeamIndex.Player ? 1 : 0.2f;
				return razorwireOrb;
			});
		}
		private void NerfRazorwireOrb(On.RoR2.Orbs.LightningOrb.orig_Begin orig, LightningOrb self)
		{
			if (self.lightningType == LightningOrb.LightningType.RazorWire)
			{
				self.procCoefficient = razorwireProcCoeff;
				self.damageType.damageType = DamageType.BleedOnHit;
			}

			orig(self);
		}
	}
	public class RazorwireBehavior : CharacterBody.ItemBehavior
	{
		public static float rechargeTime = 5;
		public static int baseRazors = 2;
		public static int stackRazors = 1;

		private float reloadTimer;
		BuffDef razorBuff => CommonAssets.razorChargeBuff;

		void Awake()
        {
			base.enabled = false;
        }

		void OnDisable()
        {
			if(body != null)
			{
				while (body.HasBuff(razorBuff))
				{
					body.RemoveBuff(razorBuff);
				}
			}
        }

		void FixedUpdate()
        {
			int totalRazors = baseRazors + (this.stack - 1) * stackRazors;

			int buffCount = body.GetBuffCount(razorBuff);
			if (buffCount < totalRazors)
            {
				float rechargeInterval = rechargeTime / totalRazors;
				reloadTimer += Time.fixedDeltaTime;
				while(this.reloadTimer > rechargeInterval && buffCount < totalRazors)
                {
					buffCount++;
					body.AddBuff(razorBuff);
					reloadTimer -= rechargeInterval;
                }
            }
		}
    }
}
