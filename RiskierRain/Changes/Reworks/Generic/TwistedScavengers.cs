using RiskierRain.CoreModules;
using RiskierRain.Items;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.GivePickupsOnStart;

namespace RiskierRain
{
    partial class RiskierRainPlugin
    {
        string baseTscavTokenName = "BorboTscav";
        MultiCharacterSpawnCard twistedScavengerSpawnCard = LegacyResourcesAPI.Load<MultiCharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscScavLunar");

        void DoSpeedScavenger()
        {
            List<ItemInfo> itemInfos = new List<ItemInfo>();

            //white
            AddItemInfo(ref itemInfos, RoR2Content.Items.Hoof.name, 30);
            AddItemInfo(ref itemInfos, RoR2Content.Items.SprintBonus.name, 5);
            AddItemInfo(ref itemInfos, RoR2Content.Items.BoostAttackSpeed.name, 7); //3

            //green
            AddItemInfo(ref itemInfos, RoR2Content.Items.Feather.name, 2);
            AddItemInfo(ref itemInfos, RoR2Content.Items.JumpBoost.name, 2);
            AddItemInfo(ref itemInfos, RoR2Content.Items.EquipmentMagazine.name, 3);

            //red
            AddItemInfo(ref itemInfos, RoR2Content.Items.ExtraLife.name, 2);

            //yellow

            //lunar
            AddItemInfo(ref itemInfos, RoR2Content.Items.LunarSecondaryReplacement.name, 1);
            AddItemInfo(ref itemInfos, RoR2Content.Items.LunarUtilityReplacement.name, 1);
            AddItemInfo(ref itemInfos, RoR2Content.Items.AutoCastEquipment.name, 0);

            //NinjaGear.instance.EquipDef.name
            //RoR2Content.Equipment.FireBallDash.name
            //RoR2Content.Equipment.Jetpack.name
            CharacterBody body = GenerateTwistedScavenger(itemInfos.ToArray(), "Baba the Enlightened", "Swift", RoR2Content.Equipment.Jetpack.name);
            body.baseMaxHealth *= 0.3f;
            body.levelMaxHealth = body.baseMaxHealth * 0.3f;
        }

        void DoBoboScavenger()
        {
            List<ItemInfo> itemInfos = new List<ItemInfo>();

            //white
            AddItemInfo(ref itemInfos, RoR2Content.Items.PersonalShield.name, 0);
            //AddItemInfo(ref itemInfos, RoR2Content.Items.IgniteOnKill.name, 1); 

            //green
            AddItemInfo(ref itemInfos, FrozenShell.instance.ItemsDef.name, 1);
            AddItemInfo(ref itemInfos, FlowerCrown.instance.ItemsDef.name, 1);
            AddItemInfo(ref itemInfos, BirdBand.instance.ItemsDef.name, 0);
            AddItemInfo(ref itemInfos, UtilityBelt.instance.ItemsDef.name, 1);
            AddItemInfo(ref itemInfos, ChefReference.instance.ItemsDef.name, 1);

            //red
            AddItemInfo(ref itemInfos, RoR2Content.Items.BarrierOnOverHeal.name, 3);

            //yellow
            AddItemInfo(ref itemInfos, RoR2Content.Items.Pearl.name, 2);
            AddItemInfo(ref itemInfos, RoR2Content.Items.ShinyPearl.name, 2);

            //lunar
            AddItemInfo(ref itemInfos, RoR2Content.Items.RandomDamageZone.name, 3);
            //AddItemInfo(ref itemInfos, RoR2Content.Items.LunarBadLuck.name, 1);


            CharacterBody body = GenerateTwistedScavenger(itemInfos.ToArray(), "Bobo the Unbreakable", "Unstoppable", RoR2Content.Equipment.GainArmor.name);
            body.baseDamage *= 0.5f;
            body.levelDamage = body.baseDamage * 0.2f;
            body.baseAttackSpeed = 0.7f;
            body.baseMoveSpeed = 2f;
        }

