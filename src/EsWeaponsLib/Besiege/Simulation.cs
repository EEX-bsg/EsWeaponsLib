using UnityEngine;

namespace EWL.Besiege
{
    // シミュ状態・権威判定の入口。
    internal static class Simulation
    {
        /// <summary>シミュ中だけ存在するオブジェクト。シミュ終了で本体が破棄するので、これが消えたら弾・エフェクトを回収する。</summary>
        internal static Object Scope { get { return ReferenceMaster.physicsGoalInstance; } }

        /// <summary>このブロックの武装・物理を実行してよいか(SP / MP Host / MP Client ローカルシミュ)。IsHost では判定しない。</summary>
        internal static bool HasAuthority(BlockBehaviour block)
        {
            return block != null && block.isSimulating && block.SimPhysics;
        }

        internal static bool IsLevelSimulating { get { return StatMaster.levelSimulating; } }

        /// <summary>MP ホストのグローバルシミュ。ここで出た弾だけをクライアントへ配信する(ローカルシミュの弾は共有しない)。
        /// 配信役の判定であって、武装の実行権威の判定ではない(権威は <see cref="HasAuthority"/>)。</summary>
        internal static bool PublishesProjectiles
        {
            get { return StatMaster.isMP && StatMaster.isHosting && !StatMaster.isLocalSim && StatMaster.levelSimulating; }
        }

        /// <summary>MP クライアントで、ローカルシミュ中でない(ホストのシミュを表示する)。</summary>
        internal static bool ShowsRemoteProjectiles
        {
            get { return StatMaster.isMP && !StatMaster.isHosting && !StatMaster.isLocalSim; }
        }

        /// <summary>ブロックを持つプレイヤーの NetworkId(Modding.Common.Player)。SP は 0。</summary>
        internal static ushort PlayerIdOf(BlockBehaviour block)
        {
            return block != null && block.ParentMachine != null ? block.ParentMachine.PlayerID : (ushort)0;
        }

        internal static bool IsInfiniteAmmo(BlockBehaviour block)
        {
            return block.ParentMachine != null && block.ParentMachine.InfiniteAmmoMode;
        }
    }
}
