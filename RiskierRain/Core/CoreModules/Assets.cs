using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RiskierRain.CoreModules.StatHooks;

namespace RiskierRain.CoreModules
{
    class Assets : CoreModule
    {
        public static EffectDef CreateEffect(GameObject effect)
        {
            if (effect == null)
            {
                Debug.LogError("Effect prefab was null");
                return null;
            }

            var effectComp = effect.GetComponent<EffectComponent>();
            if (effectComp == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
                return null;
            }

            var vfxAttrib = effect.GetComponent<VFXAttributes>();
            if (vfxAttrib == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
                return null;
            }

            var def = new EffectDef
            {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };

            effectDefs.Add(def);
            return def;
        }

        public static List<ArtifactDef> artifactDefs = new List<ArtifactDef>();
        public static List<BuffDef> buffDefs = new List<BuffDef>();
        public static List<EffectDef> effectDefs = new List<EffectDef>();
        public static List<SkillFamily> skillFamilies = new List<SkillFamily>();
        public static List<SkillDef> skillDefs = new List<SkillDef>();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<GameObject> networkedObjectPrefabs = new List<GameObject>();
        public static List<Type> entityStates = new List<Type>();

        public static List<ItemDef> itemDefs = new List<ItemDef>();
        public static List<EquipmentDef> equipDefs = new List<EquipmentDef>();
        public static List<EliteDef> eliteDefs = new List<EliteDef>();

        public static List<GameObject> masterPrefabs = new List<GameObject>();
        public static List<GameObject> bodyPrefabs = new List<GameObject>();

        public static string executeKeywordToken = "DUCK_EXECUTION_KEYWORD";
        public static string shredKeywordToken = "DUCK_SHRED_KEYWORD";

        public override void Init()
        {
            AddShatterspleenSpikeBuff();
            AddRazorwireCooldown();
            AddTrophyHunterDebuffs();
            AddBanditExecutionDebuff();
            AddShredDebuff();
            AddCooldownBuff();
            AddAspdPenaltyDebuff();
            AddHopooDamageBuff();

            On.RoR2.CharacterBody.RecalculateStats += RecalcStats_Stats;

            LanguageAPI.Add(executeKeywordToken,
                $"<style=cKeywordName>Finisher</style>" +
                $"<style=cSub>Enemies targeted by this skill can be " +
                $"<style=cIsHealth>instantly killed</style> if below " +
                $"<style=cIsHealth>{Tools.ConvertDecimal(survivorExecuteThreshold)} health</style>.</style>");

            LanguageAPI.Add(shredKeywordToken, $"<style=cKeywordName>Shred</style>" +
                $"<style=cSub>Apply a stacking debuff that increases ALL damage taken by {shredArmorReduction}% per stack. Critical Strikes apply more Shred.</style>");

            AddExecutionDebuff();
            AddLuckBuff();
            IL.RoR2.HealthComponent.TakeDamage += AddExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;
            On.RoR2.CharacterBody.AddTimedBuff_BuffIndex_float += LuckBuffAdd;
            On.RoR2.CharacterBody.RemoveBuff_BuffIndex += LuckBuffRemove;
            On.RoR2.CharacterMaster.OnInventoryChanged += LuckCalculation;
        }

