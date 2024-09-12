using BepInEx;
using RiskierRain.CoreModules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.CharacterBody;
using UnityEngine.AddressableAssets;

namespace RiskierRain
{
	internal partial class RiskierRainPlugin : BaseUnityPlugin
	{
		float resdiscSpinPerKill = 0.015f; //0.025f
		float resdiscDecayRate = 2f; //1.25f


		private GameObject daggerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/DaggerProjectile");
		private GameObject willowispPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/WilloWispDelay");
		private GameObject voidsentPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ExplodeOnDeathVoid/ExplodeOnDeathVoidExplosion.prefab").WaitForCompletion();
		private GameObject spleenPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/BleedOnHitAndExplodeDelay");
		private GameObject fireworkProjectilePrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/FireworkProjectile");
		private GameObject resdiscProjectilePrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/LaserTurbineBomb");

		private GameObject meatballProjectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");

		private static float willowispBaseDamage = 2f;
		private static float willowispScaleFraction = 0.8f;
		private static float willowispBaseRange = 16f;
		private static float willowispStackRange = 0f;

		private static float voidsentBaseDamage = 3.5f;
		private static float voidsentScaleFraction = 0.8f;
		private static float voidsentBaseRange = 24f;
		private static float voidsentStackRange = 0f;
		private static float voidsentBaseChance = 33f;
		private static float voidsentStackChance = 0f;

		private static float gasBaseBurnDamage = 0.5f;
		private static float gasStackBurnDamage = 2f;
		private static float gasBaseDamage = 0.5f;
		private static float gasStackDamage = 0;

		private static float razorwireDamage = 3.6f;
		private static float razorwireProcCoeff = 0.2f;
		private static float razorwireCooldown = 1f;

		public static bool useNkuhanaKnockbackSlow = false;
		public static bool useDiscipleKnockbackSlow = false;


		private void BuffDisciple()
		{
			On.RoR2.Items.SprintWispBodyBehavior.FixedUpdate += FixDiscipleBS;
			IL.RoR2.Items.SprintWispBodyBehavior.Fire += NerfDiscipleDamage;
			useDiscipleKnockbackSlow = true;

			LanguageAPI.Add("ITEM_SPRINTWISP_DESC",
				$"Fire a <style=cIsDamage>tracking wisp</style> " +
				$"for <style=cIsDamage>300% damage</style> " +
				$"that <style=cIsUtility>pushes and slows</style> enemies for 3 seconds. " +
				$"Fires every <style=cIsUtility>1</style><style=cStack>(-50% per stack)</style> seconds " +
				$"while sprinting. Fire rate increases with <style=cIsUtility>movement speed</style>.");
		}

        private void WillowispNerfs()
        {
            this.willowispPrefab.GetComponent<RoR2.DelayBlast>().procCoefficient = 0f;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += WillOWispChanges;
            LanguageAPI.Add("ITEM_EXPLODEONDEATH_DESC",
                $"On killing an enemy, spawn a <style=cIsDamage>lava pillar</style> in a <style=cIsDamage>{willowispBaseRange}m</style> radius for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(willowispBaseDamage)}</style> <style=cStack>(+{Tools.ConvertDecimal(willowispBaseDamage * willowispScaleFraction)} per stack)</style> base damage.");
        }

        private void VoidsentNerfs()
        {
            this.voidsentPrefab.GetComponent<RoR2.DelayBlast>().procCoefficient = 0f;
            IL.RoR2.HealthComponent.TakeDamageProcess += VoidsentFlameChanges;
            LanguageAPI.Add("ITEM_EXPLODEONDEATHVOID_PICKUP",
                $"Chance to detonate full health enemies on hit. <style=cIsVoid>Corrupts all Will-o'-the-wisps</style>.");
            LanguageAPI.Add("ITEM_EXPLODEONDEATHVOID_DESC",
                $"Upon hitting an enemy at or above <style=cIsDamage>100% health</style>, " +
				$"has a {voidsentBaseChance}% chance to " +
				$"<style=cIsDamage>detonate</style> them in a <style=cIsDamage>{voidsentBaseRange}m</style> radius " +
				$"for <style=cIsDamage>{Tools.ConvertDecimal(voidsentBaseDamage)}</style> " +
				$"<style=cStack>(+{Tools.ConvertDecimal(voidsentBaseDamage * voidsentScaleFraction)} per stack)</style> base damage. " +
				$"<style=cIsVoid>Corrupts all Will-o'-the-wisps</style>.");
        }

