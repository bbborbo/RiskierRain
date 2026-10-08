using BepInEx.Configuration;
using R2API;
using RainrotSharedUtils;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.GivePickupsOnStart;

namespace SwanSongExtended.Scavengers
{
    public abstract class TwistedScavengerBase<T> : TwistedScavengerBase where T : TwistedScavengerBase<T>
    {
        public static T instance { get; private set; }

        public TwistedScavengerBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class TwistedScavengerBase : SharedBase
    {
        public static string baseTscavTokenName = "SwansongScavenger";
        MultiCharacterSpawnCard twistedScavengerSpawnCard = LegacyResourcesAPI.Load<MultiCharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscScavLunar");

        public string scavFullName => string.IsNullOrWhiteSpace(ScavFullNameOverride) ? $"{ScavName} the {ScavTitle}" : ScavFullNameOverride;
        public abstract string ScavName { get; }
        public abstract string ScavTitle { get; }
        public abstract string ScavLangTokenName { get; }
        public abstract string ScavEquipName { get; } //Can be "" if no equipment is desired
        public virtual List<ItemDefInfo> ItemDefInfos { get; set; } = new List<ItemDefInfo>() { };
        public virtual List<ItemInfo> ItemInfos { get; set; } = new List<ItemInfo>() { };
        public virtual string ScavFullNameOverride { get; set; } = ""; //Only use if you do not wish to use the "ScavName the ScavTitle" format
        public abstract ExpansionDef RequiredExpansion { get; }
        public abstract float SelectionWeight { get; }
        public override string ConfigName => "Scavengers : " + scavFullName;
        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;

        public GameObject ScavObject;
        public CharacterBody ScavBody;

        public abstract void PopulateItemInfos();
        public override void Init()
        {
            PopulateItemInfos();
            GenerateTwistedScavenger();
        }
        public override void Lang()
        {

        }
        public override void Hooks()
        {

        }

        internal void AddItemDefInfo(ItemDef itemDef, int count)
        {
            if (count <= 0)
                return;
            ItemDefInfo itemInfo = new ItemDefInfo();

            itemInfo.itemDef = itemDef;
            itemInfo.count = count;

            ItemDefInfos.Add(itemInfo);
        }

        internal void AddItemInfo(string name, int count)
        {
            if (count <= 0)
                return;
            ItemInfo itemInfo = new ItemInfo();

            itemInfo.itemString = name;
            itemInfo.count = count;

            ItemInfos.Add(itemInfo);
        }

        internal void GenerateTwistedScavenger()
        {
            Log.Debug("Generating Twisted Scavenger: " + scavFullName);
            LanguageAPI.Add(ScavLangTokenName.ToUpper() + "_NAME", scavFullName);

            CharacterMaster master = TwistedScavengerUtils.CreateNewScavengerMaster(ScavLangTokenName, out ScavBody);
            ScavObject = master.gameObject;

            Content.AddMasterPrefab(ScavObject);
            Content.AddCharacterBodyPrefab(ScavBody.gameObject);

            GivePickupsOnStart pickupComp = ScavObject.AddComponent<GivePickupsOnStart>();
            pickupComp.itemDefInfos = ItemDefInfos.ToArray();
            pickupComp.itemInfos = ItemInfos.ToArray();
            if (string.IsNullOrWhiteSpace(ScavEquipName) == false)
            {
                pickupComp.equipmentString = ScavEquipName;
            }

            CustomScavengers.AddScavengerMaster(master.gameObject, SelectionWeight, RequiredExpansion);
        }
    }
}
