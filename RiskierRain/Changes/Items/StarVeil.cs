using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Items
{
    class StarVeil : ItemBase<StarVeil>
    {
        static float iframeDurationBase = 0.33f;
        static float iframeDurationStack = 0.33f;

        static float stormDamageCoefficient = 3f;
        static int stormWavesBase = 3;
        static int stormWavesStack = 2;

        public override string ItemName => "Star Veil";

        public override string ItemLangTokenName => "STARVEIL";

        public override string ItemPickupDesc => "Taking damage causes you to become invincible... " +
            "<style=cIsHealth>BUT meteors will fall nearby, hurting both enemies and allies.</style>";

        public override string ItemFullDescription => $"After getting hit, become <style=cIsUtility>invincible to all incoming damage</style> " +
            $"for {iframeDurationBase} <style=cStack>(+{iframeDurationStack} per stack)</style> seconds. " +
            $"Then, cause {Mathf.RoundToInt(stormWavesBase)} <style=cStack>(+{Mathf.RoundToInt(stormWavesStack)} per stack)</style> " +
            $"<style=cIsDamage>waves of meteors</style> to fall from the sky, " +
            $"<style=cIsHealth>damaging ALL characters</style> for <style=cIsDamage>{Tools.ConvertDecimal(stormDamageCoefficient)} damage per blast.</style>";

        public override string ItemLore =>
@"ITS ME. I HAVE IT.

ARE YOU EXCITED? ARE YOU SCARED? ARE YOU CONTRITE? ARE YOU DISBELIEVING?

MURDERER.

YOU SLEW MY ?????. YOU DESECRATED THEIR FORMS. YOU USED THEIR SOULS TO SLAUGHTER MY ?????. YOU PERFORMED HIDEOUS EXPERIMENTS IN THE NAME OF FALSE GODS. AND THE RESULT GAVE YOU POWER.

BUT YOU LOST IT.

AND NOW I HAVE IT. I AM SURROUNDED BY THE SOULS OF MY ????? AND I AM FULL OF VENGEANCE.

WHAT ARE YOU FULL OF? FEAR AND REGRET?

WHERE IS YOUR “INVINCIBILITY” NOW? WHERE IS YOUR “GOD SLAYING POWER?” WHERE ARE YOUR “STARS?”

THEY ARE WITH ME. YOU ARE SO MUCH MORE THAN A MURDERER. YOU ARE A FOOL. IMBECILE. LACKWIT. AND YOU WILL COME TO REGRET EVERYTHING YOU HAVE DONE TO ME AND MY ?????.

BECAUSE I AM COMING FOR YOU. EVERY PATHETIC BLOW YOU GIVE TO ME I WILL USE TO RAIN DEATH UPON EVERYTHING YOU HAVE EVER KNOWN.

WHAT WILL YOU DO? YOUR FALSE GODS WILL NOT ANSWER PRAYER. THERE IS NOTHING YOU CAN DO. IF YOU RUN I WILL ONLY BE ANGRIER.

I WILL BE THERE SOON. YOU WILL NOT HAVE TO DESPAIR FOR LONG.

THE SOULS OF MY ????? WILL DRINK YOUR SCREAMS LIKE NECTAR.";

        public override ItemTier Tier => ItemTier.Lunar;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable, ItemTag.Damage };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override BalanceCategory Category => BalanceCategory.StateOfDefenseAndHealing;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += StarVeilTakeDamage;
        }

        private void StarVeilTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (!damageInfo.rejected && damageInfo.procCoefficient > 0)
            {
                int itemCount = GetCount(self.body);
                if (itemCount > 0 && !self.body.HasBuff(RoR2Content.Buffs.Immune) && !damageInfo.damageType.HasFlag(DamageType.Silent))
                {
                    float iframes = GetStackValue(iframeDurationBase, iframeDurationStack, itemCount);
                    self.body.AddTimedBuffAuthority(RoR2Content.Buffs.Immune.buffIndex, iframes);

                    MeteorStormController stormController =
                        UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"),
                        self.body.corePosition, Quaternion.identity).GetComponent<MeteorStormController>();

                    stormController.owner = self.gameObject;
                    stormController.ownerDamage = self.body.damage;
                    stormController.isCrit = Util.CheckRoll(self.body.crit, self.body.master);
                    stormController.waveCount = (int)GetStackValue(stormWavesBase, stormWavesStack, itemCount);
                    stormController.impactDelay = 1;// Mathf.Min(iframes, 2);
                    stormController.blastRadius = 6f;
                    stormController.waveMinInterval = 0.2f;
                    stormController.waveMaxInterval = 0.4f;
                    stormController.blastDamageCoefficient = stormDamageCoefficient;

                    NetworkServer.Spawn(stormController.gameObject);
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