        void DoGreedyScavenger() //economy
        {
            List<ItemInfo> itemInfos = new List<ItemInfo>();

            //white
            AddItemInfo(ref itemInfos, RoR2Content.Items.Bear.name, 5);
            AddItemInfo(ref itemInfos, RoR2Content.Items.SecondarySkillMagazine.name, 4);

            //green
            AddItemInfo(ref itemInfos, RoR2Content.Items.Bandolier.name, 1);
            AddItemInfo(ref itemInfos, RoR2Content.Items.BonusGoldPackOnKill.name, 5);

            //AddItemInfo(ref itemInfos, CoinGun.instance.ItemsDef.name, 1);

            //red
            AddItemInfo(ref itemInfos, RoR2Content.Items.UtilitySkillMagazine.name, 1);

            //yellow
            //AddItemInfo(ref itemInfos, RoR2Content.Items.BleedOnHitAndExplode.name, 0);

            //lunar
            AddItemInfo(ref itemInfos, RoR2Content.Items.GoldOnHit.name, 10);


            GenerateTwistedScavenger(itemInfos.ToArray(), "Gibgib the Greedy", "Greedy", RoR2Content.Equipment.GoldGat.name);
        }

        void DoSadistScavenger() //damage
        {
            List<ItemInfo> itemInfos = new List<ItemInfo>();

            //white
            AddItemInfo(ref itemInfos, RoR2Content.Items.CritGlasses.name, 0); //3
            AddItemInfo(ref itemInfos, RoR2Content.Items.BleedOnHit.name, 4); //debuff
            AddItemInfo(ref itemInfos, RoR2Content.Items.BoostAttackSpeed.name, 0); //3
            AddItemInfo(ref itemInfos, RoR2Content.Items.IgniteOnKill.name, 1); //1

            //green
            AddItemInfo(ref itemInfos, RoR2Content.Items.DeathMark.name, 3);
            AddItemInfo(ref itemInfos, RoR2Content.Items.SlowOnHit.name, 2); //debuff
            AddItemInfo(ref itemInfos, RoR2Content.Items.WarCryOnMultiKill.name, 2);

            AddItemInfo(ref itemInfos, ChefReference.instance.ItemsDef.name, 2); //debuff

            //red
            AddItemInfo(ref itemInfos, RoR2Content.Items.ArmorReductionOnHit.name, 2); //debuff
            AddItemInfo(ref itemInfos, RoR2Content.Items.Talisman.name, 0);
            AddItemInfo(ref itemInfos, RoR2Content.Items.Icicle.name, 2); //debuff

            //yellow
            AddItemInfo(ref itemInfos, RoR2Content.Items.BleedOnHitAndExplode.name, 0); //1, debuff

            //lunar
            AddItemInfo(ref itemInfos, RoR2Content.Items.RandomDamageZone.name, 0);
            AddItemInfo(ref itemInfos, RoR2Content.Items.AutoCastEquipment.name, 1);

            CharacterBody body = GenerateTwistedScavenger(itemInfos.ToArray(), "Chipchip the Wicked", "Sadist", RoR2Content.Equipment.CrippleWard.name); //effigy debuff
            body.baseDamage *= 0.3f;
            body.levelDamage = body.baseDamage * 0.2f;
        }

        void AddItemInfo(ref List<ItemInfo> itemInfos, string name, int count)
        {
            if (count <= 0)
                return;
            ItemInfo itemInfo = new ItemInfo();
            
            itemInfo.itemString = name;
            itemInfo.count = count;

            itemInfos.Add(itemInfo);
        }

        CharacterBody GenerateTwistedScavenger(ItemInfo[] itemInfos, string fullName, string nameToken, string equipmentName = "")
        {
            Debug.Log("Generating Twisted Scavenger: " + fullName);
            nameToken = baseTscavTokenName + nameToken;
            LanguageAPI.Add(nameToken, fullName);

            GameObject masterObject = LegacyResourcesAPI.Load<GameObject>("prefabs/charactermasters/ScavLunar1Master").InstantiateClone($"{nameToken}Master", true);
            GameObject bodyObject = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/ScavLunar1Body").InstantiateClone($"{nameToken}Body", true);

            CharacterMaster master = masterObject.GetComponent<CharacterMaster>();
            master.bodyPrefab = bodyObject;
            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.baseNameToken = nameToken;

            int count = twistedScavengerSpawnCard.masterPrefabs.Length;
            Array.Resize<GameObject>(ref twistedScavengerSpawnCard.masterPrefabs, count + 1);
            twistedScavengerSpawnCard.masterPrefabs[count] = masterObject;


            foreach (GivePickupsOnStart gpos in masterObject.GetComponents<GivePickupsOnStart>())
            {
                gpos.enabled = false;
            }

            GivePickupsOnStart pickupComp = masterObject.AddComponent<GivePickupsOnStart>();
            pickupComp.itemInfos = itemInfos;
            if (equipmentName != "")
            {
                pickupComp.equipmentString = equipmentName;// "GoldGat";
            }

            Assets.bodyPrefabs.Add(bodyObject);
            Assets.masterPrefabs.Add(masterObject);
            return body;
        }
    }
}
