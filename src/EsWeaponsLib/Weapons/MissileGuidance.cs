using UnityEngine;

namespace EWL.Weapons
{
    // 誘導弾の操舵。ACM の Direct 誘導の式を簡略化したもの。係数は物理フレームあたりの値で ACM と同じ。
    // ACM の「15 フレーム分のトルクの和」は配列のずらし方の都合で実際には最新の値しか残らないので、比例項だけにしている。
    // 目標は発射要求の時点で決まったものだけを追い、見失った・解除したら以後は追わない(別の目標を拾い直さない)。
    // 追っていない間は直進させる: 目標なしは発射方向、見失い・解除の後はその時点の機首の向きを保つ(発射方向へは戻さない)。
    // 弾1発に1つ。弾のオブジェクトと一緒に使い回す。
    internal sealed class MissileGuidance
    {
        private const float GuidanceStartTime = 0.2f;    // 発射からこの時間は発射方向を保つ
        private const float HeadingHoldTorque = 50f;     // 保つ向きへ戻すトルクの係数
        private const float SteeringTorque = 500f;       // × Gain
        private const float TorqueLimit = 150f;          // × Gain
        private const float JitterTorque = 100f;         // 揺らぎの振幅 × Gain
        private const float JitterChance = 0.1f;         // 揺らぎが始まる確率(物理フレームごと)
        private const int JitterFrames = 5;
        private const float JitterMinDistanceSqr = 5000f; // 目標まで約 70m 未満では揺らがない
        private const float LeadFactor = 2f;             // 目標の1物理フレームの移動 × これだけ先を狙う

        private Transform _target;
        private bool _tracking;
        private float _releaseAngle;
        private Vector3 _launchForward;
        private Vector3 _holdForward; // 追っていない間に保つ向き
        private Vector3 _previousTargetPosition;
        private bool _hasPreviousTargetPosition;
        private Vector3 _jitter;
        private int _jitterFramesLeft;

        internal void Begin(Transform target, Vector3 launchForward, float releaseAngle)
        {
            _target = target;
            _tracking = target != null;
            _releaseAngle = releaseAngle;
            _launchForward = _holdForward = launchForward;
            _hasPreviousTargetPosition = false;
            _jitter = Vector3.zero;
            _jitterFramesLeft = 0;
        }

        internal void End()
        {
            _target = null;
            _tracking = false;
        }

        internal void Step(Rigidbody rb, Transform transform, float gain, float age)
        {
            var forward = transform.forward;
            // 見失い(シミュ停止・リスポーンで破棄、破壊で非アクティブ)と角度による解除は、発射直後を含めて毎フレーム見る
            if (_tracking && !CanKeepTracking(transform.position, forward))
            {
                End();
                _holdForward = forward;
            }
            if (age < GuidanceStartTime || !_tracking)
            {
                rb.AddTorque(Vector3.Cross(age < GuidanceStartTime ? _launchForward : _holdForward, forward) * -HeadingHoldTorque);
                return;
            }
            var targetPosition = _target.position;
            var lead = _hasPreviousTargetPosition ? (targetPosition - _previousTargetPosition) * LeadFactor : Vector3.zero;
            _previousTargetPosition = targetPosition;
            _hasPreviousTargetPosition = true;
            var offset = targetPosition + lead - transform.position;
            var torque = Vector3.ClampMagnitude(Vector3.Cross(offset.normalized, forward) * (-SteeringTorque * gain), TorqueLimit * gain);
            UpdateJitter(gain);
            if (offset.sqrMagnitude >= JitterMinDistanceSqr) torque += _jitter;
            rb.AddTorque(torque);
        }

        private bool CanKeepTracking(Vector3 position, Vector3 forward)
        {
            if (_target == null || !_target.gameObject.activeInHierarchy) return false;
            // 解除角は先読み点ではなく目標そのものへの向きで測る(先読みの揺れで解除しないように)
            return _releaseAngle <= 0f || Vector3.Angle(forward, _target.position - position) <= _releaseAngle;
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
