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

		internal static bool IsInfiniteAmmo(BlockBehaviour block)
		{
			return block.ParentMachine != null && block.ParentMachine.InfiniteAmmoMode;
		}
	}
}
