using BepInEx;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.RoR2.Items;
using R2API;
using RiskierRain.Changes.Components;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static RoR2.HoldoutZoneController;

namespace RiskierRain
{
    partial class RiskierRainPlugin
    {
        internal static void AIBlacklistSingleItem(string name)
        {
            ItemDef itemDef = LoadItemDef(name);
            List<ItemTag> itemTags = new List<ItemTag>(itemDef.tags);
            itemTags.Add(ItemTag.AIBlacklist);

            itemDef.tags = itemTags.ToArray();
        }
        #region blacklist
        void HealingItemBlacklist()
        {
            AIBlacklistSingleItem(nameof(RoR2Content.Items.BarrierOnKill));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.BarrierOnOverHeal));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.NovaOnHeal));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.Mushroom));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.Medkit));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.Tooth));
        }
        #endregion

        #region stuns
        public static float capacitorDamageCoefficient = 10f;
        public static float capacitorBlastRadius = 13f;
        public static float capacitorCooldown = 20f; //20
        void StunChanges()
        {
            LoadEquipDef(nameof(RoR2Content.Equipment.Lightning)).cooldown = capacitorCooldown;
            IL.RoR2.EquipmentSlot.FireLightning += CapacitorNerf;
            IL.RoR2.Orbs.LightningStrikeOrb.OnArrival += CapacitorBuff;
            LanguageAPI.Add("EQUIPMENT_LIGHTNING_DESC", $"Call down a lightning strike on a targeted monster, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(capacitorDamageCoefficient)} damage</style> " +
                $"and <style=cIsDamage>stunning</style> nearby monsters in a large radius.");
        }

        private void CapacitorNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul(),
                x => x.MatchStfld<RoR2.Orbs.GenericDamageOrb>("damageValue")
                );
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, capacitorDamageCoefficient);
        }

        private void CapacitorBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchStfld<BlastAttack>("falloffModel")
                );
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, (int)BlastAttack.FalloffModel.SweetSpot);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStfld<BlastAttack>("radius")
                );
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, capacitorBlastRadius);
        }
        #endregion

        #region glasses
        float glassesNewCritChance = 7f;
        private void NerfCritGlasses()
        {
            IL.RoR2.CharacterBody.RecalculateStats += this.GlassesNerf;
            LanguageAPI.Add("ITEM_CRITGLASSES_DESC",
                $"Your attacks have a <style=cIsDamage>{glassesNewCritChance}%</style> " +
                $"<style=cStack>(+{glassesNewCritChance}% per stack)</style> chance to " +
                $"'<style=cIsDamage>Critically Strike</style>', dealing <style=cIsDamage>double damage</style>.");
        }
        private void GlassesNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "CritGlasses"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(countLoc),
                x => x.MatchConvR4()
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, glassesNewCritChance);
        }
        #endregion

        #region pauldron
        private float pauldronDamageMultiplier = 1f;
        private float pauldronAspdMultiplier = 0.5f;

        private void EditWarCry()
        {
            GetStatCoefficients += this.WarCryDamage;
            IL.RoR2.CharacterBody.RecalculateStats += RemovePauldronAttackSpeed;
            LanguageAPI.Add("ITEM_WARCRYONMULTIKILL_DESC",
                $"<style=cIsDamage>Killing 3 enemies</style> within <style=cIsDamage>1</style> second " +
                $"sends you into a <style=cIsDamage>frenzy</style> for <style=cIsDamage>6s</style> <style=cStack>(+4s per stack)</style>. " +
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>50%</style>, " +
                $"<style=cIsDamage>damage</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronDamageMultiplier)}</style>, " +
                $"and <style=cIsDamage>attack speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronAspdMultiplier)}</style>.");
            LanguageAPI.Add("EQUIPMENT_TEAMWARCRY_DESC",
                $"All allies enter a <style=cIsDamage>frenzy</style> for <style=cIsDamage>7s</style>. " +
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>50%</style>, " +
                $"<style=cIsDamage>damage</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronDamageMultiplier)}</style>, " +
                $"and <style=cIsDamage>attack speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronAspdMultiplier)}</style>.");
        }

        public void WarCryDamage(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(RoR2Content.Buffs.WarCryBuff) || sender.HasBuff(RoR2Content.Buffs.TeamWarCry))
                args.damageMultAdd += pauldronDamageMultiplier;
        }

        private void RemovePauldronAttackSpeed(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int attackSpeedMultiplierLocation = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld("RoR2.CharacterBody", "baseAttackSpeed"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld("RoR2.CharacterBody", "levelAttackSpeed")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out attackSpeedMultiplierLocation),
                x => x.MatchLdloc(attackSpeedMultiplierLocation)
                );


            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "WarCryBuff"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.HasBuff))
                );
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(attackSpeedMultiplierLocation),
                x => x.MatchLdcR4(1f)
                );
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, pauldronAspdMultiplier);

            //Debug.Log(il.ToString());
        }
        #endregion

        #region meteor
        BlastAttack.FalloffModel falloffModel = BlastAttack.FalloffModel.None;
        void FixMeteorFalloff()
        {
            IL.RoR2.MeteorStormController.DetonateMeteor += MeteorFix;
        }
        private void MeteorFix(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchStfld<BlastAttack>(nameof(BlastAttack.falloffModel))
                );

            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, (int)falloffModel);
        }
        #endregion

        #region justice
        float justiceMinDamageCoeff = 8f;
        void BuffJustice()
        {
            On.RoR2.GlobalEventManager.ProcessHitEnemy += this.JusticeBuff;
            LanguageAPI.Add("ITEM_ARMORREDUCTIONONHIT_PICKUP",
                "Reduce the armor of enemies after repeatedly striking them or on massive hits.");
            LanguageAPI.Add("ITEM_ARMORREDUCTIONONHIT_DESC",
                $"After hitting an enemy <style=cIsDamage>5</style> times, or dealing " +
                $"<style=cIsDamage>more than {Tools.ConvertDecimal(justiceMinDamageCoeff)} damage</style> to them in a single hit, " +
                $"reduce their <style=cIsDamage>armor</style> by <style=cIsDamage>60</style> " +
                $"for <style=cIsDamage>8</style><style=cStack> (+8 per stack)</style> seconds.");
        }
        private void JusticeBuff(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            if (damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody component = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody component2 = victim.GetComponent<CharacterBody>();
                if (component)
                {
                    CharacterMaster master = component.master;
                    if (master)
                    {
                        int justiceCount = 0;
                        Inventory inventory = master.inventory;
                        if (inventory)
                        {
                            justiceCount = inventory.GetItemCount(RoR2Content.Items.ArmorReductionOnHit);
                        }

                        if (component2 != null && justiceCount > 0)
                        {
                            BuffDef buffIndex = RoR2Content.Buffs.PulverizeBuildup;
                            BuffDef buffType = RoR2Content.Buffs.Pulverized;
                            if (damageInfo.damage / component.damage >= justiceMinDamageCoeff && !component2.HasBuff(buffType))
                            {
                                component2.ClearTimedBuffs(buffIndex);
                                component2.AddTimedBuff(buffType, 8f * (float)justiceCount);
                                ProcChainMask procChainMask2 = damageInfo.procChainMask;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region ResonanceDisc
        void NerfResDisc()
        {
            On.RoR2.LaserTurbineController.Awake += ResDiscSpinFix;
        }

        private void ResDiscSpinFix(On.RoR2.LaserTurbineController.orig_Awake orig, RoR2.LaserTurbineController self)
        {
            //self.spinPerKill = resdiscSpinPerKill;
            //self.spinDecayRate = resdiscDecayRate;
            orig(self);
        }
        #endregion

        #region infusion
        public static float newInfusionBaseHealth = 40;

        void FuckingFixInfusion()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += InfusionBuff;
            LanguageAPI.Add("ITEM_INFUSION_PICKUP",
            "Killing an enemy permanently increases your base health.");
            LanguageAPI.Add("ITEM_INFUSION_DESC",
                $"Killing an enemy increases your <style=cIsHealing>base health permanently</style> by <style=cIsHealing>1</style> <style=cStack>(+1 per stack)</style>, " +
                $"up to a <style=cIsHealing>maximum</style> of <style=cIsHealing>{newInfusionBaseHealth} <style=cStack>(+{newInfusionBaseHealth} per stack)</style> health</style>.");
        }

        private void InfusionBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int attackerBodyLoc = 15; //really need to be getting this through IL but i dont care tbh
            int countLoc = 43;
            int capLoc = 63;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Infusion"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out countLoc),
                x => x.MatchLdcI4(out _),
                x => x.MatchMul(),
                x => x.MatchStloc(out capLoc)
                );
            c.Index--;

            c.Emit(OpCodes.Ldloc, countLoc);
            c.Emit(OpCodes.Ldloc, attackerBodyLoc); //body loc
            c.EmitDelegate<Func<int, int, RoR2.CharacterBody, int>>((currentInfusionCap, infusionCount, body) =>
            {
                float newInfusionCap = 100 * infusionCount;

                if (body != null)
                {
                    float levelBonus = 1 + 0.3f * (body.level - 1);

                    newInfusionCap = newInfusionBaseHealth * levelBonus * infusionCount;
                }

                return (int)newInfusionCap;
            });
        }
        #endregion

        #region minion on kill
        void MakeMinionsInheritOnKillEffects()
        {
            On.RoR2.Inventory.GetItemCount_ItemIndex += GetItemCountInheritOnKills;
        }

        private int GetItemCountInheritOnKills(On.RoR2.Inventory.orig_GetItemCount_ItemIndex orig, Inventory self, ItemIndex itemIndex)
        {
            int itemCount = orig(self, itemIndex);
            if (ItemCatalog.GetItemDef(itemIndex).ContainsTag(ItemTag.OnKillEffect) && itemCount == 0)
            {
                CharacterMaster master = self.GetComponent<CharacterMaster>();
                if (master != null)
                {
                    MinionOwnership mo = master.minionOwnership;
                    CharacterMaster ownerMaster = mo.ownerMaster;
                    if (ownerMaster)
                    {
                        int masterItemCount = ownerMaster.inventory.GetItemCount(itemIndex);
                        itemCount = masterItemCount;
                    }
                }
            }
            return itemCount;
        }
        #endregion

        #region weeping fungus
        public float wungusRegenBase = 1;
        public float wungusRegenStack = 1;
        public void ReworkWeepingFungus()
        {
            GetStatCoefficients += WungusRegen;
            On.RoR2.MushroomVoidBehavior.FixedUpdate += FuckWungusHeal;

            LanguageAPI.Add("ITEM_MUSHROOMVOID_PICKUP", "Regenerate health while sprinting. <style=cIsVoid>Corrupts all Bustling Fungi</style>.");
            LanguageAPI.Add("ITEM_MUSHROOMVOID_DESC",
                $"Increases <style=cIsHealing>base health regeneration</style> " +
                $"by <style=cIsHealing>+{wungusRegenBase} hp/s</style> " +
                $"<style=cStack>(+{wungusRegenStack} hp/s per stack)</style> <style=cIsUtility>while sprinting</style>. " +
                $"<style=cIsVoid>Corrupts all Bustling Fungi</style>.");
        }

        private void WungusRegen(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(DLC1Content.Buffs.MushroomVoidActive))
            {
                if (sender.inventory)
                {
                    int wungusCount = sender.inventory.GetItemCount(DLC1Content.Items.MushroomVoid);
                    args.baseRegenAdd += wungusRegenBase + wungusRegenStack * (wungusCount - 1);
                }
            }
        }

        private void FuckWungusHeal(On.RoR2.MushroomVoidBehavior.orig_FixedUpdate orig, MushroomVoidBehavior self)
        {
            self.healTimer = 0;
            orig(self);
            self.healTimer = 0;
        }
        #endregion

        #region polylute
        public float luteDamageCoefficient = 0.4f; //0.6f; //ukulele 0.8f
        public void ReworkPolylute()
        {
            if (ucrLoaded)
                return;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += PolyluteDamage;

            LanguageAPI.Add("ITEM_CHAINLIGHTNINGVOID_DESC",
                $"<style=cIsDamage>25%</style> chance " +
                $"to fire <style=cIsDamage>lightning</style> " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(luteDamageCoefficient)}</style> TOTAL damage " +
                $"up to <style=cIsDamage>3</style> <style=cStack>(+3 per stack)</style> " +
                $"times. <style=cIsVoid>Corrupts all Ukuleles</style>.");
        }

        private void PolyluteDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int luteLoc = 14;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "ChainLightningVoid"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out luteLoc)
                );
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out luteLoc),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _)
                );

            int dmgLoc = 14;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out dmgLoc)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, luteDamageCoefficient);
        }
        #endregion

        #region shuriken
        GameObject shurikenProjectilePrefab;
        public float shurikenBaseDamage = 0.8f; //3f + 1f/s
        public float shurikenProcCoefficient = 1.5f;
        public void ReworkShuriken()
        {
            shurikenProjectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/ShurikenProjectile");
            if (shurikenProjectilePrefab != null)
            {
                ProjectileController pc = shurikenProjectilePrefab.GetComponent<ProjectileController>();
                if (pc)
                {
                    pc.procCoefficient = shurikenProcCoefficient;
                }
                ProjectileDamage pd = shurikenProjectilePrefab.GetComponent<ProjectileDamage>();
                if (pd)
                {
                    pd.damageType |= DamageType.BleedOnHit;
                }
            }

            On.RoR2.PrimarySkillShurikenBehavior.FireShuriken += ModifyShurikenAttack;

            LanguageAPI.Add("ITEM_PRIMARYSKILLSHURIKEN_PICKUP",
                "Activating your Primary skill also throws a shuriken that bleeds enemies. Recharges over time.");
            LanguageAPI.Add("ITEM_PRIMARYSKILLSHURIKEN_DESC",
                $"Activating your <style=cIsUtility>Primary skill</style> " +
                $"also throws a <style=cIsDamage>shuriken</style> that " +
                $"deals <style=cIsDamage>{Tools.ConvertDecimal(shurikenBaseDamage)}</style> base damage " +
                $"and <style=cIsDamage>bleeds</style> enemies struck for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(shurikenProcCoefficient * 2.4f)}</style> base damage. " +
                $"You can hold up to <style=cIsUtility>3</style> " +
                $"<style=cStack>(+1 per stack)</style> " +
                $"<style=cIsDamage>shurikens</style> which all reload over <style=cIsUtility>10</style> seconds.");
        }

        private void ModifyShurikenAttack(On.RoR2.PrimarySkillShurikenBehavior.orig_FireShuriken orig, PrimarySkillShurikenBehavior self)
        {
            Ray aimRay = self.GetAimRay();
            ProjectileManager.instance.FireProjectile(
                self.projectilePrefab, aimRay.origin,
                Util.QuaternionSafeLookRotation(aimRay.direction) * self.GetRandomRollPitch(),
                self.gameObject, self.body.damage * (shurikenBaseDamage), 0f,
                Util.CheckRoll(self.body.crit, self.body.master), DamageColorIndex.Item, null, -1f);
        }
        #endregion


        #region lepton daisy
        public float daisyRadiusMultiplier = 1.1f; //increase by 10%
        public void BuffDaisy()
        {
            On.RoR2.HoldoutZoneController.OnEnable += DaisyRadiusIncrease;
        }

        private void DaisyRadiusIncrease(On.RoR2.HoldoutZoneController.orig_OnEnable orig, HoldoutZoneController self)
        {
            orig(self);
            int itemCount = Util.GetItemCountForTeam(TeamIndex.Player, RoR2Content.Items.TPHealingNova.itemIndex, false);
            if (itemCount > 0)
            {
                self.baseRadius *= daisyRadiusMultiplier;
            }
        }


        #endregion

        #region lost seers lenses
        void LostSeersFix()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += FixLostSeersDamageImmunity;
        }

        private void FixLostSeersDamageImmunity(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "CritGlassesVoid"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, HealthComponent, int>>((lensCount, hc) => {
                CharacterBody cb = hc.body;
                if (cb.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToVoidDeath))
                {
                    return 0;
                }
                return lensCount;
            });
        }
        #endregion

        #region fuel cell
        public const float fuelCellCooldownMultiplier = 0.67f;
        public static string fuelCellEquipCdr = Tools.ConvertDecimal(1 - fuelCellCooldownMultiplier);
        public static int fuelCellStock = 2;
        void ReworkFuelCell()
        {
            RetierItem(nameof(RoR2Content.Items.EquipmentMagazine), ItemTier.Tier3);
            RetierItem(nameof(DLC1Content.Items.EquipmentMagazineVoid), ItemTier.VoidTier3);
            IL.RoR2.Inventory.CalculateEquipmentCooldownScale += FuelCellCdr;
            IL.RoR2.Inventory.GetEquipmentSlotMaxCharges += FuelCellStock;
            IL.RoR2.Inventory.UpdateEquipment += FuelCellStock;

            LanguageAPI.Add("ITEM_EQUIPMENTMAGAZINE_DESC",
                $"Hold {fuelCellStock} <style=cIsUtility>additional equipment charges</style> <style=cStack>(+{fuelCellStock} per stack)</style>. " +
                $"<style=cIsUtility>Reduce equipment cooldown</style> by " +
                $"<style=cIsUtility>{fuelCellEquipCdr}</style> <style=cStack>(+{fuelCellEquipCdr} per stack)</style>.");
        }

        private void FuelCellStock(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "EquipmentMagazine"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Ldc_I4, fuelCellStock);
            c.Emit(OpCodes.Mul);
        }

        private void FuelCellCdr(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int fuelCell = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "EquipmentMagazine")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out fuelCell)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdloc(fuelCell)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, fuelCellCooldownMultiplier);
        }
        #endregion

        #region bottled chaos

        public const float chaosCooldownMultiplier = 0.67f;
        public static string chaosEquipCdr = Tools.ConvertDecimal(1 - chaosCooldownMultiplier);
        void BuffBottledChaos()
        {
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += BottledChaosCdr;
            LanguageAPI.Add("ITEM_RANDOMEQUIPMENTTRIGGER_DESC", 
                $"Trigger a <style=cIsDamage>random equipment</style> effect <style=cIsDamage>1</style> <style=cStack>(+1 per stack)</style> time(s). " +
                $"<style=cIsUtility>Reduce equipment cooldown</style> by " +
                $"<style=cIsUtility>{chaosEquipCdr}</style> <style=cStack>(+{chaosEquipCdr} per stack)</style>.");
        }

        private float BottledChaosCdr(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            float scale = orig(self);
            int chaosCount = self.GetItemCount(DLC1Content.Items.RandomEquipmentTrigger);
            if (chaosCount > 0)
                scale *= Mathf.Pow(chaosCooldownMultiplier, chaosCount);
            return scale;
        }
        #endregion

        #region sticky bomb
        public static float stickyDamageCoeffBase = 3.2f; //3.2 is 8 stacks to beat atg, 4.0 is 6 stacks
        public static float stickyDamageCoeffStack = 0.4f;
        void ReworkStickyBomb()
        {
            RetierItem(nameof(RoR2Content.Items.StickyBomb), ItemTier.Tier2);

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += StickyBombRework;
            LanguageAPI.Add("ITEM_STICKYBOMB_DESC",
                $"<style=cIsDamage>5%</style> <style=cStack>(+5% per stack)</style> chance " +
                $"on hit to attach a <style=cIsDamage>bomb</style> to an enemy, detonating for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(stickyDamageCoeffBase)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(stickyDamageCoeffStack)} per stack)</style> TOTAL damage.");

            GameObject stickyPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/StickyBomb");
            ProjectileImpactExplosion pie = stickyPrefab.GetComponent<ProjectileImpactExplosion>();
        }

        private void StickyBombRework(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int stickyLoc = 14;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "StickyBomb"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out stickyLoc)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnHitProcDamage))
                );
            c.Emit(OpCodes.Ldloc, stickyLoc);
            c.EmitDelegate<Func<float, int, float>>((damageCoefficient, itemCount) =>
            {
                float damageOut = stickyDamageCoeffBase + (stickyDamageCoeffStack * (itemCount - 1));
                return damageOut;
            });
        }
    }
    #endregion
}