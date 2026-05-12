using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using MoreStats;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;
using RiskierRain.Components;
using EntityStates.GoldGat;

namespace RiskierRain.Changes
{
    public static partial class EquipmentChanges
    {
        public static void Initialize()
        {
            //eccentric vase
            RiskierRainPlugin.RemoveEquipmentAsync(RoR2_Base_Gateway.Gateway_asset);
            ChangeJadeElephant();
            ChangeCapacitor();
            ChangeTheBackup();
            ChangeFuelArray();
            ChangeGoobo();
            ChangeOcularHud();
            ChangeGlowingMeteorite();
            ChangeCrowdfunder();

            ChangeEnigmaBlacklists();
            //kinda want to remove this but it needs a replacement
            ChangeEquipmentBlacklists();

            //ChangeShower();
            //ChangeTincture();
        }

        #region jade elephant
        public static float elephantBuffDuration = 10;
        public static int elephantArmor = 300;
        public static void ChangeJadeElephant()
        {
            ChangeBuffStacking(nameof(RoR2Content.Buffs.ElephantArmorBoost), true);
            On.RoR2.EquipmentSlot.FireGainArmor += ChangeElephantDuration;
            GetStatCoefficients += ReduceElephantArmor;
            LanguageAPI.Add("EQUIPMENT_GAINARMOR_PICKUP", "Gain massive armor for 10 seconds.");
            LanguageAPI.Add("EQUIPMENT_GAINARMOR_DESC",
                "Gain <style=cIsDamage>200 armor</style> for <style=cIsUtility>10 seconds.</style>");
        }

        public static void ReduceElephantArmor(CharacterBody sender, StatHookEventArgs args)
        {
            int elephantBuffCount = sender.GetBuffCount(RoR2Content.Buffs.ElephantArmorBoost);

            if (elephantBuffCount > 0)
            {
                args.armorAdd += (elephantBuffCount * elephantArmor) - 500;
            }
        }
        public static bool ChangeElephantDuration(On.RoR2.EquipmentSlot.orig_FireGainArmor orig, EquipmentSlot self)
        {
            self.characterBody.AddTimedBuff(RoR2Content.Buffs.ElephantArmorBoost, elephantBuffDuration);
            return true;
        }
        #endregion
        #region royal capacitor
        public static float capacitorDamageCoefficient = 10f; //30f
        public static float capacitorBlastRadius = 13f; //3f
        public static float capacitorCooldown = 20f; //20f
        public static BlastAttack.FalloffModel capacitorFalloff = BlastAttack.FalloffModel.SweetSpot;
        public static void ChangeCapacitor()
        {
            LoadEquipDef(nameof(RoR2Content.Equipment.Lightning)).cooldown = capacitorCooldown;
            IL.RoR2.EquipmentSlot.FireLightning += CapacitorNerf;
            IL.RoR2.Orbs.LightningStrikeOrb.OnArrival += CapacitorBuff;
            LanguageAPI.Add("EQUIPMENT_LIGHTNING_DESC", $"Call down a lightning strike on a targeted monster, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(capacitorDamageCoefficient)} damage</style> " +
                $"and <style=cIsDamage>stunning</style> nearby monsters in a large radius.");
        }

