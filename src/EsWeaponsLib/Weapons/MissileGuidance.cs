using UnityEngine;

namespace EWL.Weapons
{
    // 誘導弾の操舵(仮実装)。ACM の Direct 誘導の式を簡略化したもの。係数は物理フレームあたりの値で ACM と同じ。
    // ACM の「15 フレーム分のトルクの和」は配列のずらし方の都合で実際には最新の値しか残らないので、比例項だけにしている。
    // 弾1発に1つ。弾のオブジェクトと一緒に使い回す。
    internal sealed class MissileGuidance
    {
        internal const float LockOnAngle = 30f;          // 目標を探す範囲: 発射方向から [度]
        private const float GuidanceStartTime = 0.2f;    // 発射からこの時間は発射方向を保つ
        private const float HeadingHoldTorque = 50f;     // 発射方向へ戻すトルクの係数
        private const float SteeringTorque = 500f;       // × Gain
        private const float TorqueLimit = 150f;          // × Gain
        private const float JitterTorque = 100f;         // 揺らぎの振幅 × Gain
        private const float JitterChance = 0.1f;         // 揺らぎが始まる確率(物理フレームごと)
        private const int JitterFrames = 5;
        private const float JitterMinDistanceSqr = 5000f; // 目標まで約 70m 未満では揺らがない
        private const float LeadFactor = 2f;             // 目標の1物理フレームの移動 × これだけ先を狙う
        private const float TurnBrake = 0.0001f;         // 減速: 速度 × これ × 向きとトルクの角度 [度]

        private Transform _target;
        private bool _hasTarget;
        private Vector3 _launchForward;
        private Vector3 _previousTargetPosition;
        private bool _hasPreviousTargetPosition;
        private Vector3 _jitter;
        private int _jitterFramesLeft;

        internal void Begin(Transform target, Vector3 launchForward)
        {
            _target = target;
            _hasTarget = target != null;
            _launchForward = launchForward;
            _hasPreviousTargetPosition = false;
            _jitter = Vector3.zero;
            _jitterFramesLeft = 0;
        }

        internal void End()
        {
            _target = null;
        }

        internal void Step(Rigidbody rb, Transform transform, float gain, float age)
        {
            var forward = transform.forward;
            if (!_hasTarget || age < GuidanceStartTime)
            {
                rb.AddTorque(Vector3.Cross(_launchForward, forward) * -HeadingHoldTorque);
                return;
            }
            // 目標を見失ったら(シミュ停止・リスポーンで破棄、破壊で非アクティブ)旋回をやめる。下の減速は続く(ACM と同じ)
            var torque = Vector3.zero;
            if (_target != null && _target.gameObject.activeInHierarchy)
            {
                var targetPosition = _target.position;
                var lead = _hasPreviousTargetPosition ? (targetPosition - _previousTargetPosition) * LeadFactor : Vector3.zero;
                _previousTargetPosition = targetPosition;
                _hasPreviousTargetPosition = true;
                var offset = targetPosition + lead - transform.position;
                torque = Vector3.ClampMagnitude(Vector3.Cross(offset.normalized, forward) * (-SteeringTorque * gain), TorqueLimit * gain);
                UpdateJitter(gain);
                if (offset.sqrMagnitude >= JitterMinDistanceSqr) torque += _jitter;
                rb.AddTorque(torque);
            }
            // 旋回トルクは向きと直交するので、ほぼ常に 90 度ぶん減速する(トルク 0 でも Unity 5.4 の Angle は 90 度を返す。ACM と同じ)
            rb.velocity -= rb.velocity * (TurnBrake * Vector3.Angle(forward, torque));
        }

        private void UpdateJitter(float gain)
        {
            if (_jitterFramesLeft <= 0 && Random.value > 1f - JitterChance)
            {
                float amplitude = JitterTorque * gain;
                _jitter = new Vector3(Random.Range(-amplitude, amplitude), Random.Range(-amplitude, amplitude), Random.Range(-amplitude, amplitude));
                _jitterFramesLeft = JitterFrames;
            }
            else if (_jitterFramesLeft > 0) _jitterFramesLeft--;
            else _jitter = Vector3.zero;
        }
    }
}
