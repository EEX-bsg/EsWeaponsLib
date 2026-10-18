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

        /// <summary>バニラの弾の命中を本体が処理する側か(SP・MP ホスト・ローカルシミュ中のクライアント)。本体の弾の判定と同じ条件。</summary>
        internal static bool SimulatesVanillaProjectiles
        {
            get { return StatMaster.levelSimulating && (!StatMaster.isClient || StatMaster.isLocalSim); }
        }

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

        /// <summary>このピアのプレイヤーのブロックか(SP は常に true)。</summary>
        internal static bool IsLocalPlayers(BlockBehaviour block)
        {
            return block != null && block.ParentMachine != null && block.ParentMachine.isLocalMachine;
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

        /// <summary>ゴッドツール「爆発する砲弾」。静的な GodTools はローカル UI の値なので、MP でも正しい発射者のマシンのフラグを読む。</summary>
        internal static bool IsExplodingCannonballs(BlockBehaviour block)
        {
            return block != null && block.ParentMachine != null && block.ParentMachine.ExplodingCannonballs;
        }

        /// <summary>ゴッドツールの効果が出たことを記録する(本体と同じく、レベルのクリア記録に反映される)。</summary>
        internal static void MarkGodToolUsed()
        {
            StatMaster.GodTools.HasBeenUsed = true;
        }
    }
}
