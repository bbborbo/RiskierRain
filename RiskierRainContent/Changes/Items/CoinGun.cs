using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using RoR2.ExpansionManagement;

namespace RiskierRainContent.Items
{
    class CoinGun : ItemBase<CoinGun>
    {
        public static int baseGoldChunk = 25;
        public static bool includeDeploys = true;

        static float bonusDamageMin = 0.15f;

        static float bonusDamagePerChunk = 0.03f;
        float bonusGold = 0.1f;
        public static BuffDef bronzeDamageBuff;
        public static int maxBronze = 3;
        public static BuffDef silverDamageBuff;
        public static int maxSilver = 6;
        public static BuffDef goldDamageBuff;
        public static int maxGold = 9;
        public static BuffDef platinumDamageBuff;
        public static int maxPlatinum = 10;

        string damageBoostPerChestPerStack = Tools.ConvertDecimal(bonusDamagePerChunk);

        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Coin Gun";

        public override string ItemLangTokenName => "ECONOMYWEAPON";

        public override string ItemPickupDesc => "Deal bonus damage based on the gold you have saved up.";

        /*public override string ItemFullDescription => $"<style=cIsUtility>Gain {Tools.ConvertDecimal(bonusGold)} extra gold</style>. " +
            $"Also deal <style=cIsDamage>{damageBoostPerChestPerStack} <style=cStack>(+{damageBoostPerChestPerStack} per stack)</style></style> " +
            $"bonus damage <style=cIsDamage>per chest you can afford</style>, for up to a maximum of <style=cIsUtility>{maxPlatinum} chests</style>.";*/

        public override string ItemFullDescription => $"<style=cIsUtility>Gain {Tools.ConvertDecimal(bonusGold)} extra gold</style>. " +
            $"Also deal <style=cIsDamage>{Tools.ConvertDecimal(bonusDamageMin)} <style=cStack>(+{Tools.ConvertDecimal(bonusDamageMin)} per stack)</style> bonus damage</style>, " +
            $"plus <style=cIsDamage>{damageBoostPerChestPerStack} <style=cStack>(+{damageBoostPerChestPerStack} per stack)</style> per chest you can afford</style>, " +
            $"for up to a maximum of <style=cIsUtility>{maxPlatinum} chests</style>.";

        public override string ItemLore =>
@"Man, this thing is so cool. B-zap! Haha.

Hey. Dude. Check this out.

What the hell is that thing?

No. Watch this.

…Huh? That sucks.

Listen, dude. TWO. MONEY. GUNS.

You're an idiot. This one charges using gold. That one shoots gold. What happens if you shoot all our gold, Cooper? We can't charge our guns.

It's fine, dude. The monsters on this planet are loaded. Look at all this!

…Huh. What is that thing?

It's this, like, hat thing I found? It attracts money or something. If we survive this we're gonna be rich.

You've gotta stop picking up those things. They've all tried to kill us at some point.

Not this one, dude. Look- oops. Ow.

Uh.

What?

What happened to all of our gold?";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; } = new ItemTag[] { ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override GameObject ItemModel => LoadDropPrefab("CoinGun");

        public override Sprite ItemIcon => LoadItemIcon("texIconCoinGun");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterMaster.GiveMoney += GoldGunMoneyBoost;
            On.RoR2.HealthComponent.TakeDamage += GoldGunDamageBoost;
            GetStatCoefficients += this.GiveBonusDamage;
        }

