﻿using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class WeirdlyShapedOpal : ItemBase<WeirdlyShapedOpal>
	{
		#region
		public override string ConfigName => "Reworks : Opal";
		[AutoConfig("Armor Increase Unconditional", 10)]
        public static int opalArmorBase = 10;
		[AutoConfig("Regen Increase Unconditional", "Scales with level", 1f)]
		public static float opalRegenBase = 1f;
		[AutoConfig("Armor Increase Per Buff", 5)]
		public static int opalArmorPerBuff = 5;
		[AutoConfig("Regen Increase Per Buff", "Scales with level", 0.5f)]
		public static float opalRegenPerBuff = 0.5f;
		[AutoConfig("Max Opal Buff", 5)]
		public static int opalMaxBuff = 5;
		[AutoConfig("Opal Area Radius", 20)]
		public static float opalAreaRadius = 20;
        #endregion
        public static float opalAreaRadiusSqr => opalAreaRadius * opalAreaRadius;
		public static BuffDef opalStatBuff;
		public static GameObject opalAreaIndicator = null;
		public override AssetBundle assetBundle => null;

        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

		public override string ItemName => "Weirdly-shaped Opal";

        public override string ItemLangTokenName => "BORBOOPAL";

        public override string ItemPickupDesc => "Increases armor and regen while enemies are nearby.";

        public override string ItemFullDescription => $"Increases base health regeneration by " +
			$"{HealingColor($"+{opalRegenBase} hp/s")} {StackText($"+{opalRegenBase} hp/s")} " +
			$"and armor by {HealingColor($"+{opalArmorBase}")} {StackText("+" + opalArmorBase)}. " +
			$"For each enemy within {UtilityColor(opalAreaRadius.ToString() +"m")}, also gain " +
			$"{HealingColor($"+{opalRegenPerBuff} hp/s")} {StackText($"+{opalRegenPerBuff} hp/s")} " +
			$"base health regeneration and {HealingColor($"+{opalArmorPerBuff}")} {StackText("+" + opalArmorPerBuff)}" +
			$" armor, up to {UtilityColor(opalMaxBuff.ToString())} times.";

        public override string ItemLore => "<style=cMono>//--AUTO-TRANSCRIPTION FROM UES [Redacted] --//</style>\n\n\"...You think this planet is as bad as they say?\"\n\nLiz sat in silence among her fellow soldiers. She, like the rest of her squadron, had been taken from the middle of a firefight in the galactic outback and brought before an old UES veteran. Liz was used to debriefing by now, it all blended together in her head. Something about monsters, and the missing UES Contact Light.\n\n\"...Dunno.\" Liz murmured as she turned a small, oddly-shaped hunk of opal in her hand. It was one of the only things she had that reminded her of Parker. Of when things were calm, and peaceful.\n\n\"...Heh, I don't think a shiny rock will do much,\" A soldier joked. Liz's brow furrowed under her helmet. \"Yeah, probably not... but...\"\n\nLiz took a deep breath and slipped the opal back into her pocket. \"It just helps.\"";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/pickupmodels/PickupOddlyShapedOpal");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/itemicons/texOddlyShapedOpalIcon");
		public override ExpansionDef RequiredExpansion => SotvExpansionDef();

		public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
			return IDR;
        }

        public override void Hooks()
		{
			BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Items.OutOfCombatArmor);
			On.RoR2.CharacterBody.OnInventoryChanged += AddOpalItemBehavior;
			GetStatCoefficients += OpalStatCoefficients;
		}
        public override void Init()
        {
            base.Init();
			SwanSongPlugin.RetierItem(Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/OutOfCombatArmor/OutOfCombatArmor.asset").WaitForCompletion());
			CreateAssets();
		}

        private void CreateAssets()
		{
			opalStatBuff = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/OutOfCombatArmor/bdOutOfCombatArmorBuff.asset").WaitForCompletion();
			opalStatBuff.canStack = true;

			opalAreaIndicator = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/NearbyDamageBonusIndicator").InstantiateClone("OpalAreaIndicator", true);
			Transform transform = opalAreaIndicator.transform.Find("Radius, Spherical");
			transform.localScale = Vector3.one * opalAreaRadius * 2f;
			/*MaterialControllerComponents.HGIntersectionController hgintersectionController = transform.gameObject.AddComponent<MaterialControllerComponents.HGIntersectionController>();
			MeshRenderer component = transform.GetComponent<MeshRenderer>();
			hgintersectionController.renderer = component;
			Material original = SpikestripContentBase.AddressablesLoad<Material>("d0eb35f70367cdc4882f3bb794b65f2b");
			Material material = Object.Instantiate<Material>(original);
			material.SetTexture("_RemapTex", SpikestripContentBase.AddressablesLoad<Texture>("385005992afbfce4089807386adc07b0"));
			material.SetColor("_TintColor", SpikestripContentBase.ColorRGB(50f, 82f, 115f, 1f));
			component.material = material;
			hgintersectionController.material = material;
			material.SetFloat("_BrightnessBoost", 0.1f);*/
			Content.AddNetworkedObjectPrefab(opalAreaIndicator);
		}

        private void OpalStatCoefficients(CharacterBody sender, StatHookEventArgs args)
		{
			int itemCount = GetCount(sender);
			int buffCount = sender.GetBuffCount(opalStatBuff);

			args.armorAdd += itemCount * ((buffCount * opalArmorPerBuff) + opalArmorBase);
			args.baseRegenAdd += itemCount * ((buffCount * opalRegenPerBuff) + opalRegenBase) * (1 + (0.2f * sender.level));
		}

		private void AddOpalItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
		{
			orig(self);
            if (NetworkServer.active)
			{
				self.AddItemBehavior<WeirdOpalBehavior>(GetCount(self));
			}
		}
	}
	public class WeirdOpalBehavior : CharacterBody.ItemBehavior
	{
		GameObject areaIndicatorInstance;
		public float frequency = 6;
		float interval => 1 / frequency;
		float timer;
		public void FixedUpdate()
		{
			timer += Time.fixedDeltaTime;
			if (timer >= interval)
			{
				timer -= interval;
				TeamIndex alliedTeamIndex = this.body.teamComponent.teamIndex;
				int nearbyEnemyCount = 0;
				for (TeamIndex team = TeamIndex.Neutral; team < TeamIndex.Count; team++)
				{
					if (team != alliedTeamIndex && team > TeamIndex.Neutral && nearbyEnemyCount < WeirdlyShapedOpal.opalMaxBuff)
					{
						foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(team))
						{
							Vector3 distanceVector = teamComponent.transform.position - this.body.corePosition;
							if (distanceVector.sqrMagnitude <= WeirdlyShapedOpal.opalAreaRadiusSqr)
							{
								nearbyEnemyCount++;
								if(nearbyEnemyCount >= WeirdlyShapedOpal.opalMaxBuff)
                                {
									break;
                                }
							}
						}
					}
				}
				this.SetBuffCount(nearbyEnemyCount);
			}
		}

		public void SetBuffCount(int nearbyEnemies)
		{
			int buffCount = this.body.GetBuffCount(WeirdlyShapedOpal.opalStatBuff);

			if (buffCount != nearbyEnemies)
			{
				bool flag2 = buffCount < nearbyEnemies;
				if (flag2)
				{
					for (int i = 0; i < nearbyEnemies - buffCount; i++)
					{
						this.body.AddBuff(WeirdlyShapedOpal.opalStatBuff);
					}
				}
				else
				{
					bool flag3 = buffCount > nearbyEnemies;
					if (flag3)
					{
						for (int j = 0; j < buffCount - nearbyEnemies; j++)
						{
							this.body.RemoveBuff(WeirdlyShapedOpal.opalStatBuff);
						}
					}
				}
			}
		}
		private void Start()
		{
			this.indicatorEnabled = true;
		}
		private void OnDestroy()
		{
			this.indicatorEnabled = false;
		}
		private bool indicatorEnabled
		{
			get
			{
				return this.areaIndicatorInstance;
			}
			set
			{
				if (this.indicatorEnabled == value)
				{
					if (value)
					{
						this.areaIndicatorInstance = Instantiate<GameObject>(WeirdlyShapedOpal.opalAreaIndicator, this.body.corePosition, Quaternion.identity);
						this.areaIndicatorInstance.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject, null);
						//this.areaIndicatorInstance.transform.localScale = Vector3.one * WeirdlyShapedOpal.opalAreaRadius;
					}
					else
					{
						Destroy(this.areaIndicatorInstance);
						this.areaIndicatorInstance = null;
					}
				}
			}
		}
	}
}
