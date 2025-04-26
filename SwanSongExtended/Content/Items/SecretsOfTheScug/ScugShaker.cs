using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using UnityEngine;
using static SwanSongExtended.Modules.Language.Styling;
using static MoreStats.OnHit;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using RoR2.Artifacts;
using static R2API.DamageAPI;
using HarmonyLib;

namespace SwanSongExtended.Items
{
    class ScugShaker : ItemBase<ScugShaker>
    {

        #region config
        [AutoConfig("Damage Base", 8)]
        public static float damageBase = 2.5f;
        [AutoConfig("Damage Stack", 4)]
        public static float damageStack = 2.5f;
        public override string ConfigName => "Item: " + ItemName;
        #endregion
        #region Abstract
        public override string ItemName => "Scug Shaker";

        public override string ItemLangTokenName => "SCUGSHAKER";

        public override string ItemPickupDesc => $"High damage attacks store {DamageColor("Scugs.")} When struck, shake stored scugs, {DamageColor("damaging and chilling enemies.")} {VoidColor("Corrupts all Pear Wigglers.")}";

        public override string ItemFullDescription => $"Attacks dealing 400% or more damage store up to 3 {StackText("+ 3")}{DamageColor("Scugs.")} When struck, release 1 scug per 5% max hp lost for {DamageValueText(damageBase)} {StackText($" + {ConvertDecimal(damageBase)}")} and {UtilityColor("chilling enemies.")} {VoidColor("Corrupts all Pear Wigglers.")}";

        public override string ItemLore => "Shake 'em and sic 'em.";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.Utility };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlScugShaker.prefab");

        public override Sprite ItemIcon => LoadItemIcon();
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSOTS;
        #endregion
        public static BuffDef storedScugBuff;
        GameObject scugBomb;
        float pearLifetime = 10;
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += ScugShakerTakeDamage;
            GetHitBehavior += ScugShakerOnHit;
            storedScugBuff = Content.CreateAndAddBuff("StoredScugBuff", Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(), Color.black, true, false);
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }


        private void CreateScug()
        {
            scugBomb = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bomb/SpiteBomb.prefab").WaitForCompletion().InstantiateClone("ScugBomb", true);

            ModdedDamageTypeHolderComponent mdthc = scugBomb.AddComponent<ModdedDamageTypeHolderComponent>();
            if (mdthc)
            {
                mdthc.Add(ChillRework.ChillRework.ChillOnHit);
            }
        }
        private void ScugShakerOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (damageInfo.damage / attackerBody.damage < 4)
            {
                return;
            }
            int scugCount = GetCount(attackerBody);
            if (scugCount <= 0 || !NetworkServer.active)
            {
                return;
            }
            if (attackerBody.GetBuffCount(storedScugBuff) < scugCount * 3)//make this not hardcoded idgaf//for testing
                attackerBody.AddBuff(storedScugBuff);
        }

        private void ScugShakerTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            CharacterBody body = self?.body;
            int itemCount = GetCount(body);
            if (itemCount <= 0)
            {
                return;
            }
            if (self.body.GetBuffCount(storedScugBuff) <= 0)
            {
                return;
            }
            int percentLost = (int)Math.Round((damageInfo.damage / self.fullCombinedHealth) * 20);
            ShakeScugs(itemCount--, percentLost, body, self);
        }

        private void ShakeScugs(int a, int b, CharacterBody body, HealthComponent healthComponent)
        {
            for (int i = 0; i < b && body.GetBuffCount(storedScugBuff) > 0; i++)//a=itemcount -1; b=%hp lost
            {
                ShakeScug(a, healthComponent);
                body.RemoveBuff(storedScugBuff);
            }

        }


        private void ShakeScug(int i, HealthComponent a)
        {
            Vector3 spawnPosition = a.body.corePosition + UnityEngine.Random.insideUnitSphere * a.body.radius * 5;
            //stolen code. revisit
            Vector3 b = UnityEngine.Random.insideUnitSphere * (BombArtifactManager.bombSpawnBaseRadius + a.body.bestFitRadius * BombArtifactManager.bombSpawnRadiusCoefficient);
            float velocityY = UnityEngine.Random.Range(5f, 25f);

            Ray ray = new Ray(spawnPosition + b + new Vector3(0f, BombArtifactManager.maxBombStepUpDistance, 0f), Vector3.down);
            float maxDistance = BombArtifactManager.maxBombStepUpDistance + BombArtifactManager.maxBombFallDistance;
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                if (spawnPosition.y < raycastHit.point.y + 4f)
                {
                    spawnPosition.y = raycastHit.point.y + 4f;
                }
                Vector3 raycastOrigin = spawnPosition + b;
                raycastOrigin.y = raycastHit.point.y;


                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(scugBomb, spawnPosition, UnityEngine.Random.rotation);
                SpiteBombController component = gameObject.GetComponent<SpiteBombController>();
                DelayBlast delayBlast = component.delayBlast;
                TeamFilter component2 = gameObject.GetComponent<TeamFilter>();
                component.bouncePosition = raycastOrigin;
                component.initialVelocityY = velocityY;
                delayBlast.position = spawnPosition;
                delayBlast.baseDamage = damageBase + damageStack * i;
                delayBlast.baseForce = 2300f;
                delayBlast.attacker = a.gameObject;
                delayBlast.radius = 7f;// BombArtifactManager.bombBlastRadius;
                delayBlast.crit = a.body.RollCrit();
                delayBlast.procCoefficient = 0.75f;
                delayBlast.maxTimer = 8f;//BombArtifactManager.bombFuseTimeout;
                delayBlast.timerStagger = 0f;
                delayBlast.falloffModel = BlastAttack.FalloffModel.None;
                component2.teamIndex = a.body.teamComponent.teamIndex;
                NetworkServer.Spawn(gameObject);
            }

            
        }

        public override void Init()
        {
            CreateScug();
            base.Init();
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = PearWiggler.instance.ItemsDef, //consumes pear wiggler
                itemDef2 = ScugShaker.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}

