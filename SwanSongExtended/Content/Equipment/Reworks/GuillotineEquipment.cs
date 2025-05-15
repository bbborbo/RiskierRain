using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static MoreStats.StatHooks;
using System.Linq;
using System.Collections;
using UnityEngine.AddressableAssets;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Equipment
{
    class GuillotineEquipment : EquipmentBase<GuillotineEquipment>
    {
        public override AssetBundle assetBundle => null;
        public static BuffDef luckBuff;
        public static BuffDef executionDebuff;
        #region config
        public override string ConfigName => "Reworks : Old Guillotine";

        [AutoConfig("Bonus Aspect Drop Chance", 0.05f)]
        public static float aspectDropChance = 0.05f;

        [AutoConfig("Base Execution Threshold", 0.20f)]
        public static float newExecutionThresholdBase = 0.20f;
        [AutoConfig("Bonus Execution Threshold For Status", 0.10f)]
        public static float newExecutionThresholdStack = 0.10f;

        [AutoConfig("Execution Status Bonus VS Bosses", false)]
        bool strongerVsBosses = false;
        [AutoConfig("Execution Status Bonus VS Elites", true)]
        bool strongerVsElites = true;

        [AutoConfig("Execution Duration", 10)]
        public static float executeDuration = 10;
        [AutoConfig("Luck Duration", 10)]
        public static float luckDuration = 9;
        [AutoConfig("Guillotine Damage Coefficient", 1)]
        public static float guillotineDamageCoefficient = 1;
        #endregion
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

        string baseThreshold = Tools.ConvertDecimal(newExecutionThresholdBase + newExecutionThresholdStack);
        string stackThreshold = Tools.ConvertDecimal(newExecutionThresholdStack);

        public override string EquipmentName => "Old Guillotine";

        public override string EquipmentLangTokenName => "BOBOGUILLOTINE";

        public override string EquipmentPickupDesc => "Instantly kill low health Elite monsters.";
        //"Target a low health monster to instantly kill them, empowering yourself. Stronger against Elites.";

        public override string EquipmentFullDescription => $"Instantly kill Elite monsters below {RedText($"{baseThreshold} max health")}. " +
            $"{UtilityColor(Tools.ConvertDecimal(aspectDropChance) + "chance")} to claim the power of slain Elite monsters.";
        //$"Target a monster, allowing them to be " +
        //$"{RedText("instantly killed")} at or below {RedText($"{baseThreshold} max health")} " +
        //$"{StackColor($"(+{stackThreshold} per Elite tier)")}. " +
        //$"Executing them in this manner will {DamageColor("empower you")}, " +
        //$"resetting {UtilityColor("all skill cooldowns")} and {UtilityColor($"increasing Luck for {luckDuration} seconds")}.";

        public override string EquipmentLore => "Order: Old Guillotine" +
            "\nTracking Number: 782*****" +
            "\nEstimated Delivery: 04/29/2056" +
            "\nShipping Method: Standard" +
            "\nShipping Address: Warehouse 36, Anklar, Primas V" +
            "\nShipping Details:\n" +
            "" +
            "\nEveryone is still operating on adrenaline here. We finally overthrew our oppressors and have taken back Primas V! " +
            "I know some of the overlords will attempt to buy their way onto a stealth transport, but that’s going to be quite difficult due to their epic economic blunder.\n" +
            "" +
            "\nWe don’t just want blood for all the injustices we’ve suffered at their hands. We want to send a message to would-be sympathizers. " +
            "This old guillotine will serve both as an execution method and a symbol to strike fear into their hearts wherever they might be hiding.\n" +
            "\nPrimas V is alive!";

        public override GameObject EquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/pickupmodels/PickupGuillotine");

        public override Sprite EquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/itemicons/texGuillotineIcon");

        public override float BaseCooldown => 25f;
        public override bool EnigmaCompatible => false;
        public override bool CanBeRandomlyActivated => false;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;
        }

        public IEnumerator GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();

            CloneVanillaDisplayRules(instance.EquipDef, RoR2Content.Items.ExecuteLowHealthElite);
            yield break;
        }

        #region stats

        private void GuillotineStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            int executionBuffCount = sender.GetBuffCount(executionDebuff);
            float threshold = newExecutionThresholdBase + newExecutionThresholdStack * executionBuffCount;
            args.ModifyBaseExecutionThreshold(threshold, executionBuffCount > 0);

            args.luckAdd += sender.GetBuffCount(luckBuff);
        }

        private void GuillotineExecuteBehavior(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            CharacterBody attackerBody = damageReport.attackerBody;
            CharacterBody victimBody = damageReport.victimBody;

            if (attackerBody && victimBody)
            {
                SkillLocator skillLocator = attackerBody.skillLocator;
                if (victimBody.HasBuff(executionDebuff))
                {
                    attackerBody.AddTimedBuffAuthority(luckBuff.buffIndex, luckDuration);

                    if (skillLocator != null)
                    {
                        //apply skill reset
                        if (NetworkServer.active && !skillLocator.networkIdentity.hasAuthority)
                        {
                            NetworkWriter networkWriter = new NetworkWriter();
                            networkWriter.StartMessage(63);
                            networkWriter.Write(skillLocator.gameObject);
                            networkWriter.FinishMessage();
                            NetworkConnection clientAuthorityOwner = skillLocator.networkIdentity.clientAuthorityOwner;
                            if (clientAuthorityOwner != null)
                            {
                                clientAuthorityOwner.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
                                return;
                            }
                        }
                        else
                        {
                            GenericSkill[] array = new GenericSkill[]
                            {
                                skillLocator.primary,
                                skillLocator.secondary,
                                skillLocator.utility,
                                skillLocator.special
                            };
                            Util.ShuffleArray<GenericSkill>(array);
                            foreach (GenericSkill genericSkill in array)
                            {
                                if (genericSkill && genericSkill.CanApplyAmmoPack())
                                {
                                    Debug.LogFormat("Resetting skill {0}", new object[]
                                    {
                                    genericSkill.skillName
                                    });
                                    genericSkill.AddOneStock();
                                }
                            }
                        }
                    }
                }
            }


            orig(self, damageReport);
        }
        #endregion

        public override void Init()
        {
            SwanSongPlugin.RetierItem(Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/ExecuteLowHealthElite/ExecuteLowHealthElite.asset").WaitForCompletion());
            //Debug.LogError("Riskier Rain Guillotine Equipment still needs to be fixed!");

            executionDebuff = Content.CreateAndAddBuff("bdExecutionDebuffStackable",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Nullifier/texBuffNullifiedIcon.tif").WaitForCompletion(),
                Color.white,
                true, true);
            executionDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;

            luckBuff = Content.CreateAndAddBuff("bdLuckBuffStackable",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Nullifier/texBuffNullifiedIcon.tif").WaitForCompletion(),
                Color.green,
                true, false);

            base.Init();

            EquipDef.unlockableDef = UnlockableCatalog.GetUnlockableDef("KillElitesMilestone");
        }

        public override void Hooks()
        {
            //On.RoR2.EquipmentSlot.UpdateTargets += GuillotineTargeting;
            //On.RoR2.GlobalEventManager.OnCharacterDeath += GuillotineExecuteBehavior;
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.EquipDef, RoR2Content.Items.ExecuteLowHealthElite);

            //GetMoreStatCoefficients += GuillotineStats;
            On.RoR2.CharacterBody.RecalculateStats += AddEliteExecuteThreshold;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += GuillotineNewExecuteBehavior;
        }

        private void GuillotineNewExecuteBehavior(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<EquipmentDef>(nameof(EquipmentDef.dropOnDeathChance))
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, DamageReport, float>>((baseDropChance, damageReport) =>
                {
                    float dropChance = baseDropChance;
                    if (damageReport.victimIsElite)
                    {
                        CharacterBody attackerBody = damageReport.attackerBody;
                        if (attackerBody != null && attackerBody.executeEliteHealthFraction > 0)
                        {
                            dropChance = aspectDropChance;
                        }
                    }
                    return dropChance;
                });
            }
        }

        private void AddEliteExecuteThreshold(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if(self.inventory?.currentEquipmentIndex == this.EquipDef.equipmentIndex)
            {
                self.executeEliteHealthFraction = newExecutionThresholdBase;
            }
        }

        private void GuillotineTargetingOld(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLocation = 51;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Equipment", "BossHunter"),
                x => x.MatchCallOrCallvirt<EquipmentDef>(nameof(EquipmentDef.equipmentIndex)),
                x => x.MatchCeq()
                );
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<int, EquipmentIndex, int>>((oneIfTrue, currentEquipIndex) =>
            {
                if (currentEquipIndex == GuillotineEquipment.instance.EquipDef.equipmentIndex)
                    oneIfTrue = 1;

                return oneIfTrue;
            });

            /*ILCursor c = new ILCursor(il);

            int num = 2;

            c.GotoNext(MoveType.After,
                x => x.MatchStloc(num)
                );

            c.Emit(OpCodes.Ldarg, 0);
            c.Emit(OpCodes.Ldloc, num);
            c.EmitDelegate<Func<EquipmentSlot, bool, bool>>((equipSlot, canTarget) =>
            {
                bool b = canTarget;

                if (equipSlot.stock > 0 && equipSlot.equipmentIndex == this.EquipDef.equipmentIndex)
                {
                    b = true;
                }

                return b;
            });
            c.Emit(OpCodes.Stloc, num);*/
        }

        private void GuillotineTargeting(On.RoR2.EquipmentSlot.orig_UpdateTargets orig, EquipmentSlot self, EquipmentIndex targetingEquipmentIndex, bool userShouldAnticipateTarget)
        {
            bool isGuillotine = targetingEquipmentIndex == GuillotineEquipment.instance.EquipDef.equipmentIndex;
            if (!isGuillotine)
            {
                orig(self, targetingEquipmentIndex, userShouldAnticipateTarget);
                return;
            }

            if (userShouldAnticipateTarget)
            {
                self.ConfigureTargetFinderForEnemies();
            }
            HurtBox source = null;

            //i think this makes it prioritize elites
            using (IEnumerator<HurtBox> enumerator = self.targetFinder.GetResults().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    HurtBox hurtBox = enumerator.Current;
                    if (hurtBox && hurtBox.healthComponent && hurtBox.healthComponent.body)
                    {
                        bool isElite = hurtBox.healthComponent.body.isElite;
                        if (isElite && !hurtBox.healthComponent.body.HasBuff(executionDebuff))
                        {
                            source = hurtBox;
                            break;
                        }
                    }
                }
            }
            if (source == null)
                source = self.targetFinder.GetResults().FirstOrDefault<HurtBox>();

            self.currentTarget = new EquipmentSlot.UserTargetInfo(source);

            bool targetHasTransform = self.currentTarget.transformToIndicateAt;
            if (targetHasTransform)
            {
                self.targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/LightningIndicator");
            }
            self.targetIndicator.active = targetHasTransform;
            self.targetIndicator.targetTransform = (targetHasTransform ? self.currentTarget.transformToIndicateAt : null);
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
            bool b = false;

            HurtBox hurtBox = slot.currentTarget.hurtBox;
            if (hurtBox)
            {
                CharacterBody targetBody = hurtBox.healthComponent.body;
                CharacterBody attackerBody = slot.characterBody;
                if (targetBody)
                {
                    float damage = 0;
                    DamageType type = DamageType.Generic;
                    if (targetBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                    {
                        damage = newExecutionThresholdBase * targetBody.maxHealth;
                        type |= DamageType.NonLethal;
                    }
                    else
                    {
                        int executeCount = 1 + PowerStatusCheck(targetBody);

                        for(int i = 0; i < executeCount; i++)
                        {
                            targetBody.AddTimedBuff(executionDebuff, executeDuration);
                        }
                        damage = attackerBody.damage * guillotineDamageCoefficient;
                    }

                    BlastAttack blastAttack = new BlastAttack()
                    {
                        radius = 10,
                        procCoefficient = 0,
                        position = targetBody.transform.position,
                        attacker = slot.gameObject,
                        crit = Util.CheckRoll(attackerBody.crit, attackerBody.master),
                        baseDamage = damage,
                        falloffModel = BlastAttack.FalloffModel.SweetSpot,
                        damageType = type,
                        baseForce = 0,
                        teamIndex = TeamComponent.GetObjectTeam(attackerBody.gameObject)
                    };
                    blastAttack.Fire();

                    slot.InvalidateCurrentTarget();

                    b = true;
                }
                
            }
            return b;
        }

        int PowerStatusCheck(CharacterBody body)
        {
            int power = 0;

            if (body.isElite && strongerVsElites)
            {
                power++;

                if (body.HasBuff(RoR2Content.Buffs.AffixHaunted) || body.HasBuff(RoR2Content.Buffs.AffixPoison))
                {
                    power++;
                }
            }
            if (body.isBoss && strongerVsBosses)
                power++;

            return power;
        }
    }
}
