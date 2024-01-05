using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRainContent.Items
{
    class AtgMissileMk3 : ItemBase<AtgMissileMk3>
    {
        public static GameObject missilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/MissileProjectile");
        public float procCoefficient = 0.6f;
        public float procChance = 15;
        public float atgMk3BaseDamageCoefficientPerRocket = 3;
        static float atgMk3TotalDamageMultiplierBase = 0.75f;
        static float atgMk3TotalDamageMultiplierStack = 0.75f;
        string damagePerStack = Tools.ConvertDecimal(atgMk3TotalDamageMultiplierStack);
        string damageBase = Tools.ConvertDecimal(atgMk3TotalDamageMultiplierBase + atgMk3TotalDamageMultiplierStack);
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

        #region Abstract
        public override string ItemName => "AtG Missile Mk. 3";

        public override string ItemLangTokenName => "MISSILEMISSILEMISSILE";

        public override string ItemPickupDesc => "Chance to fire a volley of missiles.";

        public override string ItemFullDescription => $"<style=cIsDamage>{procChance}%</style> chance to fire a series of missiles on hit, " +
            $"that deal <style=cIsDamage>{damageBase}</style> <style=cStack>(+{damagePerStack} per stack)</style> TOTAL combined damage.";

        public override string ItemLore => "Fixing a spare bayonet onto his shotgun, he glanced at the horizon. " +
            "The thundering of footsteps big and small was growing louder and louder – they were nearly upon him. He went over his kit one last time.\n\n" +

            "Five bayonets. Twenty three packs of incendiary explosives. Fifteen magazines of armor-piercing ammunition. " +
            "Thirty three sticky bombs. Four tear-gas grenades, and so on.\n\n" +

            "And his favorite – two shoulder-mounted missile launchers, loaded with six AtG Viper Missiles. " +
            "Heat seeking, detonation power of 15 pounds of TNT per missile. Light-weight, and the best part – automatic firing mechanism. " +
            "He initially favored a more analog approach to his weapons, but the thing had grown on him.\n\n" +

            "Turning to face the oncoming mob, he loaded his shotgun. The adrenaline started pumping.\n\n" +

            "'Bring it on.'";

        /*"Order: AtG Missile Mk. 3" +
        "\nTracking Number: 162***********" +
        "\nEstimated Delivery: [REDACTED]" +
        "\nShipping Method: MILITARY" +
        "\nShipping Address: Belt Num.1053" +
        "\nShipping Details:\n" +
        "\nHow much more firepower does [REDACTED] need? For your sake, we revisited the last iteration of the Viper Missile System. Please do not ask us again - or we will come to [REDACTED] and take care of it ourselves. You do not want that, trust me.\n" +
        "\nWe kept most of the features of the Mk. 2 system, but it should be far more stable for field use. Consider it the best of both worlds.";*/

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/pickupmodels/PickupMissileLauncher");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/itemicons/texMissileLauncherIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;
        }

        public static void GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();
            CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.Missile);
        }

        public override void Hooks()
        {
            missilePrefab.GetComponent<ProjectileController>().procCoefficient = procCoefficient;
            RiskierRainContent.RetierItem(nameof(RoR2Content.Items.Missile));
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.GlobalEventManager.OnHitEnemy += AtgReworkLogic;
            IL.RoR2.GlobalEventManager.OnHitEnemy += RemoveVanillaAtgLogic;
            On.RoR2.BodyCatalog.Init += GetDisplayRules;
            On.RoR2.Items.ContagiousItemManager.Init += ChangeVoidShrimpPairing;
            ReworkPlasmaShrimp();
        }

        private void ChangeVoidShrimpPairing(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            List<ItemDef.Pair> newTransformationTable = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].ToList();

            ItemDef plasmaShrimp = DLC1Content.Items.MissileVoid;
            ItemDef atgOld = RoR2Content.Items.Missile;
            ItemDef atgNew = AtgMissileMk3.instance.ItemsDef;

            if (IsInTable(plasmaShrimp, atgOld, ref newTransformationTable, out ItemDef.Pair pair))
            {
                newTransformationTable.Remove(pair);
                newTransformationTable.Add(new ItemDef.Pair { itemDef1 = atgNew, itemDef2 = plasmaShrimp });

                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = newTransformationTable.ToArray();
            }
            orig();
        }

        private static bool IsInTable(ItemDef def, ItemDef transformation, ref List<ItemDef.Pair> newTransformationTable, out ItemDef.Pair pair)
        {
            pair = new ItemDef.Pair { itemDef1 = transformation, itemDef2 = def };
            return newTransformationTable.Contains(pair);
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
        #endregion Abstract

        #region Hooks
        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            self.AddItemBehavior<Mk3MissileBehavior>(GetCount(self));
        }

        private void RemoveVanillaAtgLogic(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Missile"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.Index--;
            c.Remove();
            c.Remove();
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, 0);
        }

        void AtgReworkLogic(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);


            if(damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;
                if (attackerBody)
                {
                    CharacterMaster attackerMaster = attackerBody.master;
                    if (attackerMaster != null)
                    {
                        TeamComponent teamComponent = attackerBody.GetComponent<TeamComponent>();
                        TeamIndex team = teamComponent ? teamComponent.teamIndex : TeamIndex.Neutral;

                        if (!damageInfo.procChainMask.HasProc(ProcType.Missile))
                        {
                            int missileItemCount = GetCount(attackerBody);
                            CreateMissiles(damageInfo, victim, attackerBody, attackerMaster, missileItemCount);
                        }
                    }
                }
            }
        }

        private void CreateMissiles(DamageInfo damageInfo, GameObject victim, CharacterBody attackerBody, CharacterMaster attackerMaster, int missileItemCount)
        {
            Mk3MissileBehavior missileLauncher = attackerBody.gameObject.GetComponent<Mk3MissileBehavior>();

            if (missileLauncher != null && missileItemCount > 0)
            {
                float atgTotalDamage = damageInfo.damage * (atgMk3TotalDamageMultiplierBase + atgMk3TotalDamageMultiplierStack * missileItemCount);
                float atgDamagePerRocket = atgMk3BaseDamageCoefficientPerRocket * attackerBody.damage;
                float atgDamageRemainder = atgTotalDamage % atgDamagePerRocket;
                int atgTotalMissiles = (int)((atgTotalDamage - atgDamageRemainder) / atgDamagePerRocket);


                float finalRocketProcChanceFraction = atgDamageRemainder / atgDamagePerRocket;
                float rollMultiplier = 1;
                if (atgTotalMissiles < 1)
                    rollMultiplier = finalRocketProcChanceFraction;

                if (Util.CheckRoll(rollMultiplier * procChance * damageInfo.procCoefficient, attackerMaster))
                {
                    if (rollMultiplier < 1)
                    {
                        atgTotalMissiles++;
                    }
                    else if (Util.CheckRoll(finalRocketProcChanceFraction * procChance * damageInfo.procCoefficient, attackerMaster))
                    {
                        atgTotalMissiles++;
                    }

                    if (atgTotalMissiles > 0)
                    {
                        int currentMissiles = missileLauncher.currentMissiles.Count;
                        List<FireProjectileInfo> missilesToFire = new List<FireProjectileInfo>();
                        for (int i = 0; i < atgTotalMissiles; i++)
                        {
                            FireProjectileInfo newMissile = NewMissile(atgMk3BaseDamageCoefficientPerRocket, damageInfo, attackerBody, victim);

                            missilesToFire.Add(newMissile);
                            if (missilesToFire.Count + currentMissiles > 75)
                            {
                                int remainingMissiles = missilesToFire.Count + currentMissiles - 75;
                                Debug.Log($"Discarded {remainingMissiles} missiles!");
                                break;
                            }
                        }

                        missileLauncher.SetMissiles(missilesToFire);
                    }
                }
            }
        }
        public static FireProjectileInfo NewMissile(float damage, DamageInfo damageInfo, CharacterBody attackerBody, GameObject victim)
        {
            GameObject gameObject = attackerBody.gameObject;
            InputBankTest component = gameObject.GetComponent<InputBankTest>();
            Vector3 position = component ? component.aimOrigin : gameObject.transform.position;
            Vector3 up = Vector3.up;
            float rotationVariance = UnityEngine.Random.Range(0.1f, 0.5f); //0.1f

            float rocketDamage = attackerBody.damage * damage;
            ProcChainMask procChainMask2 = damageInfo.procChainMask;
            procChainMask2.AddProc(ProcType.Missile);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = missilePrefab,
                position = position,
                rotation = Util.QuaternionSafeLookRotation(up + UnityEngine.Random.insideUnitSphere * rotationVariance),
                procChainMask = procChainMask2,
                owner = gameObject,
                damage = rocketDamage,
                crit = damageInfo.crit,
                force = 200f,
                damageColorIndex = DamageColorIndex.Item,
                target = victim
            };

            return (fireProjectileInfo);
        }
        #endregion

        #region plasma shrimp
        float shrimpShieldBase = 40;
        float shrimpDamageCoeffBase = 0.09f;
        float shrimpDamageCoeffStack = 0.04f;
        public void ReworkPlasmaShrimp()
        {
            IL.RoR2.GlobalEventManager.OnHitEnemy += ShrimpRework;
            GetStatCoefficients += ShrimpShieldFix;

            LanguageAPI.Add("ITEM_MISSILEVOID_PICKUP", "While you have shield, fire missiles on every hit. <style=cIsVoid>Corrupts all AtG Missile Mk. 3s</style>.");
            LanguageAPI.Add("ITEM_MISSILEVOID_DESC",
                $"Gain <style=cIsHealing>{shrimpShieldBase} shield</style>. " +
                $"While you have a <style=cIsHealing>shield</style>, " +
                $"hitting an enemy fires <style=cIsDamage>3</style> missiles that each deal " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(shrimpDamageCoeffBase)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(shrimpDamageCoeffStack)} per stack)</style> TOTAL damage. " +
                $"<style=cIsVoid>Corrupts all AtG Missile Mk. 3s</style>.");
        }

        private void ShrimpShieldFix(CharacterBody sender, StatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv && inv.GetItemCount(DLC1Content.Items.MissileVoid) > 0)
            {
                args.shieldMultAdd -= 0.1f;
                args.baseShieldAdd += shrimpShieldBase;
            }
        }

        private void ShrimpRework(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //jump to shrimp implementation
            int shrimpLoc = 32;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MissileVoid"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out shrimpLoc)
                );

            /*int dmgLoc = 37;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdloc(shrimpLoc),
                x => x.MatchConvR4(),
                x => x.MatchMul(),
                x => x.MatchStloc(out dmgLoc)
                );*/

            //inject our new damage coefficient
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnHitProcDamage))
                );
            c.Emit(OpCodes.Ldloc, shrimpLoc);
            c.EmitDelegate<Func<float, int, float>>((damageCoefficient, itemCount) =>
            {
                float damageOut = shrimpDamageCoeffBase + (shrimpDamageCoeffStack * (itemCount - 1));
                return damageOut;
            });

            //force it to fire 3
            c.GotoNext(MoveType.Before,
                x => x.MatchLdloc(out _),
                x => x.MatchLdcI4(out _),
                x => x.MatchBgt(out _)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, 1);
        }
        #endregion
    }
    public class Mk3MissileBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public List<FireProjectileInfo> currentMissiles = new List<FireProjectileInfo>(0);

        float missileMaxTimer = 0.075f;
        float currentMissileTimer = 0;
        float missileSpread = 0;
        float missileSpreadFraction = 0.33f;
        float missileSpreadMax = 0.6f;

        public void SetMissiles(List<FireProjectileInfo> newMissiles, bool replace = false)
        {
            if (replace == true)
            {
                currentMissiles = new List<FireProjectileInfo>(newMissiles);
            }
            else
            {
                for (int i = 0; i < newMissiles.Count; i++)
                {
                    currentMissiles.Add(newMissiles[i]);
                }
            }
            currentMissileTimer += GetScaledDelay();
        }

        private void FixedUpdate()
        {
            if (currentMissiles.Count > 0 && stack > 0)
            {
                while (currentMissileTimer <= 0f)
                {
                    FireProjectileInfo missile = currentMissiles[0];
                    missile.position = body.gameObject.transform.position;
                    missile.rotation = Util.QuaternionSafeLookRotation(Vector3.up + UnityEngine.Random.insideUnitSphere * missileSpread);
                    missileSpread += (missileSpreadMax - missileSpread) * missileSpreadFraction;

                    ProjectileManager.instance.FireProjectile(missile);

                    List<FireProjectileInfo> newMissileList = new List<FireProjectileInfo>(currentMissiles);
                    newMissileList.RemoveAt(0);
                    //Debug.Log(newMissileList.Count);
                    SetMissiles(newMissileList, true);
                }

                if (this.currentMissileTimer > 0f)
                {
                    currentMissileTimer -= Time.fixedDeltaTime;
                }
            }
            else
            {
                currentMissileTimer = 0;
                missileSpread = 0;
            }
        }
        private float GetScaledDelay()
        {
            return missileMaxTimer / body.attackSpeed;
        }
    }
}