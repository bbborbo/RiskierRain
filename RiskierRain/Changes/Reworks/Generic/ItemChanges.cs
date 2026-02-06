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
        float glassesNewCritChance = 10f;
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
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
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
            return;
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
                            justiceCount = inventory.GetItemCountEffective(RoR2Content.Items.ArmorReductionOnHit);
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
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
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
            On.RoR2.Inventory.GetItemCountEffective_ItemIndex += GetItemCountEffectiveInheritOnKills;
        }

        private int GetItemCountEffectiveInheritOnKills(On.RoR2.Inventory.orig_GetItemCountEffective_ItemIndex orig, Inventory self, ItemIndex itemIndex)
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
                        int masterItemCount = ownerMaster.inventory.GetItemCountEffective(itemIndex);
                        itemCount = masterItemCount;
                    }
                }
            }
            return itemCount;
        }
        #endregion

        #region weeping fungus
        public float wungusRegenBase = 1.5f;
        public float wungusRegenStack = 1.5f;
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
                    int wungusCount = sender.inventory.GetItemCountEffective(DLC1Content.Items.MushroomVoid);
                    args.baseRegenAdd += wungusRegenBase + wungusRegenStack * (wungusCount - 1) * (1 + sender.level * 0.2f);
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
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
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
        public float shurikenBaseDamage = 0.8f; //3f
        public float shurikenStackDamage = 0.0f; //1f
        public float shurikenProcCoefficient = 2f;
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
                    pd.damageType.damageSource = DamageSource.Primary;
                }
            }

            //On.RoR2.PrimarySkillShurikenBehavior.FireShuriken += ModifyShurikenAttack;
            IL.RoR2.PrimarySkillShurikenBehavior.FireShuriken += ModifyFireShuriken;

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

        private void ModifyFireShuriken(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_damage"),
                x => x.MatchLdcR4(out _),
                x => x.MatchLdcR4(out _)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ModifyFireShuriken));
                return;
            }
            c.Index++;
            c.Next.Operand = shurikenBaseDamage - shurikenStackDamage;
            c.Index++;
            c.Next.Operand = shurikenStackDamage;
        }

        private void ModifyShurikenAttack(On.RoR2.PrimarySkillShurikenBehavior.orig_FireShuriken orig, PrimarySkillShurikenBehavior self)
        {
            Ray aimRay = self.GetAimRay();
            ProjectileManager.instance.FireProjectileWithoutDamageType(
                self.projectilePrefab, aimRay.origin,
                Util.QuaternionSafeLookRotation(aimRay.direction) * self.GetRandomRollPitch(),
                self.gameObject, self.body.damage * (shurikenBaseDamage), 0f,
                Util.CheckRoll(self.body.crit, self.body.master), DamageColorIndex.Item, null, -1f);
        }
        #endregion


        #region lepton daisy
        public float daisyRadiusMultiplier = 1.15f; //increase by 10%
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
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
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
            RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_EquipmentMagazine.EquipmentMagazine_asset, ItemTier.Tier3, FixFuelCellIcon);
            RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_EquipmentMagazineVoid.EquipmentMagazineVoid_asset, ItemTier.VoidTier3);
            void FixFuelCellIcon(ItemDef itemDef)
            {
                Sprite sprite = CoreModules.Assets.retierAssetBundle.LoadAsset<Sprite>("Assets/Icons/Fuel_Cell.png");
                if (sprite)
                    itemDef.pickupIconSprite = sprite;
            }

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
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
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
            int chaosCount = self.GetItemCountEffective(DLC1Content.Items.RandomEquipmentTrigger);
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
            RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_StickyBomb.StickyBomb_asset, ItemTier.Tier2);

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
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
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
        #endregion

        #region sale star

        public static void SaleStarChanges()
        {
            LanguageAPI.Add("ITEM_LOWERPRICEDCHESTS_PICKUP", "First chest bought yields an additional reward. Usable once per stage.");
            LanguageAPI.Add("ITEM_LOWERPRICEDCHESTS_DESC", 
                $"Gain <style=cIsUtility>1</style> <style=cStack>(+1 per stack)</style> extra item on the first chest opened per stage.");

            IL.RoR2.PurchaseInteraction.OnInteractionBegin += SaleStarOnInteraction;
            IL.RoR2.ChestBehavior.BaseItemDrop += SaleStarItemDrop;
        }

        private static void SaleStarItemDrop(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int droppedCountloc = 3;
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<ChestBehavior>(nameof(ChestBehavior.Roll)),
                x => x.MatchLdloc(out droppedCountloc)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(SaleStarItemDrop));
                return;
            }

            c.Emit(OpCodes.Ldloc, droppedCountloc);
            c.EmitDelegate<Func<ChestBehavior, int, ChestBehavior>>((chest, droppedIndex) =>
            {
                //max drop count is used to tell how many items the chest would have dropped
                if(droppedIndex + 1 /*next drop*/ >= chest.maxDropCount && chest.maxDropCount > chest.dropCount)
                {
                    chest.maxDropCount = chest.dropCount;
                    chest.dropTable = Addressables.LoadAssetAsync<PickupDropTable>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.dtShrineChance_asset).WaitForCompletion();
                }    
                return chest;
            });
        }

        private static void SaleStarOnInteraction(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int totalTransformedLoc = 13;
            ILLabel skipLabel = c.DefineLabel();
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<PurchaseInteraction>(nameof(PurchaseInteraction.saleStarCompatible)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt("RoR2.Inventory/ItemTransformation/TryTransformResult", "get_totalTransformed"),
                x => x.MatchStloc(out totalTransformedLoc)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 1);
                return;
            }

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchStloc(out _),
                x => x.MatchBr(out skipLabel)) 
                && c.TryGotoPrev(MoveType.Before, 
                x => x.MatchLdloc(totalTransformedLoc)
                );
            if (b2)
            {
                c.Emit(OpCodes.Br, skipLabel);
            }
            else
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 2);
            }

            bool b3 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<ChestBehavior>(nameof(ChestBehavior.dropCount))
                );
            if (!b3)
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 3);
                return;
            }

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldloc, totalTransformedLoc);
            c.EmitDelegate<Func<ChestBehavior, int, int>>((chest, totalTransformed) =>
            {
                chest.maxDropCount = chest.dropCount;
                return chest.dropCount + totalTransformed;
            });


            bool b4 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<RouletteChestController>(nameof(RouletteChestController.dropCount))
                );
            if (!b4)
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 4);
                return;
            }

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldloc, totalTransformedLoc);
            c.EmitDelegate<Func<RouletteChestController, int, int>>((chest, totalTransformed) =>
            {
                return chest.dropCount + totalTransformed;
            });
        }
        #endregion

        #region Chance Doll
        public static int chanceDollChanceBase = 30;
        public static int chanceDollChanceStack = 30;
        public static void ChanceDollChanges()
        {
            LoadAsync<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_ExtraShrineItem.dtChanceDoll_asset, (dropTable) =>
            {
                dropTable.tier2Weight = 0.65f;//0.79f
                dropTable.tier3Weight = 0.30f;//0.20f
                dropTable.bossWeight = 0.05f;//0.01f
            });
            IL.RoR2.ShrineChanceBehavior.AddShrineStack += ChanceDollActivationChance;
            Stage.onServerStageBegin += ChanceDollShrineSpawn;

            LanguageAPI.Add("ITEM_EXTRASHRINEITEM_PICKUP", "Gain a chance for higher rarity items from Shrines of Chance.");
            LanguageAPI.Add("ITEM_EXTRASHRINEITEM_DESC", 
                $"On Shrine of Chance success, " +
                $"<style=cIsUtility>{chanceDollChanceBase}%</style> " +
                $"<style=cStack>(+{chanceDollChanceStack}% per stack)</style> " +
                $"chance to get higher rarity items.");
        }

        private static void ChanceDollActivationChance(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int dollCountLoc = 5;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", nameof(DLC2Content.Items.ExtraShrineItem)),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out dollCountLoc))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchLdloc(dollCountLoc),
                x => x.MatchLdcI4(out _)
                );

            if (!b)
            {
                DebugBreakpoint(nameof(ChanceDollActivationChance), 1);
                return;
            }
            c.Next.Operand = chanceDollChanceBase;
            c.Index += 2;
            c.Next.Operand = chanceDollChanceStack;

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchAdd(),
                x => x.MatchConvR4()
                );
            if(!b1)
            {
                DebugBreakpoint(nameof(ChanceDollActivationChance), 2);
                return;
            }
            c.EmitDelegate<Func<float, float>>(Util.ConvertAmplificationPercentageIntoReductionPercentage);
        }

        private static void ChanceDollShrineSpawn(Stage currentStage)
        {
            if (!Run.instance)
                return;

            SceneDef currentScene = currentStage.sceneDef;
            if (currentScene.preventStageAdvanceCounter
                || currentScene.sceneType == SceneType.Intermission
                || currentScene.sceneType == SceneType.Cutscene
                || currentScene.sceneType == SceneType.UntimedStage
                || currentScene.sceneType == SceneType.Junk)
                return;

            int itemCount = Util.GetItemCountForTeam(TeamIndex.Player, DLC2Content.Items.ExtraShrineItem.itemIndex, true, true);
            if (itemCount <= 0)
                return;

            Xoroshiro128Plus rng = Run.instance.stageRng;
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode =
                    SceneInfo.instance && SceneInfo.instance.approximateMapBoundMesh
                        ? DirectorPlacementRule.PlacementMode.RandomNormalized
                        : DirectorPlacementRule.PlacementMode.Random
            };

            string path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.iscShrineChance_asset;//printerSpawncardPaths.Evaluate(rng.nextNormalizedFloat);
            if (currentScene.baseSceneName == "goolake"
                || currentScene.baseSceneName == "ironalluvium")
                path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.iscShrineChanceSandy_asset;
            else if (currentScene.baseSceneName == "snowyforest"
                || currentScene.baseSceneName == "nest"
                || currentScene.baseSceneName == "frozenwall")
                path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.iscShrineChanceSnowy_asset;
            InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

            GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
            Debug.Log($"(chance doll) chance shrine spawned at " +
                $"[{pillarObject.transform.position.x}, {pillarObject.transform.position.y}, {pillarObject.transform.position.z}] ");
        }
        #endregion

        #region warped echo

        public static float warpedEchoDamageReduction = 0.3f;
        public static void WarpedEchoChanges()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += WarpedEchoDamageReduction;

            LanguageAPI.Add("ITEM_DELAYEDDAMAGE_DESC",
                $"The next source of damage is <style=cIsHealing>reduced</style> by " +
                $"<style=cIsHealing>{warpedEchoDamageReduction * 100}%</style> and " +
                $"<style=cIsHealing>spread</style> into <style=cIsUtility>3 <style=cStack>(+1 per stack)</style> hits</style>. " +
                $"Recharges every <style=cIsUtility>15s</style>.");
        }

        private static void WarpedEchoDamageReduction(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", nameof(DLC2Content.Items.DelayedDamage)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(0.9f)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(WarpedEchoDamageReduction));
                return;
            }

            c.Next.Operand = 1 - warpedEchoDamageReduction;
        }
        #endregion

        #region elusive antlers
        public static float elusiveAntlersPickupDuration = 24f;//60f
        public static float elusiveAntlersBuffDuration = 18f;//12f
        public static float elusiveAntlersPickupInterval = 12f;//10f
        public static float elusiveAntlersPickupIntervalReductionStack = 0.1f;//0.1f
        public static float elusiveAntlersMoveSpeedPerBuff = 0.06f; //0.12f
        public static float elusiveAntlersFreeMovespeedBase = 0.06f; //0f
        public static float elusiveAntlersFreeMovespeedStack = 0.06f; //0f
        public static void ElusiveAntlersChanges()
        {
            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_SpeedBoostPickup.ElusiveAntlersPickup_prefab, (pickupObject) =>
            {
                if(pickupObject.TryGetComponent(out BeginRapidlyActivatingAndDeactivating flasher))
                {
                    flasher.delayBeforeBeginningBlinking = elusiveAntlersPickupDuration - 2;
                }
                //destroying on timer is manually implemented in ElusiveAntlersPickup
                //if(pickupObject.TryGetComponent(out DestroyOnTimer timer))
                //{
                //    timer.duration = elusiveAntlersPickupDuration;
                //}
            });
            On.RoR2.ElusiveAntlersPickup.Start += ElusiveAntlersPickupStats;
            IL.RoR2.CharacterBody.RecalculateStats += ElusiveAntlersBuffMoveSpeed;
            IL.RoR2.ElusiveAntlersBehavior.FixedUpdate += ElusiveAntlersPickupInterval;
            GetStatCoefficients += ElusiveAntlersBaseMovespeed;

            LanguageAPI.Add("ITEM_SPEEDBOOSTPICKUP_DESC",
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>{elusiveAntlersFreeMovespeedBase.AsPercent()}</style> " +
                $"<style=cStack>(+{elusiveAntlersFreeMovespeedStack.AsPercent()} per stack)</style>. " +
                $"Every <style=cIsUtility>{elusiveAntlersPickupInterval}s</style> " +
                $"<style=cStack>(-{elusiveAntlersPickupIntervalReductionStack.AsPercent()} per stack)</style>, " +
                $"spawn an orb of energy nearby granting " +
                $"<style=cIsUtility>+{elusiveAntlersMoveSpeedPerBuff.AsPercent()} movement speed</style> up to " +
                $"<style=cIsUtility>3 <style=cStack>(+3 per stack)</style> " +
                $"times</style> for <style=cIsUtility>{elusiveAntlersBuffDuration}s</style>.");
        }

        private static void ElusiveAntlersPickupInterval(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<ElusiveAntlersBehavior>(nameof(ElusiveAntlersBehavior.spawnTimer))) 
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<ElusiveAntlersBehavior>(nameof(ElusiveAntlersBehavior.spawnTimer))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(ElusiveAntlersPickupInterval));
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, ElusiveAntlersBehavior, float>>((timerOld, self) =>
            {
                float interval = elusiveAntlersPickupInterval;
                if (self.body.inventory)
                {
                    int itemCount = self.body.inventory.GetItemCountEffective(DLC2Content.Items.SpeedBoostPickup);
                    if(itemCount > 1)
                    {
                        interval *= Mathf.Pow(1f - elusiveAntlersPickupIntervalReductionStack, itemCount - 1f);
                    }
                }
                return interval;
            });
        }

        private static void ElusiveAntlersPickupStats(On.RoR2.ElusiveAntlersPickup.orig_Start orig, ElusiveAntlersPickup self)
        {
            self.despawnMinAge = elusiveAntlersPickupDuration;
            self.shardPickupBuffTimeSeconds = elusiveAntlersBuffDuration;
            orig(self);
        }

        private static void ElusiveAntlersBuffMoveSpeed(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Buffs", nameof(DLC2Content.Buffs.ElusiveAntlersBuff)))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(ElusiveAntlersBuffMoveSpeed));
                return;
            }
            c.Next.Operand = elusiveAntlersMoveSpeedPerBuff;
        }

        private static void ElusiveAntlersBaseMovespeed(CharacterBody sender, StatHookEventArgs args)
        {
            if (!sender.inventory)
                return;
            int itemCount = sender.inventory.GetItemCountEffective(DLC2Content.Items.SpeedBoostPickup);
            if(itemCount > 0)
            {
                args.moveSpeedMultAdd += elusiveAntlersFreeMovespeedBase + elusiveAntlersFreeMovespeedStack * (itemCount - 1);
            }
        }
        #endregion

        #region luminous shot

        public static float luminousTotalDamageBase = 1.75f;//1.75f
        public static float luminousTotalDamageStack = 1.0f;//0.5f
        public static void LuminousShotBuff()
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += ChangeLuminousShotStats;

            LanguageAPI.Add("ITEM_INCREASEPRIMARYDAMAGE_DESC",
                $"Activating <style=cIsUtility>Secondary skill</style> stores " +
                $"up to <style=cIsUtility>5 charges</style> <style=cStack>(+1 per stack)</style>. " +
                $"Requires <style=cIsUtility>3 charges</style> for your " +
                $"<style=cIsUtility>Primary skill</style> to fire lightning strikes, " +
                $"dealing <style=cIsDamage>{luminousTotalDamageBase.AsPercent()} TOTAL damage</style> " +
                $"<style=cStack>(+{luminousTotalDamageStack.AsPercent()} per stack)</style> each. " +
                $"<style=cIsUtility>Reduces Secondary skill cooldown by 20%</style>."
                );
        }

        private static void ChangeLuminousShotStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", nameof(DLC2Content.Items.IncreasePrimaryDamage)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ChangeLuminousShotStats), 1);
                return;
            }

            c.Next.Operand = luminousTotalDamageBase;

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(ChangeLuminousShotStats), 2);
                return;
            }

            c.Next.Operand = luminousTotalDamageStack;
        }
        #endregion

        #region eclipse lite

        public static float eclipseLiteHealPerSecondBase = 1f;
        public static float eclipseLiteHealPerSecondStack = 1f;
        public static void EclipseLiteChanges()
        {
            On.RoR2.CharacterBody.OnSkillCooldown += FixEclipseLiteRestockScaling;
            IL.RoR2.CharacterBody.OnSkillCooldown += ChangeEclipseLiteStats;

            LanguageAPI.Add("ITEM_BARRIERONCOOLDOWN_PICKUP", "Gain a small heal when a skill comes off cooldown.");
            LanguageAPI.Add("ITEM_BARRIERONCOOLDOWN_DESC", 
                $"When a skill comes off cooldown, <style=cIsHealing>heal</style> for " +
                $"<style=cIsHealing>{eclipseLiteHealPerSecondBase} <style=cStack>(+{eclipseLiteHealPerSecondStack} per stack)</style> health</style>. " +
                $"Scales with the skill's base cooldown.");
        }

        private static void FixEclipseLiteRestockScaling(On.RoR2.CharacterBody.orig_OnSkillCooldown orig, CharacterBody self, GenericSkill skill, int restocks)
        {
            if(restocks > 1)
            {
                int rechargeStock = skill.skillDef.GetRechargeStock(skill);
                if (rechargeStock > 1)
                    restocks = Mathf.CeilToInt(restocks / rechargeStock);
            }
            orig(self, skill, restocks);
        }

        private static void ChangeEclipseLiteStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ChangeEclipseLiteToHealing(c);
            RemoveEclipseLiteMaxHealthScaling(c);
        }

        private static HealthComponent ecliteThingy = null;
        private static void ChangeEclipseLiteToHealing(ILCursor c)
        {
            c.Index = 0;

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.AddBarrierAuthority))
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ChangeEclipseLiteToHealing));
                return;
            }

            c.Remove();
            c.EmitDelegate<Action<HealthComponent, float>>((healthComponent, value) =>
            {
                ecliteThingy = healthComponent;
                healthComponent.Heal(value, default(ProcChainMask), true);
            });
        }

        private static void RemoveEclipseLiteMaxHealthScaling(ILCursor c)
        {
            c.Index = 0;

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxHealth")
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(RemoveEclipseLiteMaxHealthScaling), 1);
                return;
            }
            //replace max health with 1
            c.EmitDelegate<Func<float, float>>((maxHealthWhichFunctionsAsAMultiplier) => { return 1; });

            //change the fraction values to not be fractions of 1
            ChangeSingleValue(eclipseLiteHealPerSecondBase, index: 1);
            ChangeSingleValue(eclipseLiteHealPerSecondStack, index: 2);

            void ChangeSingleValue(float newValue, int index)
            {
                bool b2 = c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcR4(out _)
                    );
                if (!b2)
                {
                    DebugBreakpoint($"{nameof(RemoveEclipseLiteMaxHealthScaling)}:{nameof(ChangeSingleValue)}", 1);
                    return;
                }
                c.Next.Operand = newValue;
            }
        }

        private static void RemoveEclipseLiteRestockScaling(ILCursor c)
        {
            c.Index = 0;

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(2),
                x => x.MatchConvR4()
                );
            if (!b)
            {
                DebugBreakpoint(nameof(RemoveEclipseLiteRestockScaling));
                return;
            }

            c.EmitDelegate<Func<float, GenericSkill, float>>((restocks, skill) =>
            {
                int rechargeStock = skill.skillDef.GetRechargeStock(skill);
                if (rechargeStock > 1)
                {
                    restocks /= rechargeStock;
                }
                return restocks;
            });
            //c.EmitDelegate<Func<int, int>>((_) => { return 1; });
        }
        #endregion

        #region topaz brooch

        public static float broochPercentBase = 0.02f;
        public static float broochPercentStack = 0.0f;
        public static float broochFlatBase = 15f;//15f
        public static float broochFlatStack = 15f;//15f

        public static void TopazBroochBuff()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += TopazBroochPercentBarrier;

            LanguageAPI.Add("ITEM_BARRIERONKILL_DESC",
                $"Gain a <style=cIsHealing>temporary barrier</style> on kill " +
                $"for <style=cIsHealing>15 health <style=cStack>(+15 per stack)</style></style> " +
                $"PLUS <style=cIsHealing>{broochPercentBase.AsPercent()}</style> of your <style=cIsHealing>maximum health</style>.");
        }

        private static void TopazBroochPercentBarrier(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int broochCountLoc = 55;
            int bodyLoc = 16;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", nameof(RoR2Content.Items.BarrierOnKill)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(out broochCountLoc))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out bodyLoc),
                x => x.MatchCallOrCallvirt<CharacterBody>("get_healthComponent"))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.AddBarrier))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(TopazBroochPercentBarrier));
                return;
            }

            c.Emit(OpCodes.Ldloc, broochCountLoc);
            c.Emit(OpCodes.Ldloc, bodyLoc);
            c.EmitDelegate<Func<float, int, CharacterBody, float>>((barrierIn, stack, body) =>
            {
                if (body == null)
                    return barrierIn;

                float percentInBarrier = broochPercentBase + (broochPercentStack * (stack - 1));
                return barrierIn + body.healthComponent.fullCombinedHealth * percentInBarrier;
            });
        }
        #endregion
    }

}