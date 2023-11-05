using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RiskierRain.CoreModules.StatHooks;

namespace RiskierRain.Items
{
    class BirdBand : ItemBase<BirdBand>
    {
        public static BuffDef birdBuff;
        public static BuffDef birdDebuff;
        public float regenDurationBase = 0.3f;
        public float regenDurationStack = 0.3f;

        public override string ItemName => "Dev\u2019s Item";

        public override string ItemLangTokenName => "BIRDBAND";

        public override string ItemPickupDesc => "High damage hits also make you Regenerative for a short duration. Recharges over time.";

        public override string ItemFullDescription => $"Hits that deal <style=cIsDamage>more than 400% damage</style> also make you <style=cIsHealing>Regenerative</style> " +
            $"for <style=cIsDamage>{regenDurationBase + regenDurationStack} seconds</style> <style=cStack>(+{regenDurationStack} seconds per stack)</style>, " +
            $"restoring <style=cIsHealing>10% of your maximum health</style> per second. Recharges every 5 seconds.";

        public override string ItemLore => 
@"“I’m telling you, they’re real.”

“No way, dude. It’s ridiculous even for a ghost story.”

“Yeah! ‘Even higher than the gods themselves’? Get real.”

“I’ve seen proof, I’m telling you. Shit that defies explanation.”

“Oh, do tell. I’m on the edge of my seat.”

“You know those missile launchers? The ones that used to shoot one big fuckoff rocket? Why’d they start vomiting those little stingers all of a sudden?”

“I dunno.”

“It was them. They rewrote it. And- and that all that shit that just disappeared, like it was never there. And all our medical supplies that suddenly got way less effective. You notice that all the monsters don’t hit as hard as they used to?”

“Dude…”

“It’s real, I’m telling you! And all our suits got so much… clunkier. All at once. Even though nothing’s wrong with them.”

“...”

“And my wungus only heals one hp now.”

“Dude. What the fuck are you talking about?”

“He’s lost it.”";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlBirdBand.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_BIRDBAND.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            GetHitBehavior += BirdBand_GetHitBehavior;
        }
        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<BirdBandBehavior>(GetCount(self));
            }
        }
        private void BirdBand_GetHitBehavior(CharacterBody body, DamageInfo damageInfo, GameObject victim)
        {
            int bandCount = GetCount(body);
            float damageCoefficient = damageInfo.damage / body.damage;
            if(bandCount > 0 && damageCoefficient >= 4 && body.HasBuff(birdBuff))// && !damageInfo.procChainMask.HasProc(ProcType.Rings))
            {
                body.RemoveBuff(birdBuff);
                for(int i = 0; i < 5; i++)
                {
                    body.AddTimedBuffAuthority(birdDebuff.buffIndex, i + 1);
                }
                //ProcChainMask procChainMask = damageInfo.procChainMask;
                //procChainMask.AddProc(ProcType.Rings);
                body.AddTimedBuffAuthority(RoR2Content.Buffs.CrocoRegen.buffIndex, regenDurationBase + regenDurationStack * bandCount);
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuffs();
            Hooks();
        }
        internal static void CreateBuffs()
        {
            birdBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                birdBuff.name = "birdBandReady";
                birdBuff.buffColor = Color.green;
                birdBuff.canStack = false;
                birdBuff.isDebuff = false;
                birdBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffElementalRingsReadyIcon");
            };
            Assets.buffDefs.Add(birdBuff);
            birdDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                birdDebuff.name = "birdBandCooldown";
                birdDebuff.buffColor = Color.blue;
                birdDebuff.canStack = true;
                birdDebuff.isDebuff = true;
                birdDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffElementalRingsCooldownIcon");
            };
            Assets.buffDefs.Add(birdDebuff);
        }
    }
    public class BirdBandBehavior : CharacterBody.ItemBehavior
    {
        private void FixedUpdate()
        {
            bool isBuffed = this.body.HasBuff(BirdBand.birdBuff);
            bool isDebuffed = this.body.HasBuff(BirdBand.birdDebuff);
            bool isNeither = !isBuffed && !isDebuffed;
            if (isNeither)
            {
                this.body.AddBuff(BirdBand.birdBuff);
            }
            bool isBoth = isBuffed && isDebuffed;
            if (isBoth)
            {
                this.body.RemoveBuff(BirdBand.birdBuff);
            }
        }
    }
}
