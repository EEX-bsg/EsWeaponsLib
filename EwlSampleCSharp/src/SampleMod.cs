using System;
using System.Runtime.CompilerServices;
using Modding;
using Modding.Modules;
using UnityEngine;

namespace EwlSampleCSharp
{
    public class SampleMod : ModEntryPoint
    {
        // E'sWeaponsLib の Mod ID
        private static readonly Guid EwlModId = new Guid("0d219960-b202-4906-b29d-8d90a61a89b8");

        public override void OnLoad()
        {
            // EWL が無い環境では、EWL の型に触れる前にやめる(モジュールを登録しない)
            if (!Mods.IsModLoaded(EwlModId))
            {
                Debug.LogWarning("[EwlSampleCSharp] E'sWeaponsLib is not loaded. SampleCannon is disabled.");
                return;
            }
            RegisterModules();
        }

        // EWL の型を使うモジュールの登録は別メソッドに分け、EWL がある確認の後でだけ呼ぶ
        // (インライン化されると OnLoad の時点で EWL の型を探しに行くので禁止する)
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterModules()
        {
            CustomModules.AddBlockModule<SampleCannonModule, SampleCannonBehaviour>("SampleCannon", true);
        }
    }
}