        private void CeremonialDaggerNerfs()
        {
            this.daggerPrefab.GetComponent<ProjectileController>().procCoefficient = 0f;
            this.daggerPrefab.GetComponent<ProjectileSimple>().lifetime = 3;
        }

        private void GasolineChanges()
        {
            IL.RoR2.GlobalEventManager.ProcIgniteOnKill += GasChanges;
            LanguageAPI.Add("ITEM_IGNITEONKILL_DESC",
                $"Killing an enemy <style=cIsDamage>ignites</style> all enemies within " +
                $"<style=cIsDamage>12m</style> <style=cStack>(+4m per stack)</style> " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(gasBaseDamage)}</style> base damage. " +
                $"Additionally, enemies <style=cIsDamage>burn</style> " +
                $"for <style=cIsDamage>{50 * (gasBaseBurnDamage + gasStackBurnDamage)}%</style> " +
                $"<style=cStack>(+{50 * (gasStackBurnDamage)}% per stack)</style> base damage.");
        }

        private void ResonanceDiscNerfs()
        {
            this.resdiscProjectilePrefab.GetComponent<ProjectileController>().procCoefficient = 0; //0.5f
            this.resdiscProjectilePrefab.GetComponent<ProjectileImpactExplosion>().blastProcCoefficient = 0; //0.5f
            EntityStates.LaserTurbine.FireMainBeamState.mainBeamProcCoefficient = 0;
        }

