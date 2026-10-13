using EWL.Api;
using EWL.Weapons;
using UnityEngine;

namespace EWL.Besiege
{
    // 攻撃者タグの作り方と「処理中のタグ」。呼ぶのはシミュ権威側だけ。
    internal static class AttackerTags
    {
        /// <summary>
        /// EWL が直撃・爆風を与えている最中の攻撃者(それ以外は null)。本体の誘爆は元の爆発処理の中で同期的に起きるので、
        /// その間に起爆した爆発物はこの攻撃者のものと確定できる(位置で推定しない)。
        /// </summary>
        internal static Attacker Current { get; private set; }

        /// <summary>処理中のタグを掲げる。戻り値を <see cref="End"/> に渡して元に戻す(入れ子可)。</summary>
        internal static Attacker Begin(Attacker attacker)
        {
            var previous = Current;
            Current = attacker;
            return previous;
        }

        internal static void End(Attacker previous)
        {
            Current = previous;
        }

        /// <summary>ブロックの持ち主。チームはブロックの値(シミュ開始時のスナップショット)。</summary>
        internal static Attacker OfBlock(BlockBehaviour block)
        {
            ushort playerId = Simulation.PlayerIdOf(block);
            return new Attacker(playerId, block.Team, PlayerOf(playerId));
        }

        /// <summary>番号だけ分かるプレイヤー(バニラの弾)。チームはその時点のプレイヤーの値。</summary>
        internal static Attacker OfPlayerId(ushort playerId)
        {
            var player = PlayerOf(playerId);
            return new Attacker(playerId, player != null ? player.Team : MPTeam.None, player);
        }

        /// <summary>弾(EWL・バニラ)の撃ち手。弾でなければ null。</summary>
        internal static Attacker OfProjectile(Collider collider)
        {
            var rb = collider.attachedRigidbody;
            var body = rb != null ? rb.gameObject : collider.gameObject;
            var ewlProjectile = body.GetComponent<Projectile>();
            if (ewlProjectile != null) return ewlProjectile.Shooter;
            var arrow = body.GetComponent<ProjectileScript>();
            var pooled = body.GetComponent<NetworkProjectile>();
            if (arrow == null && pooled == null && body.GetComponent<CannonBallDamage>() == null) return null;
            // SP・ローカルシミュで物理を持つのは自分の機体だけ。MP のグローバルシミュでは弾が持ち主を記録している
            if (!StatMaster.isMP || StatMaster.isLocalSim) return LocalPlayer();
            if (arrow != null) return OfPlayerId((ushort)arrow.ownerID);
            return pooled != null ? OfPlayerId(pooled.playerId) : null; // プール外の弾(散弾)は持ち主の記録がない
        }

        private static Attacker LocalPlayer()
        {
            var player = StatMaster.isMP ? Modding.Common.Player.GetLocalPlayer() : null;
            return player != null ? new Attacker(player.NetworkId, player.Team, player) : new Attacker(0, MPTeam.None, null);
        }

        private static Modding.Common.Player PlayerOf(ushort playerId)
        {
            // SP には Player が無い。退出済みの番号は null
            return StatMaster.isMP ? Modding.Common.Player.From(playerId) : null;
        }
    }
}