        public static void CapacitorNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul(),
                x => x.MatchStfld<RoR2.Orbs.GenericDamageOrb>("damageValue")
                );
            if (!b)
            {
                DebugBreakpoint(nameof(CapacitorNerf));
                return;
            }
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, capacitorDamageCoefficient);
        }

        public static void CapacitorBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchStfld<BlastAttack>("falloffModel")
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(CapacitorBuff), 1);
                return;
            }
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, (int)capacitorFalloff);

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStfld<BlastAttack>("radius")
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(CapacitorBuff), 2);
                return;
            }
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, capacitorBlastRadius);
        }
        #endregion
        #region the backup
        public static float backupCooldown = 60;//100
        public static void ChangeTheBackup()
        {
            LoadAsync<EquipmentDef>(RoR2_Base_DroneBackup.DroneBackup_asset, (equip) =>
            {
                equip.cooldown = 60;
            });
        }
        #endregion
        #region op fuel array buff
        public static void ChangeFuelArray()
        {
            On.EntityStates.QuestVolatileBattery.CountDown.OnEnter += FuelArrayUseEquipmentEffects;
        }

        public static void FuelArrayUseEquipmentEffects(On.EntityStates.QuestVolatileBattery.CountDown.orig_OnEnter orig, EntityStates.QuestVolatileBattery.CountDown self)
        {
            orig(self);
            CharacterBody body = self.networkedBodyAttachment.attachedBody;
            if (body && body.equipmentSlot && body.hasAuthority)
            {
                body.equipmentSlot.OnEquipmentExecuted();
            }
        }
        #endregion
        #region goobo jr
        public static Func<ItemIndex, bool> gooboItemCopyFilter = new Func<ItemIndex, bool>(Inventory.defaultItemCopyFilterDelegate);

        static float gummyLifetime = 30;//30
        static int gummyDamage = 0; //20
        static float gummyDamageMultiplier = 0.7f;
        static int gummyHealth = 20; //20
        static float gummyHealthMultiplier = 1f;
        public static void ChangeGoobo()
        {
            LoadAsync<GameObject>(RoR2_Base_Engi.EngiTurretMaster_prefab, (turretMaster) =>
            {
                MasterSummon turretMasterSummon = turretMaster.GetComponent<MasterSummon>();
                if (turretMasterSummon != null)
                    gooboItemCopyFilter = turretMasterSummon.inventoryItemCopyFilter;
            });
            LoadAsync<GameObject>(RoR2_DLC1_GummyClone.GummyCloneProjectile_prefab, (gummyCloneProjectilePrefab) =>
            {
                GummyCloneProjectile gummyCloneProjectile = gummyCloneProjectilePrefab.GetComponent<GummyCloneProjectile>();
                if (gummyCloneProjectile)
                {
                    gummyCloneProjectile.damageBoostCount = gummyDamage;
                    gummyCloneProjectile.hpBoostCount = gummyHealth;
                    gummyCloneProjectile.maxLifetime = gummyLifetime;
                }
            });

            IL.RoR2.Projectile.GummyCloneProjectile.SpawnGummyClone += GummyInheritItems;
            GetStatCoefficients += GummyStats;
            LanguageAPI.Add("EQUIPMENT_GUMMYCLONE_DESC",
                $"Spawn a gummy clone with <style=cIsDamage>all</style> of your items, that has " +
                $"<style=cIsDamage>{Tools.ConvertDecimal((1 + gummyDamage * 0.1f) * gummyDamageMultiplier)} damage</style> " +
                $"and <style=cIsHealing>{Tools.ConvertDecimal((1 + gummyDamage * 0.1f) * gummyDamageMultiplier)} health</style>. " +
                $"Expires in <style=cIsUtility>{gummyLifetime}</style> seconds.");
        }

        private static void GummyStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender?.equipmentSlot?.equipmentIndex == DLC1Content.Equipment.GummyClone.equipmentIndex)
            {
                args.healthMultAdd -= 1 - gummyHealthMultiplier;
                args.damageMultAdd -= 1 - gummyDamageMultiplier;
            }
        }

        private static void GummyInheritItems(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int n = 5;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchNewobj<DirectorSpawnRequest>(),
                x => x.MatchStloc(out n)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(GummyInheritItems));
                return;
            }

            c.Emit(OpCodes.Ldloc, n);
            c.EmitDelegate<Action<DirectorSpawnRequest>>((spawnRequest) =>
            {
                spawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(spawnRequest.onSpawnedServer,
                    new Action<SpawnCard.SpawnResult>(delegate (SpawnCard.SpawnResult spawnResult)
                    {
                        CopyInventoryFromOwner cico = spawnResult.spawnedInstance.AddComponent<CopyInventoryFromOwner>();
                        cico.inventoryItemCopyFilter = gooboItemCopyFilter;
                        cico.copyEquipment = false;
                    }));
            });
        }
        #endregion
        #region ocular hud
        public static float critHudDamageMul = 1;
        private static void ChangeOcularHud()
        {
            GetStatCoefficients += HudCritDamage;
            LanguageAPI.Add("EQUIPMENT_CRITONUSE_PICKUP", "Increased 'Critical Strike' damage. Gain 100% Critical Strike Chance for 8 seconds.");
            LanguageAPI.Add("EQUIPMENT_CRITONUSE_DESC",
                "<style=cIsHealth>Passively double Critical Strike Damage</style>. " +
                "On use, gain <style=cIsDamage>+100% Critical Strike Chance</style> for 8 seconds.");
        }


        private static void HudCritDamage(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.equipmentSlot)
            {
                if (sender.equipmentSlot.equipmentIndex == RoR2Content.Equipment.CritOnUse.equipmentIndex)
                    args.critDamageMultAdd += critHudDamageMul;
            }
        }
        #endregion
        #region meteor
        static BlastAttack.FalloffModel meteorFalloff = BlastAttack.FalloffModel.None;
        static void ChangeGlowingMeteorite()
        {
            IL.RoR2.MeteorStormController.DetonateMeteor += FixMeteorFalloff;
        }
        private static void FixMeteorFalloff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<BlastAttack>(nameof(BlastAttack.falloffModel))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(FixMeteorFalloff));
                return;
            }

            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, (int)meteorFalloff);
        }
        #endregion
        #region crowdfunder funny money
        public static float crunderFunnyMoneyProcChance = 10;
        static void ChangeCrowdfunder()
        {
            On.RoR2.GlobalEventManager.ProcessHitEnemy += CrunderFunnyMoney;
            LanguageAPI.Add("EQUIPMENT_GOLDGAT_PICKUP", "Toggle to fire. Costs gold per bullet. Passively has a chance to gain gold on hit.");
            LanguageAPI.Add("EQUIPMENT_GOLDGAT_DESC",
                $"Fires a continuous barrage that deals <style=cIsDamage>100% damage per bullet</style>. " +
                $"Costs $1 per bullet. Hitting enemies has a {crunderFunnyMoneyProcChance}% chance to refund the cost. Cost increases over time.");
        }

        private static void CrunderFunnyMoney(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (!damageInfo.rejected && damageInfo.procCoefficient > 0 && damageInfo.attacker)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    Inventory inv = attackerBody.inventory;
                    if (inv && inv.currentEquipmentIndex == RoR2Content.Equipment.GoldGat._equipmentIndex)
                    {
                        if (Util.CheckRoll(crunderFunnyMoneyProcChance * damageInfo.procCoefficient, attackerBody.master))
                        {
                            uint goldAmount = (uint)((float)GoldGatFire.baseMoneyCostPerBullet *
                                (1f + (TeamManager.instance.GetTeamLevel(attackerBody.master.teamIndex) - 1f) * 0.25f));
                            GoldOrb goldOrb = new GoldOrb();
                            goldOrb.origin = damageInfo.position;
                            goldOrb.target = attackerBody.mainHurtBox;
                            goldOrb.goldAmount = goldAmount;
                            OrbManager.instance.AddOrb(goldOrb);
                            //EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact"), damageInfo.position, Vector3.up, true);
                        }
                    }
                }
            }
            orig(self, damageInfo, victim);
        }
        #endregion

        #region blacklists
        public static EquipmentDef[] scavBlacklistedEquips = new EquipmentDef[]
        {
            RoR2Content.Equipment.PassiveHealing,
            RoR2Content.Equipment.Fruit,
            RoR2Content.Equipment.LifestealOnHit
        };
        static void ChangeEnigmaBlacklists()
        {
            ChangeEquipmentEnigma(nameof(RoR2Content.Equipment.CrippleWard), true);
            ChangeEquipmentEnigma(nameof(RoR2Content.Equipment.Jetpack), true);
        }
        static void ChangeEquipmentBlacklists()
        {
            On.RoR2.Inventory.SetEquipmentIndex_EquipmentIndex_bool += BlacklistEquipmentFromScavengers;
        }

        private static void BlacklistEquipmentFromScavengers(On.RoR2.Inventory.orig_SetEquipmentIndex_EquipmentIndex_bool orig, Inventory self, EquipmentIndex newEquipmentIndex, bool isRemovingEquipment)
        {
            CharacterBody body = self.gameObject.GetComponent<CharacterBody>();
            if (body != null && body.bodyIndex == BodyCatalog.FindBodyIndex("ScavBody"))
            {
                bool flag = false;
                foreach (EquipmentDef def in scavBlacklistedEquips)
                {
                    if (newEquipmentIndex == def.equipmentIndex)
                    {
                        flag = true;
                    }
                }

                if (flag)
                {
                    Debug.Log("A scavenger almost spawned with a healing equipment! But they didnt ;)");
                    newEquipmentIndex = EquipmentIndex.None;
                }
            }

            orig(self, newEquipmentIndex, isRemovingEquipment);
        }
        #endregion
        #region helfire
        private static void ChangeTincture()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += MakeTinctureIgnoreArmor;
        }

        private static void MakeTinctureIgnoreArmor(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (damageInfo.dotIndex.HasFlag(DotController.DotIndex.Helfire))
            {
                damageInfo.damageType |= DamageType.BypassArmor;
            }
            orig(self, damageInfo);
        }
        #endregion



        #region blast shower
        private static int blastShowerBuffCount = 3; //0
        private static void ChangeShower()
        {
            On.RoR2.EquipmentSlot.FireCleanse += BlastShowerProtectionBuffs;
        }

        private static bool BlastShowerProtectionBuffs(On.RoR2.EquipmentSlot.orig_FireCleanse orig, EquipmentSlot self)
        {
            if (orig(self))
            {
                for (int i = 0; i < blastShowerBuffCount; i++)
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
