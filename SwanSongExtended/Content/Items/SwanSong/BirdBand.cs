using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static MoreStats.OnHit;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class BirdBand : ItemBase<BirdBand>
    {
        public override string ConfigName => "Items : Devs Item";
        public static BuffDef birdBuff;
        public static BuffDef birdDebuff;
        public static float regenDurationBase = 1.5f;
        public static float regenDurationStack = 0.75f;
        public static int cooldownTime = 7;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Dev\u2019s Item";

        public override string ItemLangTokenName => "BIRDBAND";

        public override string ItemPickupDesc => "High damage hits also make you Regenerative for a short duration. Recharges over time.";

        public override string ItemFullDescription => $"Hits that deal <style=cIsDamage>more than 400% damage</style> also make you <style=cIsHealing>Regenerative</style> " +
            $"for <style=cIsDamage>{regenDurationBase} seconds</style> <style=cStack>(+{regenDurationStack} seconds per stack)</style>, " +
            $"restoring <style=cIsHealing>10% of your maximum health</style> per second. Recharges every {cooldownTime} seconds.";

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

        public override GameObject ItemModel => LoadDropPrefab("mdlBirdBand");

        public override Sprite ItemIcon => LoadItemIcon("texIconBirdBand");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }
        public override void Init()
        {
            birdBuff = Content.CreateAndAddBuff(
                "bdBirdBandReady",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                Color.green,
                false, false
                );
            birdDebuff = Content.CreateAndAddBuff(
                "bdBirdBandCooldown",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsCooldownIcon.tif").WaitForCompletion(),
                Color.blue,
                true, true
                );
            birdDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            base.Init();
        }
        public override void Hooks()
        {
        }
    }
    public class BirdBandItemBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => BirdBand.instance.ItemsDef;

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            if (damageReport.attackerBody == null)
                return;

            if (stack <= 0 || !body.HasBuff(BirdBand.birdBuff))
                return;

            float damageCoefficient = damageReport.damageInfo.damage / body.damage;
            if (damageCoefficient < 4)// && !damageInfo.procChainMask.HasProc(ProcType.Rings))
                return;

            body.RemoveBuff(BirdBand.birdBuff);
            for (int i = 0; i < BirdBand.cooldownTime; i++)
            {
                body.AddTimedBuffAuthority(BirdBand.birdDebuff.buffIndex, i + 1);
            }
            //ProcChainMask procChainMask = damageInfo.procChainMask;
            //procChainMask.AddProc(ProcType.Rings);
            body.AddTimedBuffAuthority(RoR2Content.Buffs.CrocoRegen.buffIndex, BirdBand.regenDurationBase + (BirdBand.regenDurationStack * (stack - 1)));
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
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