        private void RazorwireReworks()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += RazorwireNerf;
            LanguageAPI.Add("ITEM_THORNS_DESC",
                $"Getting hit causes you to explode in a burst of razors, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(razorwireDamage)} damage</style>. " +
                $"Hits up to <style=cIsDamage>5</style> <style=cStack>(+2 per stack)</style> targets " +
                $"in a <style=cIsDamage>25m</style> <style=cStack>(+10m per stack)</style> radius");
        }

        #region nkuhana
        float nkuhanaNewDamageMultiplier = 3.5f; //2.5
		void BuffNkuhana()
		{
			IL.RoR2.HealthComponent.ServerFixedUpdate += NkuhanasBuff;
			RiskierRainPlugin.useNkuhanaKnockbackSlow = true;

			LanguageAPI.Add("ITEM_NOVAONHEAL_DESC",
				$"Store <style=cIsHealing>100%</style> <style=cStack>(+100% per stack)</style> of healing as <style=cIsHealing>Soul Energy</style>. " +
				$"After your <style=cIsHealing>Soul Energy</style> reaches <style=cIsHealing>10%</style> of your <style=cIsHealing>maximum health</style>, " +
				$"<style=cIsDamage>fire a skull</style> that deals <style=cIsDamage>{Tools.ConvertDecimal(nkuhanaNewDamageMultiplier)}</style> " +
				$"of your <style=cIsHealing>Soul Energy</style> as <style=cIsDamage>damage</style>.");
		}

		private void NkuhanasBuff(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<DevilOrb>(nameof(DevilOrb.damageValue))
				);

			c.Index -= 2;
			c.Remove();
			c.Emit(OpCodes.Ldc_R4, nkuhanaNewDamageMultiplier);
		}
		#endregion

		private void RazorwireNerf(ILContext il)
        {
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.After,
				x => x.MatchLdflda<HealthComponent>("itemCounts"),
				x => x.MatchLdfld<HealthComponent./*private*/ItemCounts>("thorns"),
				x => x.MatchLdcI4(0)
				);
			c.Index--;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Int32, HealthComponent, Int32>>((itemCount, hc) =>
			{
				CharacterBody body = hc.body;
                if (body.HasBuff(CoreModules.Assets.noRazorwire))
                {
					itemCount = 0;
                }
                else if (itemCount > 0)
                {
					body.AddTimedBuffAuthority(CoreModules.Assets.noRazorwire.buffIndex, razorwireCooldown);
                }

				return itemCount;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchLdcR4(out _),
				x => x.MatchLdarg(0),
				x => x.MatchLdfld<HealthComponent>("body")
				//,x => x.MatchCallOrCallvirt<CharacterBody>("damage")
				);
			c.Remove();
			c.Emit(OpCodes.Ldc_R4, razorwireDamage);
        }

        private void NerfDiscipleDamage(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.Orbs.DevilOrb>("damageValue")
				);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<float, SprintWispBodyBehavior, float>>((damageIn, behavior) => {
				float damageOut = damageIn / behavior.stack;
				return damageOut;
            });
		}

        private void FixDiscipleBS(On.RoR2.Items.SprintWispBodyBehavior.orig_FixedUpdate orig, SprintWispBodyBehavior self)
		{
			CharacterBody body = self.body;
			if (body.isSprinting)
			{
				self./*private*/fireTimer -= Time.fixedDeltaTime;
				if (self./*private*/fireTimer <= 0f)
				{
					self./*private*/fireTimer += (body.baseMoveSpeed * 1.45f) / (body.moveSpeed * self.stack);
					self./*private*/Fire();
				}
			}
		}

        private void BuffDevilOrb(ILContext il)
        {
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.DamageInfo>("force")
				);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Vector3, DevilOrb, Vector3>>((forceIn, orb) =>
			{
				HealthComponent healthComponent = orb.target.healthComponent;

				float forceMultiplier = 1;
				if (healthComponent.body.characterMotor != null)
				{
					forceMultiplier = healthComponent.body.characterMotor.mass;
				}
				else if (healthComponent.body.rigidbody != null)
				{
					forceMultiplier = healthComponent.body.rigidbody.mass;
				}

				switch (orb.effectType)
				{
					case DevilOrb.EffectType.Skull:
                        if (useNkuhanaKnockbackSlow) 
						{ 
							forceMultiplier *= 25;
							healthComponent.body.AddTimedBuffAuthority(RoR2Content.Buffs.Slow50.buffIndex, 3);
						}
						else 
						{ 
							forceMultiplier *= 0; 
						}
						break;
					case DevilOrb.EffectType.Wisp:
						if (useDiscipleKnockbackSlow) 
						{ 
							forceMultiplier *= 15;
							healthComponent.body.AddTimedBuffAuthority(RoR2Content.Buffs.Slow50.buffIndex, 3);
						}
						else 
						{ 
							forceMultiplier *= 0; 
						}
						break;
				}

				Vector3 forceOut = (orb.target.transform.position - orb.attacker.transform.position).normalized * (100 + forceMultiplier);

				return forceOut;
			});
		}

		private void GasChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.InflictDotInfo>(nameof(RoR2.InflictDotInfo.totalDamage))
				);
			c.Index--;
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, DamageReport, int, float>>((currentDamage, damageReport, itemCount) =>
			{
				float newBurnDamage = currentDamage;

				newBurnDamage = (gasBaseBurnDamage + gasStackBurnDamage * itemCount) * damageReport.attackerBody.damage;

				return newBurnDamage;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.BlastAttack>(nameof(RoR2.BlastAttack.baseDamage))
				);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, DamageReport, int, float>>((currentDamage, damageReport, itemCount) =>
			{
				float newHitDamage = currentDamage;

				newHitDamage = (gasBaseDamage + gasStackDamage * itemCount) * damageReport.attackerBody.damage;

				return newHitDamage;
			});
		}

		private void GasChangesOld(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			c.GotoNext(MoveType.After,
				x => x.MatchLdcR4(1.5f),
				x => x.MatchLdcR4(1.5f),
				x => x.MatchLdarg(1),
				x => x.MatchConvR4(),
				x => x.MatchMul(),
				x => x.MatchAdd()
				);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, int, float>>((currentDuration, itemCount) =>
			{
				float newDuration = currentDuration;

				newDuration = gasBaseBurnDamage + gasStackBurnDamage * itemCount;

				return newDuration;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.BlastAttack>(nameof(RoR2.BlastAttack.baseDamage))
				);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate<Func<float, DamageReport, int, float>>((currentDamage, damageReport, itemCount) =>
			{
				float newDamage = currentDamage;

				newDamage = (gasBaseDamage + gasStackDamage * itemCount) * damageReport.attackerBody.damage;

				return newDamage;
			});
		}

		private void WillOWispChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			int countLoc = -1;
			c.GotoNext(MoveType.After,
				x => x.MatchLdsfld("RoR2.RoR2Content/Items", "ExplodeOnDeath"),
				x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
				x => x.MatchStloc(out countLoc)
				);

			c.GotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnKillProcDamage))
				);

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentDamage, itemCount) =>
			{
				float newDamage = willowispBaseDamage * (1 + willowispScaleFraction * (itemCount - 1));

				return newDamage;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.DelayBlast>(nameof(RoR2.DelayBlast.radius))
				);

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentRadius, itemCount) =>
			{
				float newRadius = willowispBaseRange + willowispStackRange * itemCount;

				return newRadius;
			});
		}

		private void VoidsentFlameChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			int countLoc = -1;
			c.GotoNext(MoveType.Before,
				x => x.MatchLdsfld("RoR2.DLC1Content/Items", "ExplodeOnDeathVoid"),
				x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
				x => x.MatchStloc(out countLoc)
				);
			c.Index += 2;
			c.EmitDelegate<Func<int, int>>((itemCountIn) =>
			{
				if(itemCountIn > 0)
				{
					if (Util.CheckRoll(voidsentBaseChance + voidsentStackChance * (itemCountIn - 1)))
					{
						return itemCountIn;
					}
				}
				return 0;
			});

			//return;
			c.GotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnKillProcDamage))
				);

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentDamage, itemCount) =>
			{
				float newDamage = voidsentBaseDamage * (1 + voidsentScaleFraction * (itemCount - 1));

				return newDamage;
			});

			c.GotoNext(MoveType.Before,
				x => x.MatchStfld<RoR2.DelayBlast>(nameof(RoR2.DelayBlast.radius))
				);

			c.Emit(OpCodes.Ldloc, countLoc);
			c.EmitDelegate<Func<float, int, float>>((currentRadius, itemCount) =>
			{
				float newRadius = voidsentBaseRange + voidsentStackRange * itemCount;

				return newRadius;
			});
		}

		float discipleDevilorbProc = 1;
		float opinionDevilorbProc = 1;
		private void NerfDevilOrb(On.RoR2.Orbs.DevilOrb.orig_Begin orig, DevilOrb self)
		{
			switch (self.effectType)
			{
				case DevilOrb.EffectType.Skull:
					self.procCoefficient = 0;
					break;
				case DevilOrb.EffectType.Wisp:
					self.procCoefficient = 0;
					break;
			}
			orig(self);
		}
		private void NerfRazorwireOrb(On.RoR2.Orbs.LightningOrb.orig_Begin orig, LightningOrb self)
		{
            switch (self.lightningType)
            {
				case LightningOrb.LightningType.Tesla:
					//self.procCoefficient = 0;
					break;
				case LightningOrb.LightningType.RazorWire:
					if (!Tools.isLoaded("Rein.GeneralFixes")) self.procCoefficient = razorwireProcCoeff;
					break;
            }

			orig(self);
		}
		private void NerfChargedPerforatorOrb(On.RoR2.Orbs.SimpleLightningStrikeOrb.orig_Begin orig, SimpleLightningStrikeOrb self)
		{
			self.procCoefficient = 0f;

			orig(self);
		}
	}
}