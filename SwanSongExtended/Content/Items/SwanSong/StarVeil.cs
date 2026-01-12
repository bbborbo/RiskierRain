using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class StarVeil : ItemBase<StarVeil>
    {
        public static float iframeDurationBase = 0.33f;
        public static float iframeDurationStack = 0.33f;
        public static float stormDamageCoefficient = 3f;
        public static int stormWavesBase = 3;
        public static int stormWavesStack = 2;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
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
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable, ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.CannotCopy };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/starVeil.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/starveil.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
        }
    }

    public class StarVeilBehavior : BaseItemBodyBehavior, IOnTakeDamageServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => StarVeil.instance.ItemsDef;

        void Start()
        {
            body?.healthComponent?.AddOnTakeDamageServerReceiver(this);
        }
        void OnDestroy()
        {
            body?.healthComponent?.RemoveOnTakeDamageServerReceiver(this);
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if (damageReport.damageInfo.damageType.damageType.HasFlag(DamageType.Silent))
                return;
            if (body.HasBuff(RoR2Content.Buffs.Immune) || body.HasBuff(RoR2Content.Buffs.HiddenInvincibility))
                return;

            float iframes = StarVeil.GetStackValue(StarVeil.iframeDurationBase, StarVeil.iframeDurationStack, stack);
            body.AddTimedBuffAuthority(RoR2Content.Buffs.Immune.buffIndex, iframes);
            bool selfDamage = false;// damageInfo.attacker == self.gameObject;

            if (damageReport.damageInfo.procCoefficient > 0 || selfDamage)
            {
                MeteorStormController stormController =
                    UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"),
                    body.corePosition, Quaternion.identity).GetComponent<MeteorStormController>();

                stormController.owner = body.gameObject;
                stormController.ownerDamage = body.damage;
                stormController.isCrit = Util.CheckRoll(body.crit, body.master);
                stormController.waveCount = (int)StarVeil.GetStackValue(StarVeil.stormWavesBase, StarVeil.stormWavesStack, stack);
                stormController.impactDelay = 1;// Mathf.Min(iframes, 2);
                stormController.blastRadius = 6f;
                stormController.waveMinInterval = 0.2f;
                stormController.waveMaxInterval = 0.4f;
                stormController.blastDamageCoefficient = StarVeil.stormDamageCoefficient;

                NetworkServer.Spawn(stormController.gameObject);
            }
        }
    }
}
