using UnityEngine;

namespace EWL.Besiege
{
	// シミュ状態・権威判定の入口。
	internal static class Simulation
	{
		/// <summary>シミュ中の物体の置き場。シミュ終了時に本体が丸ごと破棄する(弾・エフェクトの後始末を兼ねる)。</summary>
		internal static Transform Root { get { return ReferenceMaster.physicsGoalInstance; } }

		/// <summary>このブロックの武装・物理を実行してよいか(SP / MP Host / MP Client ローカルシミュ)。IsHost では判定しない。</summary>
		internal static bool HasAuthority(BlockBehaviour block)
		{
			return block != null && block.isSimulating && block.SimPhysics;
		}

		internal static bool IsInfiniteAmmo(BlockBehaviour block)
		{
			return block.ParentMachine != null && block.ParentMachine.InfiniteAmmoMode;
		}
	}
}
