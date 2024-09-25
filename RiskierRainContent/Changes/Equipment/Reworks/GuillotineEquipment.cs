using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
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
using static BorboStatUtils.BorboStatUtils;
using System.Linq;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent.Equipment
{
    class GuillotineEquipment : EquipmentBase<GuillotineEquipment>
    {
        public static BuffDef luckBuff;
        public static BuffDef executionDebuff;
        public static float newExecutionThresholdBase = 0.15f;
        public static float newExecutionThresholdStack = 0.10f;

        bool strongerVsBosses = false;
        bool strongerVsElites = true;

        private float executeDuration = 10;
        private float luckDuration = 9;
        private float guillotineDamageCoefficient = 1;
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

        string baseThreshold = Tools.ConvertDecimal(newExecutionThresholdBase + newExecutionThresholdStack);
        string stackThreshold = Tools.ConvertDecimal(newExecutionThresholdStack);

        public override string EquipmentName => "Old Guillotine";

        public override string EquipmentLangTokenName => "BOBOGUILLOTINE";

        public override string EquipmentPickupDesc => "Target a low health monster to instantly kill them, empowering yourself. Stronger against Elites.";

        public override string EquipmentFullDescription => $"Target a monster, allowing them to be " +
            $"<style=cIsHealth>instantly killed at or below {baseThreshold} max health (+{stackThreshold} per Elite tier).</style> " +
            $"Executing them in this manner will <style=cIsDamage>empower you</style>, " +
            $"resetting <style=cIsUtility>all skill cooldowns</style> and <style=cIsUtility>increasing Luck for {luckDuration} seconds.</style>";

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

        public override bool CanDrop { get; } = true;

        public override float Cooldown { get; } = 25f;

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

        #region assets
        private void AddExecutionDebuff()
        {
            executionDebuff = ScriptableObject.CreateInstance<BuffDef>();

            executionDebuff.buffColor = Color.white;
            executionDebuff.canStack = true;
            executionDebuff.isDebuff = false;
            executionDebuff.name = "ExecutionDebuffStackable";
            executionDebuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Nullifier/texBuffNullifiedIcon.tif").WaitForCompletion();

            CoreModules.Assets.buffDefs.Add(executionDebuff);
        }
        private void AddLuckBuff()
        {
            luckBuff = ScriptableObject.CreateInstance<BuffDef>();

            luckBuff.buffColor = Color.green;
            luckBuff.canStack = true;
            luckBuff.isDebuff = false;
            luckBuff.name = "LuckBuffStackable";
            luckBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Nullifier/texBuffNullifiedIcon.tif").WaitForCompletion();

            CoreModules.Assets.buffDefs.Add(luckBuff);
        }
        #endregion

        #region stats
        private void GuillotineLuckBuff(CharacterBody sender, ref float luck)
        {
            luck += sender.GetBuffCount(luckBuff);
        }

        private void GuillotineExecutionThreshold(CharacterBody sender, ref float executeThreshold)
        {
            int executionBuffCount = sender.GetBuffCount(executionDebuff);

            float threshold = newExecutionThresholdBase + newExecutionThresholdStack * executionBuffCount;
            executeThreshold = ModifyExecutionThreshold(executeThreshold, threshold, executionBuffCount > 0);
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

        public override void Init(ConfigFile config)
        {
            RiskierRainContent.RetierItem(nameof(RoR2Content.Items.ExecuteLowHealthElite), ItemTier.NoTier);
            //Debug.LogError("Riskier Rain Guillotine Equipment still needs to be fixed!");
            AddExecutionDebuff();
            AddLuckBuff();
            CreateEquipment();
            CreateLang();
            Hooks();
        }

        public override void Hooks()
        {
            On.RoR2.EquipmentSlot.UpdateTargets += GuillotineTargeting;
            On.RoR2.GlobalEventManager.OnCharacterDeath += GuillotineExecuteBehavior;
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.EquipDef, RoR2Content.Items.ExecuteLowHealthElite);
            GetExecutionThreshold += GuillotineExecutionThreshold;
            ModifyLuckStat += GuillotineLuckBuff;
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
