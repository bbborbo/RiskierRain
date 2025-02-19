using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Items
{
    class Aglet : ItemBase<Aglet>
    {
        //public override bool lockEnabled => true;
        public override bool isEnabled => false;
        #region config
        public static float baseSpeedBuff = 0.30f;
        public static float stackSpeedBuff = 0.30f;
        public const float maxGroundTimeForMaxBuff = 5;
        public const float minGroundTimeForMinBuff = 1;
        public const float buffPerSecond = 5;
        public const float ungroundedLossRate = 1;
        #endregion

        public static BuffDef agletSpeed;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Aglet";

        public override string ItemLangTokenName => "AGLET";

        public override string ItemPickupDesc => "Increase movement speed while grounded.";

        public override string ItemFullDescription => $"Your movement speed gradually increases while grounded, " +
            $"up to {Tools.ConvertDecimal(baseSpeedBuff)} " +
            $"(+{Tools.ConvertDecimal(stackSpeedBuff)} per stack) " +
            $"after {maxGroundTimeForMaxBuff} seconds.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            agletSpeed = Content.CreateAndAddBuff(
                "bdAgletSpeed",
                assetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png"), //replace me
                new Color(0.9f, 0.9f, 0.2f),
                true, false
                );
            base.Init();
        }

        public override void Hooks()
        {
            GetStatCoefficients += AgletSpeedBuff;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }

        private void AgletSpeedBuff(CharacterBody sender, StatHookEventArgs args)
        {
            int agletCount = GetCount(sender);
            int buffCount = sender.GetBuffCount(agletSpeed);
            if (buffCount > 0 && agletCount > 0)
            {
                float maxSpeedBonus = baseSpeedBuff + stackSpeedBuff * (agletCount - 1);
                float fraction = buffCount / ((maxGroundTimeForMaxBuff - minGroundTimeForMinBuff) * buffPerSecond);
                float speedBonus = maxSpeedBonus * fraction;
                args.moveSpeedMultAdd += speedBonus;
            }
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<AgletItemBehavior>(GetCount(self));
            }
        }
    }
    public class AgletItemBehavior : CharacterBody.ItemBehavior
    {
        CharacterMotor motor;
        int buffCount;
        float timeGrounded;

        void Start()
        {
            motor = body.characterMotor;
        }

        private void FixedUpdate()
        {
            if (NetworkServer.active && motor != null)
            {
                if (motor.isGrounded)
                {
                    if (timeGrounded < Aglet.maxGroundTimeForMaxBuff)
                    {
                        timeGrounded = Mathf.Min(timeGrounded + Time.fixedDeltaTime, Aglet.maxGroundTimeForMaxBuff);
                    }
                }
                else if (timeGrounded > 0)
                {
                    timeGrounded = Mathf.Max(timeGrounded - Time.fixedDeltaTime, 0);
                }

                int targetBuffCount = Mathf.Max(Mathf.CeilToInt((timeGrounded - Aglet.minGroundTimeForMinBuff) * Aglet.buffPerSecond), 0);
                if (targetBuffCount > buffCount)
                {
                    this.body.AddBuff(Aglet.agletSpeed);
                    buffCount++;
                }
                else if (targetBuffCount < buffCount)
                {
                    this.body.RemoveBuff(Aglet.agletSpeed);
                    buffCount--;
                }
            }
        }

        private void OnDisable()
        {
            while(body.HasBuff(Aglet.agletSpeed))
                this.body.RemoveBuff(Aglet.agletSpeed);
        }
    }
}