        public static BuffDef hopooDamageBuff;
        public static BuffDef hopooDamageBuffTemporary;
        private void AddHopooDamageBuff()
        {
            hopooDamageBuff = CreateHopooDamageBuff("Permanent");
            hopooDamageBuffTemporary = CreateHopooDamageBuff("Temporary");
            buffDefs.Add(hopooDamageBuff);
            buffDefs.Add(hopooDamageBuffTemporary);
        }
        private BuffDef CreateHopooDamageBuff(string suffix)
        {
            BuffDef hopoo = ScriptableObject.CreateInstance<BuffDef>();
            {
                hopoo.buffColor = Color.cyan;
                hopoo.canStack = true;
                hopoo.isDebuff = false;
                hopoo.name = "HopooFeatherDamageBuff" + suffix;
                hopoo.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            return hopoo;
        }

        public static BuffDef banditShredDebuff;
        public static int shredArmorReduction = 15;
        private void AddShredDebuff()
        {
            banditShredDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                banditShredDebuff.buffColor = Color.red;
                banditShredDebuff.canStack = true;
                banditShredDebuff.isDebuff = true;
                banditShredDebuff.name = "BanditShredDebuff";
                banditShredDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            buffDefs.Add(banditShredDebuff);
        }

        private void RecalcStats_Stats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.HasBuff(Assets.aspdPenaltyDebuff))
            {
                self.attackSpeed *= (1 - Assets.aspdPenaltyPercent);
            }
            int shredBuffCount = self.GetBuffCount(Assets.banditShredDebuff);
            if (shredBuffCount > 0)
            {
                self.armor -= shredBuffCount * shredArmorReduction;
            }
            if (self.HasBuff(captainCdrBuff))
            {
                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    float mult = (1 - captainCdrPercent);
                    ApplyCooldownScale(skillLocator.primary, mult);
                    ApplyCooldownScale(skillLocator.secondary, mult);
                    ApplyCooldownScale(skillLocator.utility, mult);
                    ApplyCooldownScale(skillLocator.special, mult);
                }
            }
        }

        public static BuffDef aspdPenaltyDebuff;
        public static float aspdPenaltyPercent = 0.20f;
        private void AddAspdPenaltyDebuff()
        {
            aspdPenaltyDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                aspdPenaltyDebuff.buffColor = Color.red;
                aspdPenaltyDebuff.canStack = false;
                aspdPenaltyDebuff.isDebuff = false;
                aspdPenaltyDebuff.name = "AttackSpeedPenalty";
                aspdPenaltyDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffSlow50Icon");
            }
            buffDefs.Add(aspdPenaltyDebuff);
        }


        public static BuffDef captainCdrBuff;
        public static float captainCdrPercent = 0.25f;

        private void AddCooldownBuff()
        {
            captainCdrBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                captainCdrBuff.buffColor = Color.yellow;
                captainCdrBuff.canStack = false;
                captainCdrBuff.isDebuff = false;
                captainCdrBuff.name = "CaptainBeaconCdr";
                captainCdrBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
            }
            buffDefs.Add(captainCdrBuff);
        }

        public static BuffDef desperadoExecutionDebuff;
        public static BuffDef lightsoutExecutionDebuff;
        public static float survivorExecuteThreshold = 0.15f;
        public static float banditExecutionThreshold = 0.1f;
        public static float harvestExecutionThreshold = 0.2f;

        private void AddBanditExecutionDebuff()
        {
            desperadoExecutionDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                desperadoExecutionDebuff.buffColor = Color.black;
                desperadoExecutionDebuff.canStack = false;
                desperadoExecutionDebuff.isDebuff = true;
                desperadoExecutionDebuff.name = "DesperadoExecutionDebuff";
                desperadoExecutionDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            buffDefs.Add(desperadoExecutionDebuff);
            lightsoutExecutionDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                lightsoutExecutionDebuff.buffColor = Color.black;
                lightsoutExecutionDebuff.canStack = false;
                lightsoutExecutionDebuff.isDebuff = true;
                lightsoutExecutionDebuff.name = "LightsOutExecutionDebuff";
                lightsoutExecutionDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            buffDefs.Add(lightsoutExecutionDebuff);
        }

        public static BuffDef bossHunterDebuff;
        public static BuffDef bossHunterDebuffWithScalpel;
        private void AddTrophyHunterDebuffs()
        {
            bossHunterDebuff = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuff.canStack = false;
            bossHunterDebuff.isDebuff = true;
            bossHunterDebuff.name = "TrophyHunterDebuff";
            bossHunterDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

            buffDefs.Add(bossHunterDebuff);

            bossHunterDebuffWithScalpel = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuffWithScalpel.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuffWithScalpel.canStack = false;
            bossHunterDebuffWithScalpel.isDebuff = true;
            bossHunterDebuffWithScalpel.name = "TrophyHunterScalpelDebuff";
            bossHunterDebuffWithScalpel.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

            buffDefs.Add(bossHunterDebuffWithScalpel);
        }

        public static BuffDef shatterspleenSpikeBuff;
        private void AddShatterspleenSpikeBuff()
        {
            shatterspleenSpikeBuff = ScriptableObject.CreateInstance<BuffDef>();

            shatterspleenSpikeBuff.buffColor = new Color(0.7f, 0.0f, 0.2f, 1);
            shatterspleenSpikeBuff.canStack = true;
            shatterspleenSpikeBuff.isDebuff = false;
            shatterspleenSpikeBuff.name = "ShatterspleenSpikeCharge";
            shatterspleenSpikeBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

            buffDefs.Add(shatterspleenSpikeBuff);
        }

        public static BuffDef noRazorwire;
        private void AddRazorwireCooldown()
        {
            noRazorwire = ScriptableObject.CreateInstance<BuffDef>();

            noRazorwire.buffColor = Color.black;
            noRazorwire.canStack = false;
            noRazorwire.isDebuff = true;
            noRazorwire.name = "NoRazorwire";
            noRazorwire.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffEntangleIcon");

            buffDefs.Add(noRazorwire);
        }

        public static void RecalculateLuck(CharacterMaster master)
        {
            float luck = 0;
            CharacterBody body = master.GetBody();
            if (body)
            {
                luck += body.GetBuffCount(luckBuffIndex);
            }
            luck += (float)master.inventory.GetItemCount(RoR2Content.Items.Clover);
            luck -= (float)master.inventory.GetItemCount(RoR2Content.Items.LunarBadLuck);

            master.luck = luck;
        }
        private void LuckCalculation(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);
            RecalculateLuck(self);
        }
        private void LuckBuffRemove(On.RoR2.CharacterBody.orig_RemoveBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
        {
            orig(self, buffType);
            if (buffType == luckBuffIndex.buffIndex)
            {
                RecalculateLuck(self.master);
            }
        }
        private void LuckBuffAdd(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffIndex_float orig, CharacterBody self, BuffIndex buffType, float duration)
        {
            orig(self, buffType, duration);
            if(buffType == luckBuffIndex.buffIndex)
            {
                RecalculateLuck(self.master);
            }
        }

        public static BuffDef executionDebuffIndex;
        public static float newExecutionThresholdBase = 0.15f;
        public static float newExecutionThresholdStack = 0.10f;

        public static BuffDef luckBuffIndex;

        private void AddExecutionDebuff()
        {
            executionDebuffIndex = ScriptableObject.CreateInstance<BuffDef>();

            executionDebuffIndex.buffColor = Color.white;
            executionDebuffIndex.canStack = true;
            executionDebuffIndex.isDebuff = false;
            executionDebuffIndex.name = "ExecutionDebuffStackable";
            executionDebuffIndex.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffNullifiedIcon");

            buffDefs.Add(executionDebuffIndex);
        }

        private void AddLuckBuff()
        {
            luckBuffIndex = ScriptableObject.CreateInstance<BuffDef>();

            luckBuffIndex.buffColor = Color.green;
            luckBuffIndex.canStack = true;
            luckBuffIndex.isDebuff = false;
            luckBuffIndex.name = "LuckBuffStackable";
            luckBuffIndex.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffNullifiedIcon");

            buffDefs.Add(luckBuffIndex);
        }

        private void AddExecutionThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int thresholdPosition = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(float.NegativeInfinity),
                x => x.MatchStloc(out thresholdPosition)
                );
            
            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
                );

            c.Emit(OpCodes.Ldloc, thresholdPosition);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((currentThreshold, hc) =>
            {
                float newThreshold = currentThreshold;

                newThreshold = GetExecutionThreshold(currentThreshold, hc);

                return newThreshold;
            });
            c.Emit(OpCodes.Stloc, thresholdPosition);
        }

        static float GetExecutionThreshold(float currentThreshold, HealthComponent healthComponent)
        {
            float newThreshold = currentThreshold;
            CharacterBody body = healthComponent.body;

            if (body != null)
            {
                if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                {
                    //bandit specials (finisher)
                    bool hasBanditExecutionBuff = body.HasBuff(desperadoExecutionDebuff) || body.HasBuff(lightsoutExecutionDebuff);
                    newThreshold = ModifyExecutionThreshold(newThreshold, survivorExecuteThreshold, hasBanditExecutionBuff);
                    
                    //rex harvest (finisher)
                    bool hasRexHarvestBuff = body.HasBuff(RoR2Content.Buffs.Fruiting);
                    newThreshold = ModifyExecutionThreshold(newThreshold, survivorExecuteThreshold, hasRexHarvestBuff);

                    //guillotine
                    int executionBuffCount = body.GetBuffCount(executionDebuffIndex);
                    float threshold = newExecutionThresholdBase + newExecutionThresholdStack * executionBuffCount;
                    newThreshold = ModifyExecutionThreshold(newThreshold, threshold, executionBuffCount > 0);
                }
            }

            return newThreshold;
        }

        public static float ModifyExecutionThreshold(float currentThreshold, float newThreshold, bool condition)
        {
            if (condition)
            {
                return Mathf.Max(currentThreshold, newThreshold);
            }
            //else...
            return currentThreshold;
        }

        private HealthComponent.HealthBarValues DisplayExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.cullFraction = Mathf.Clamp01(GetExecutionThreshold(values.cullFraction, self));

            return values;
        }
    }
}
