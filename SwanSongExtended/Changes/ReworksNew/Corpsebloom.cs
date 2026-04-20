using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static MoreStats.StatHooks;
using static R2API.RecalculateStatsAPI;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;
using RoR2.Items;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace SwanSongExtended.Changes
{
    public class Corpsebloom : ReworkBase<Corpsebloom>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return Aegis.GetOverhealReworkConfig();
        }

        public static BuffDef lunarRotBuff;
        public static BuffDef lunarRotBarrierCooldown;

        public static int luckBase = 2;
        public static int luckStack = 2; //maybe 1?

        public static float healthRegenBase = -2;
        public static float healthRegenStack = -2;
        public static float healthRegenLevelBase = -0.3f;
        public static float healthRegenLevelStack = -0.3f;

        public static int rotStacksForFirstBonus = 3;
        public static int rotStacksForSecondBonus = 6;
        public static float damageBase = 4;
        public static float damageStack = 2;
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_RepeatHeal.RepeatHeal_asset;

        public override string ItemName => "Corpsebloom";

        public override string ItemPickupDesc => 
            "Your health degenerates over time. Gain barrier and luck at low health.";

        public override string ItemFullDesc => 
            $"For every missing {ConvertDecimal(1f / (float)LunarHealthDegenBehavior.maxBuffCount)} of health, gain Rot. " +
            $"After {rotStacksForFirstBonus} Rot has accumulated, increase base damage by {damageBase} {StackText($"+{damageStack}")}. " +
            $"After {rotStacksForSecondBonus} Rot, gain {ConvertDecimal(LunarHealthDegenBehavior.barrierFraction)} barrier " +
            $"and increase Luck by {luckBase} {StackText($"+{luckStack}")}. " +
            $"{RedText($"Reduce base health regeneration by {healthRegenBase} hp/s")} {StackText($"{healthRegenStack}")}.";

        public override void Init()
        {
            Log.Warning("Corpsebloom rework not fully implemented");
            base.Init();

            lunarRotBuff = Content.CreateAndAddBuff(
                "bdLunarFlowerRotBuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.blue,
                true, false
                );
            lunarRotBarrierCooldown = Content.CreateAndAddBuff(
                "bdLunarFlowerBarrierCooldown",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.white,
                false, false
                );
            lunarRotBarrierCooldown.isCooldown = true;
        }

        public override void Hooks()
        {
            GetMoreStatCoefficients += ElegyLuck;
            GetStatCoefficients += ElegyStats;
        }

        private void ElegyStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);

            if (itemCount > 0)
            {
                float degenMod = 1;
                int stackMod = itemCount - 1;
                float levelMult = (1 + 0.2f * sender.level);
                int buffCount = sender.GetBuffCount(lunarRotBuff.buffIndex);//LUCK/DAMAGE UP
                if (buffCount >= rotStacksForFirstBonus)
                {
                    //sender.damage += (damageBase + (damageLevel * (sender.level - 1)));
                    args.baseDamageAdd += (damageBase + damageStack * stackMod) * levelMult;
                    if (buffCount >= rotStacksForSecondBonus)
                    {
                        degenMod = 0.5f;
                    }
                }
                //sender.regen += (healthRegenBase + (healthRegenStack * (itemCount - 1))) * degenMod;//health degen
                args.baseRegenAdd += (healthRegenBase + healthRegenStack * stackMod) * degenMod * levelMult;//health degen
            }
        }

        private void ElegyLuck(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.GetBuffCount(lunarRotBuff.buffIndex) >= rotStacksForSecondBonus)
            {
                args.luckAdd += luckBase + luckStack * (GetCount(sender) - 1);
            }
        }
    }
    public class LunarHealthDegenBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => RoR2Content.Items.RepeatHeal;

        HealthComponent healthComponent;
        BuffIndex luckUpBuffIndex => Corpsebloom.lunarRotBuff.buffIndex;
        BuffIndex barrierCooldownBuffIndex => Corpsebloom.lunarRotBarrierCooldown.buffIndex;
        public static int maxBuffCount = 8;
        int buffCount = 0;

        public static float barrierFraction = 0.5f;
        public static float barrierCoolDown = 30;

        private void Start()
        {
            healthComponent = body.healthComponent;
            buffCount = body.GetBuffCount(luckUpBuffIndex);
        }

        private void FixedUpdate()
        {
            float missingHealthFraction = 1 - ((healthComponent.health + healthComponent.shield) / healthComponent.fullCombinedHealth);
            int newBuffCount = Mathf.CeilToInt(missingHealthFraction * (maxBuffCount));
            while (newBuffCount > buffCount && buffCount < maxBuffCount)
            {
                this.body.AddBuff(luckUpBuffIndex);
                buffCount++;
                if (buffCount >= Corpsebloom.rotStacksForSecondBonus & !body.HasBuff(barrierCooldownBuffIndex))
                {
                    healthComponent.AddBarrier(healthComponent.fullCombinedHealth * barrierFraction);
                    body.AddTimedBuff(barrierCooldownBuffIndex, barrierCoolDown);
                }
            }
            //NUCLEAR SAFE - this code does not run on clients
            this.body.SetBuffCount(luckUpBuffIndex, newBuffCount);
        }
        void OnDestroy()
        {
            this.body.SetBuffCount(luckUpBuffIndex, 0);
        }
    }
}
