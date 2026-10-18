using UnityEngine;

namespace EWL.Weapons
{
    // 誘導弾の操舵。弾の向き(機首)をトルクで回すだけで、速度は推進が機首の向きへ押して変える。
    // 狙う点: 目標が弾の到達までに動く先(迎撃点)。到達時間は「距離 ÷ その間の平均の速さ」の近似で、反復するほど正確になる(既定 3 回)。
    //   重力を受ける弾は、到達までに落ちる分だけ上を狙う。
    // 機首の向け方: 速度は機首より遅れて曲がるので、機首を「迎撃点への向き」より速度のずれの分だけ先へ向ける。
    //   機首を迎撃点へ向けるだけだと、速度が追いつく前に目標の横・上を通り過ぎる(山なりの軌道で前後にずれる原因)。
    // トルクの係数・揺らぎ・発射 0.2 秒の方向維持は ACM の Direct 誘導の値と同じ。
    // 目標は発射要求の時点で決まったものだけを追い、見失った・解除したら以後は追わない(別の目標を拾い直さない)。
    // 追っていない間は直進させる: 目標なしは発射方向、見失い・解除の後はその時点の機首の向きを保つ(発射方向へは戻さない)。
    // 弾1発に1つ。弾のオブジェクトと一緒に使い回す。
    internal sealed class MissileGuidance
    {
        private const float GuidanceStartTime = 0.2f;    // 発射からこの時間は発射方向を保つ
        private const float HeadingHoldTorque = 50f;     // 保つ向きへ戻すトルクの係数
        private const float SteeringTorque = 500f;       // × Gain
        private const float TorqueLimit = 150f;          // × Gain
        private const float SlipCompensation = 1f;       // 機首を速度のずれ × これだけ先へ向ける
        private const float MinimumPredictionSpeed = 1f; // 到達時間を求めるときの速さの下限 [m/s](発射直後の 0 割りを避ける)
        private const float JitterTorque = 100f;         // 揺らぎの振幅 × Gain
        private const float JitterChance = 0.1f;         // 揺らぎが始まる確率(物理フレームごと)
        private const int JitterFrames = 5;
        private const float JitterMinDistanceSqr = 5000f; // 目標まで約 70m 未満では揺らがない

        private Transform _target;
        private bool _tracking;
        private float _releaseAngle;
        private int _predictionIterations;
        private Vector3 _launchForward;
        private Vector3 _holdForward; // 追っていない間に保つ向き
        private Vector3 _previousTargetPosition;
        private bool _hasPreviousTargetPosition;
        private Vector3 _jitter;
        private int _jitterFramesLeft;

        internal void Begin(Transform target, Vector3 launchForward, float releaseAngle, int predictionIterations)
        {
            _target = target;
            _tracking = target != null;
            _releaseAngle = releaseAngle;
            _predictionIterations = predictionIterations;
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

        // thrustAcceleration: 今の推力による加速度 [m/s²](推力なし・燃料切れは 0)。maxSpeed: 推力で加速する上限 [m/s](0 は上限なし)。
        // どちらも到達時間の見積もりに使う。
        internal void Step(Rigidbody rb, Transform transform, float gain, float age, float thrustAcceleration, float maxSpeed)
        {
            var forward = transform.forward;
            // 見失い(シミュ停止・リスポーンで破棄、破壊で非アクティブ)と角度による解除は、発射直後を含めて毎フレーム見る
            if (_tracking && !CanKeepTracking(transform.position, forward))
            {
                End();
                _holdForward = forward;
            }
            // 目標の速度は毎フレーム測る(発射直後の方向維持の間も測っておき、操舵の開始から使えるようにする)
            var targetVelocity = Vector3.zero;
            if (_tracking)
            {
                var targetPosition = _target.position;
                if (_hasPreviousTargetPosition) targetVelocity = (targetPosition - _previousTargetPosition) / Time.fixedDeltaTime;
                _previousTargetPosition = targetPosition;
                _hasPreviousTargetPosition = true;
            }
            if (age < GuidanceStartTime || !_tracking)
            {
                rb.AddTorque(Vector3.Cross(age < GuidanceStartTime ? _launchForward : _holdForward, forward) * -HeadingHoldTorque);
                return;
            }
            var position = transform.position;
            var velocity = rb.velocity;
            var aimPoint = PredictInterceptPoint(position, velocity, rb.useGravity, targetVelocity, thrustAcceleration, maxSpeed);
            var offset = aimPoint - position;
            var torque = Vector3.ClampMagnitude(Vector3.Cross(SteeringDirection(offset.normalized, velocity, forward), forward) * (-SteeringTorque * gain), TorqueLimit * gain);
            UpdateJitter(gain);
            if (offset.sqrMagnitude >= JitterMinDistanceSqr) torque += _jitter;
            rb.AddTorque(torque);
        }

        // 迎撃点: 到達時間 t を「距離 ÷ (今の速さ + 推力で t/2 の間に増える速さ)」で見積もり、目標を t だけ先へ動かした点で距離を測り直す。
        // 反復回数 0 は目標の今の位置を狙う。
        private Vector3 PredictInterceptPoint(Vector3 position, Vector3 velocity, bool useGravity, Vector3 targetVelocity, float thrustAcceleration, float maxSpeed)
        {
            var targetPosition = _target.position;
            var aimPoint = targetPosition;
            float speed = velocity.magnitude;
            float timeToGo = 0f;
            for (int i = 0; i < _predictionIterations; i++)
            {
                float averageSpeed = speed + 0.5f * thrustAcceleration * timeToGo;
                if (maxSpeed > 0f) averageSpeed = Mathf.Min(averageSpeed, Mathf.Max(maxSpeed, speed));
                timeToGo = (aimPoint - position).magnitude / Mathf.Max(averageSpeed, MinimumPredictionSpeed);
                aimPoint = targetPosition + targetVelocity * timeToGo;
                if (useGravity) aimPoint -= Physics.gravity * (0.5f * timeToGo * timeToGo); // 落ちる分だけ上を狙う
            }
            return aimPoint;
        }

        // 機首を向ける向き: 迎撃点への向きに、速度の向きとのずれを足して先へ向ける。速度が小さい間は機首の向きを速度の代わりにする。
        private static Vector3 SteeringDirection(Vector3 aimDirection, Vector3 velocity, Vector3 forward)
        {
            float speed = velocity.magnitude;
            var velocityDirection = speed > MinimumPredictionSpeed ? velocity / speed : forward;
            var direction = aimDirection + (aimDirection - velocityDirection) * SlipCompensation;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : aimDirection;
        }

        private bool CanKeepTracking(Vector3 position, Vector3 forward)
        {
            if (_target == null || !_target.gameObject.activeInHierarchy) return false;
            // 解除角は迎撃点ではなく目標そのものへの向きで測る(予測の揺れで解除しないように)
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
