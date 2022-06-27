using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class Beans : ItemBase
    {
        public static BuffDef beansHealBuff;
        public static int maxBuffs = 4;
        public static float buffDuration = 2f;

        public static int baseHeal = 8;
        public static int buffHeal = 12;
        public static float stackMultiplierAdd = 0.5f;

        public override string ItemName => "The Beans of Tragedy";

        public override string ItemLangTokenName => "BORBOBAKEDBEANS";

        public override string ItemPickupDesc => "Heal instantly on kill. Rapid kills heal for more.";

        public override string ItemFullDescription => $"On kill, <style=cIsHealing>heal for {baseHeal} health</style> " +
            $"<style=cStack>(+{(int)(baseHeal * stackMultiplierAdd)} per stack)</style>. " +
            $"Also gain a <style=cIsUtility>temporary buff</style> that increases this healing by <style=cIsHealing>{buffHeal} per buff</style> " +
            $"<style=cStack>(+{(int)(buffHeal * stackMultiplierAdd)} per stack)</style>, " +
            $"up to a maximum cap of <style=cIsUtility>{maxBuffs} times.</style>";

        public override string ItemLore => "There's nothing inside.";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Healing };

        public override GameObject ItemModel => RiskierRainPlugin.assetBundle.LoadAsset<GameObject>(RiskierRainPlugin.modelsPath + "Item/Beans.prefab");

        public override Sprite ItemIcon => RiskierRainPlugin.assetBundle.LoadAsset<Sprite>(RiskierRainPlugin.iconsPath + "Item/texIconBeans.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += OnKillStuff;
        }

        private void OnKillStuff(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, RoR2.GlobalEventManager self, RoR2.DamageReport damageReport)
        {
            orig(self, damageReport);

            CharacterBody enemyBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (enemyBody.healthComponent.alive)
            {
                Debug.Log("Hello, Aetherium User");
                return;
            }

            int itemCount = GetCount(attackerBody);
            if (itemCount > 0)
            {
                int buffCount = attackerBody.GetBuffCount(beansHealBuff);
                float healAmt = (baseHeal + buffHeal * buffCount) * (1 + stackMultiplierAdd * (itemCount - 1));

                attackerBody.healthComponent.Heal(healAmt, new ProcChainMask());

                attackerBody.ClearTimedBuffs(beansHealBuff);
                int newBuffCount = Mathf.Min(buffCount + 1, maxBuffs);
                for(int i = 0; i < newBuffCount; i++)
                {
                    attackerBody.AddTimedBuffAuthority(beansHealBuff.buffIndex, buffDuration);
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        void CreateBuff()
        {
            beansHealBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                beansHealBuff.name = "BeansHealing";
                beansHealBuff.buffColor = Color.yellow;
                beansHealBuff.canStack = true;
                beansHealBuff.isDebuff = false;
                beansHealBuff.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffMedkitHealIcon");
            };
            Assets.buffDefs.Add(beansHealBuff);
        }
    }
}
