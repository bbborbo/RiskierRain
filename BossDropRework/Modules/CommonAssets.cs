using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BossDropRework.Modules
{
    public static class CommonAssets
    {
        public static BuffDef NoBossDropsBuff;
        public static BuffDef YesBossDropsBuff;
        public static void Init()
        {
            CreateForceDropsBuffs();
        }

        private static void CreateForceDropsBuffs()
        {
            NoBossDropsBuff = ScriptableObject.CreateInstance<BuffDef>();

            NoBossDropsBuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            NoBossDropsBuff.canStack = false;
            NoBossDropsBuff.isDebuff = false;
            NoBossDropsBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            NoBossDropsBuff.name = "TrophyHunterDebuff";
            NoBossDropsBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();
            NoBossDropsBuff.isHidden = true;

            R2API.ContentAddition.AddBuffDef(NoBossDropsBuff);
            YesBossDropsBuff = ScriptableObject.CreateInstance<BuffDef>();

            YesBossDropsBuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            YesBossDropsBuff.canStack = false;
            YesBossDropsBuff.isDebuff = false;
            YesBossDropsBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            YesBossDropsBuff.name = "TrophyHunterDebuff2";
            YesBossDropsBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();
            YesBossDropsBuff.isHidden = true;

            R2API.ContentAddition.AddBuffDef(YesBossDropsBuff);
        }
    }
}