        private void GiveBonusDamage(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                CoinGunBehavior coinGun = sender.GetComponent<CoinGunBehavior>();
                int damageBoostCount = coinGun.damageBoostCount;

                //float damageMult = Mathf.Sqrt(1 + bonusDamagePerChunk * damageBoostCount * itemCount) - 1;
                if(damageBoostCount > 0)
                {
                    float damageMult = bonusDamageMin + bonusDamagePerChunk * damageBoostCount;

                    args.damageMultAdd += damageMult * itemCount;
                }
            }
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    CoinGunBehavior GgBehavior = self.AddItemBehavior<CoinGunBehavior>(GetCount(self));
                }
            }
        }

        private void GoldGunMoneyBoost(On.RoR2.CharacterMaster.orig_GiveMoney orig, CharacterMaster self, uint amount)
        {
            int itemCount = GetCount(self);
            if (itemCount > 0)
            {
                amount = (uint)(amount * (1 + bonusGold));
            }

            orig(self, amount);
        }

        private void GoldGunDamageBoost(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (damageInfo.attacker != null)
            {
                CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
                if(body != null)
                {
                    var itemcount = GetCount(body);
                    if (itemcount > 0)
                    {
                        CoinGunBehavior coinGun = body.GetComponent<CoinGunBehavior>();
                        int damageBoostCount = coinGun.damageBoostCount;//body.GetBuffCount(CoinGun.goldDamageBuff);
                        CharacterMaster master = body.master;
                        /*var money = master.money;
                        if (includeDeploys)
                        {
                            var deployable = master.GetComponent<Deployable>();
                            if (deployable) money += deployable.ownerMaster.money;
                        }

                        float damageMult = Mathf.Sqrt(1 + bonusDamagePerChunk * ((damageBoostCount + 1) * itemcount));

                        damageInfo.damage *= damageMult;*/
                        if(Util.CheckRoll((damageBoostCount / maxPlatinum) * 100, master))
                        {
                            EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact"), damageInfo.position, Vector3.up, true);
                        }
                    }
                }
            }

            orig(self, damageInfo);
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
            GenerateCoinDamageBuff(ref bronzeDamageBuff, "Bronze", new Color(0.7f, 0.5f, 0.2f));
            GenerateCoinDamageBuff(ref silverDamageBuff, "Silver", new Color(0.6f, 0.6f, 0.6f));
            GenerateCoinDamageBuff(ref goldDamageBuff, "Gold", new Color(1.0f, 0.8f, 0.15f));
            GenerateCoinDamageBuff(ref platinumDamageBuff, "Platinum", new Color(0.9f, 0.9f, 1.0f));
        }

        static string baseName = "CoinGunDamageBoost";
        static Sprite defaultSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffFullCritIcon");
        //Sprite defaultSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.png").WaitForCompletion();
        BuffDef GenerateCoinDamageBuff(ref BuffDef coinBuff, string coinType, Color color, Sprite sprite = null)
        {
            coinBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                coinBuff.name = baseName + coinType;
                coinBuff.iconSprite = (sprite == null) ? defaultSprite : sprite;
                coinBuff.buffColor = color;
                coinBuff.canStack = true;
                coinBuff.isDebuff = false;
            };
            CoreModules.Assets.buffDefs.Add(coinBuff);

            return coinBuff;
        }
    }
    public class CoinGunBehavior : CharacterBody.ItemBehavior
    {
        public CharacterMaster master;
        public uint currentMoney = 0;
        int fixedBaseChestCost => Run.instance.GetDifficultyScaledCost(CoinGun.baseGoldChunk, Stage.instance.entryDifficultyCoefficient);
        public int damageBoostCount = 0;

        private void FixedUpdate()
        {
            if (currentMoney == master.money)
                return;

            currentMoney = master.money;
            if (CoinGun.includeDeploys)
            {
                var deployable = master.GetComponent<Deployable>();
                if (deployable) currentMoney += deployable.ownerMaster.money;
            }

            int newBuffCount = Mathf.Clamp((int)(currentMoney / fixedBaseChestCost), 0, CoinGun.maxPlatinum);

            if (damageBoostCount == newBuffCount)
                return;
            damageBoostCount = newBuffCount;
            Debug.Log(damageBoostCount);

            //reset buff counts
            /*foreach(BuffDef coinBuff in CoinGun.coinDamageBuffs)
            {
                if(body.GetBuffCount(coinBuff.buffIndex) > 0)
                {
                    body.SetBuffCount(coinBuff.buffIndex, 0);
                }
            }*/
            ResetAllBuffs();

            //find which buff to use
            BuffIndex coinDamageBuff = BuffIndex.None;
            if (damageBoostCount <= CoinGun.maxBronze)
            {
                coinDamageBuff = CoinGun.bronzeDamageBuff.buffIndex;
            }
            else if (damageBoostCount > CoinGun.maxBronze && damageBoostCount <= CoinGun.maxSilver)
            {
                coinDamageBuff = CoinGun.silverDamageBuff.buffIndex;
            }
            else if (damageBoostCount > CoinGun.maxSilver && damageBoostCount <= CoinGun.maxGold)
            {
                coinDamageBuff = CoinGun.goldDamageBuff.buffIndex;
            }
            else if (damageBoostCount > CoinGun.maxGold)//&& damageBoostCount <= CoinGun.maxGoldChunks)
            {
                coinDamageBuff = CoinGun.platinumDamageBuff.buffIndex;
            }
            body.SetBuffCount(coinDamageBuff, damageBoostCount);
        }

        private void ResetAllBuffs()
        {
            body.SetBuffCount(CoinGun.bronzeDamageBuff.buffIndex, 0);
            body.SetBuffCount(CoinGun.silverDamageBuff.buffIndex, 0);
            body.SetBuffCount(CoinGun.goldDamageBuff.buffIndex, 0);
            body.SetBuffCount(CoinGun.platinumDamageBuff.buffIndex, 0);
        }

        private void Start()
        {
            master = body.master;
            damageBoostCount = 0;
            currentMoney = 0;
        }
        void OnDestroy()
        {
            ResetAllBuffs();
        }
    }
}
