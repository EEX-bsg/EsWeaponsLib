using System.Collections.Generic;
using EWL.Api;
using UnityEngine;

namespace EWL.Besiege
{
    // 本体のダメージ・爆発経路への入口(落とし穴をここに集約)。呼ぶのはシミュ権威側だけ。
    internal static class Damage
    {
        // IExplosionEffect.OnExplode のマスク。0x20 = 爆発に反応させる(爆弾の誘爆等)。237 は本体爆弾・ACM と同じ。
        private const int ExplosionMask = 237;
        private const int HitMask = 0x20;
        private const int IgnoreLayerA = 20, IgnoreLayerB = 22;


        /// <summary>
        /// 命中1回分の処理。ダメージ・衝撃力・命中物の OnExplode を与え、刺さるなら刺す先の Transform を返す。
        /// 処理中は攻撃者のタグを掲げる(この中で起爆した爆発物は attacker のものになる)。
        /// </summary>
        internal static Transform ApplyHit(Collider hit, Vector3 projectileVelocity, float impactForce,
            float blockDamage, float entityDamage, bool attach, Attacker attacker)
        {
            var rb = hit.attachedRigidbody;
            if (rb == null) return null; // 地形等。刺さる弾はその場で止まるだけ

            // 当たった爆発物が自分の衝突処理で先に爆発していた場合に備え、直撃を記録する
            ExplosiveTag.NoteDirectHit(rb, attacker);
            var previous = AttackerTags.Begin(attacker);
            try { return ApplyHitToBody(rb, hit, projectileVelocity, impactForce, blockDamage, entityDamage, attach); }
            finally { AttackerTags.End(previous); }
        }

        private static Transform ApplyHitToBody(Rigidbody rb, Collider hit, Vector3 projectileVelocity, float impactForce,
            float blockDamage, float entityDamage, bool attach)
        {
            rb.AddForce(projectileVelocity * impactForce);
            Transform attachTo = null;
            var info = rb.GetComponent<BasicInfo>();

            var block = info as BlockBehaviour;
            if (block != null)
            {
                if (block.isSimulating && block.Prefab.hasHealthBar)
                {
                    if (block.gotChildBlocks)
                    {
                        var child = block.GetChildBlockFromCollider(hit);
                        if (child != null)
                        {
                            if (child.Prefab.hasHealthBar) child.BlockHealth.DamageBlock(blockDamage);
                            if (attach) attachTo = child.transform;
                        }
                    }
                    else block.BlockHealth.DamageBlock(blockDamage);
                }
            }
            else if (info is AIGenericEntity)
            {
                var killingHandler = ((AIGenericEntity)info).aiEntity.my.killingHandler;
                if (killingHandler != null && !killingHandler.my.AiCode.isDead)
                {
                    killingHandler.TakeDamage(entityDamage, InjuryType.Sharp);
                    if (attach) attachTo = killingHandler.my.AiCode.my.VisObject;
                }
            }
            else if (info is EnemyAISimple)
            {
                var ai = (EnemyAISimple)info;
                if (!ai.isDead)
                {
                    ai.TakeDamage(entityDamage, InjuryType.Sharp);
                    if (attach) attachTo = ai.visObject;
                }
            }
            else if (!StatMaster.isMP)
            {
                var killingHandler = rb.GetComponent<KillingHandler>();
                if (killingHandler != null && !killingHandler.my.AiCode.isDead)
                {
                    killingHandler.TakeDamage(entityDamage, InjuryType.Sharp);
                    if (attach) attachTo = killingHandler.my.AiCode.my.VisObject;
                }
            }

            if (attachTo == null)
            {
                // 爆弾などの「当たったら起爆」する物へ通知(エンティティに刺さった場合は除く)
                foreach (var effect in ReferenceMaster.GetInterfaces<IExplosionEffect>(rb.gameObject))
                    if (rb.gameObject.activeInHierarchy) effect.OnExplode(0f, 0f, 0f, Vector3.zero, 0f, HitMask, false);
                if (attach) attachTo = rb.transform;
            }
            return attachTo;
        }

        /// <summary>コライダーのブロック。子ブロックを持つブロックでは当たった子(ダメージを受けるのと同じブロック)。</summary>
        internal static BlockBehaviour BlockOf(Collider collider)
        {
            var rb = collider != null ? collider.attachedRigidbody : null;
            var block = rb != null ? rb.GetComponent<BlockBehaviour>() : null;
            if (block == null || !block.gotChildBlocks) return block;
            var child = block.GetChildBlockFromCollider(collider);
            return child != null ? child : block;
        }

        // 本体の爆弾(ExplodeOnCollide)と同じく、剛体ごとに1回だけ力と OnExplode を与える。処理中は攻撃者のタグを掲げる。
        // affected: null でなければ、力を与えた剛体を足す(命中イベント用)。
        internal static void Explode(Vector3 pos, float radius, float power, float upPower, List<Rigidbody> affected, Attacker attacker)
        {
            var previous = AttackerTags.Begin(attacker);
            try
            {
                var pushed = new HashSet<Rigidbody>(); // OnExplode から再入しうるのでローカル
                foreach (var collider in Physics.OverlapSphere(pos, radius))
                {
                    var rb = collider.attachedRigidbody;
                    if (rb == null || rb.gameObject.layer == IgnoreLayerA || rb.gameObject.layer == IgnoreLayerB
                        || rb.CompareTag("KeepConstraintsAlways") || !pushed.Add(rb)) continue;
                    if (affected != null) affected.Add(rb);
                    rb.WakeUp();
                    rb.constraints = RigidbodyConstraints.None;
                    rb.AddExplosionForce(power, pos, radius, upPower);
                    foreach (var effect in ReferenceMaster.GetInterfaces<IExplosionEffect>(rb.gameObject))
                        effect.OnExplode(power, upPower, 0f, pos, radius, ExplosionMask, false);
                }
            }
            finally { AttackerTags.End(previous); }
        }
    }
}
