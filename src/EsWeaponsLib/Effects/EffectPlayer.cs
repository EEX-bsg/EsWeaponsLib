using System.Collections.Generic;
using EWL.Besiege;
using UnityEngine;

namespace EWL.Effects
{
    // エフェクトの再生とプール(ACMu と同じ設計)。プレハブごとに待機キューを持ち、生成数は上限まで。
    // 上限に達したらそのエフェクトは出さない(負荷を増やさないことを優先)。
    // 弾などに付いていない間(待機中・ワールドで再生中・切り離して消えかけ)はプレハブごとの置き場の子。
    // 置き場は SceneRoot の下。シミュをまたいで再利用し、シーンが変わったら全部捨てる。
    // 使用中のエフェクトは持ち主(シミュの目印)が消えたら即座に戻る。親ごと破棄された場合は数だけ減らす。
    internal static class EffectPlayer
    {
        internal sealed class Pool
        {
            internal readonly Queue<PooledEffect> Idle = new Queue<PooledEffect>();
            internal int Total;
            internal int MaxCount;
            internal Transform Root;
        }

        private static readonly Dictionary<GameObject, Pool> Pools = new Dictionary<GameObject, Pool>();
        private static Transform _poolRoot;

        /// <summary>一回再生し、パーティクルが消えたらプールへ戻す。parent が null ならワールド座標で置き場に置く。</summary>
        internal static void PlayOnce(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale,
            Object owner, int maxCount)
        {
            var effect = Rent(prefab, maxCount);
            if (effect == null) return;
            effect.Begin(parent, pos, rot, scale, owner);
        }

        /// <summary>弾に追従させる(トレイル・弾頭)。終わらせるときは <see cref="Fade"/> か <see cref="Return"/>。</summary>
        internal static PooledEffect Attach(GameObject prefab, Transform parent, Object owner, int maxCount)
        {
            var effect = Rent(prefab, maxCount);
            if (effect == null) return null;
            effect.Begin(parent, Vector3.zero, Quaternion.identity, Vector3.one, owner);
            effect.KeepAlive = true;
            return effect;
        }

        /// <summary>親から切り離して放出を止め、残りのパーティクルが消えたらプールへ戻す。</summary>
        internal static void Fade(PooledEffect effect)
        {
            if (effect != null) effect.BeginFade();
        }

        /// <summary>ランダムに1つ鳴らす。3D 設定とミキサーは見本(Besiege.Sounds)に揃え、水中なら水中用ミキサーへ。</summary>
        internal static void PlaySound(List<AudioClip> clips, Vector3 pos, AudioSource template)
        {
            if (clips == null || clips.Count == 0) return;
            var clip = clips[Random.Range(0, clips.Count)];
            if (clip == null) return;
            var source = IdleSoundSource(template);
            if (source == null) return;
            source.transform.position = pos;
            source.outputAudioMixerGroup = Sounds.MixerAt(template.outputAudioMixerGroup, pos);
            source.pitch = 1f + Random.Range(-0.05f, 0.05f); // 本体の大砲と同じ揺らぎ。スロー時のピッチはミキサー側が掛ける
            source.clip = clip;
            source.Play();
        }

        // ponytail: 見本ごとに線形探索、上限超過は鳴らさない。足りなければ上限を上げる
        private const int MaxSoundsPerTemplate = 32;
        private static readonly Dictionary<AudioSource, List<AudioSource>> SoundPools = new Dictionary<AudioSource, List<AudioSource>>();

        private static AudioSource IdleSoundSource(AudioSource template)
        {
            var poolRoot = PoolRoot();
            List<AudioSource> sources;
            if (!SoundPools.TryGetValue(template, out sources)) SoundPools[template] = sources = new List<AudioSource>();
            sources.RemoveAll(source => source == null);
            foreach (var source in sources) if (!source.isPlaying) return source;
            if (sources.Count >= MaxSoundsPerTemplate) return null;

            var created = new GameObject("Sound").AddComponent<AudioSource>();
            created.transform.SetParent(poolRoot, false);
            created.playOnAwake = false;
            Sounds.CopySpatialSettings(template, created);
            sources.Add(created);
            return created;
        }

        private static PooledEffect Rent(GameObject prefab, int maxCount)
        {
            if (prefab == null) return null;
            var poolRoot = PoolRoot();
            Pool pool;
            if (!Pools.TryGetValue(prefab, out pool))
            {
                pool = new Pool();
                pool.Root = new GameObject(prefab.name).transform;
                pool.Root.SetParent(poolRoot, false);
                Pools[prefab] = pool;
            }
            if (maxCount > pool.MaxCount) pool.MaxCount = maxCount;

            while (pool.Idle.Count > 0)
            {
                var idle = pool.Idle.Dequeue();
                if (idle != null) return idle;
                pool.Total--; // 外部から破棄されていた
            }
            if (pool.Total >= pool.MaxCount) return null;

            var instance = (GameObject)Object.Instantiate(prefab);
            instance.SetActive(false);
            instance.transform.SetParent(pool.Root, false);
            var effect = instance.AddComponent<PooledEffect>();
            effect.Home = pool;
            pool.Total++;
            return effect;
        }

        internal static void Return(PooledEffect effect)
        {
            if (effect == null || effect.IsIdle) return;
            var pool = effect.Home;
            effect.IsIdle = true;
            effect.gameObject.SetActive(false);
            effect.transform.SetParent(pool.Root, false);
            pool.Idle.Enqueue(effect);
        }

        /// <summary>使用中に親ごと破棄されたとき。</summary>
        internal static void Forget(PooledEffect effect)
        {
            if (!effect.IsIdle) effect.Home.Total--;
        }

        // 置き場が消えていたらシーンが変わった: 中のエフェクトも音も全部消えているので、プールを一から作り直す。
        private static Transform PoolRoot()
        {
            if (_poolRoot != null) return _poolRoot;
            Pools.Clear();
            SoundPools.Clear();
            _poolRoot = SceneRoot.CreateChild("EffectPool");
            return _poolRoot;
        }
    }

    internal sealed class PooledEffect : MonoBehaviour
    {
        private const float NoParticleLifetime = 3f;

        internal EffectPlayer.Pool Home;
        internal bool IsIdle = true;
        /// <summary>true の間はパーティクルが消えても戻さない(弾に付いている間)。</summary>
        internal bool KeepAlive;

        private ParticleSystem[] _systems;
        private TrailRenderer[] _trails;
        private float _trailTime; // 軌跡が消えきるまで [s](TrailRenderer の Time の最大)
        private Object _owner;
        private bool _hasOwner;
        private float _deadline, _trailDeadline;

        internal void Begin(Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Object owner)
        {
            if (_systems == null) FindRenderers();
            IsIdle = false;
            KeepAlive = false;
            _owner = owner;
            _hasOwner = owner != null;
            _deadline = Time.time + NoParticleLifetime;
            _trailDeadline = Time.time + _trailTime;

            transform.SetParent(parent != null ? parent : Home.Root, false); // 置き場は原点・無回転
            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = scale;
            gameObject.SetActive(true);
            foreach (var particles in _systems)
            {
                particles.Clear();
                particles.Play();
            }
            foreach (var trail in _trails) trail.Clear(); // 前回の軌跡と、置き場から移った分の線を消す
        }

        // TrailRenderer の Autodestruct は止まった軌跡をオブジェクトごと破棄する。命中で切り離した軌跡や、
        // 一時停止中の弾の軌跡が持ち主の知らないうちに消えるので、無効にして消える時間はここで管理する。
        private void FindRenderers()
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _trails = GetComponentsInChildren<TrailRenderer>(true);
            _trailTime = 0f;
            foreach (var trail in _trails)
            {
                trail.autodestruct = false;
                _trailTime = Mathf.Max(_trailTime, trail.time);
            }
        }

        internal void BeginFade()
        {
            if (IsIdle) return;
            KeepAlive = false;
            transform.SetParent(Home.Root, true);
            foreach (var particles in _systems) particles.Stop();
            _deadline = Time.time + NoParticleLifetime;
            _trailDeadline = Time.time + _trailTime;
        }

        private void Update()
        {
            if (_hasOwner && _owner == null) { EffectPlayer.Return(this); return; } // シミュ終了
            if (KeepAlive) return;
            if (Time.time < _trailDeadline) return; // 切り離した軌跡が消えきるまで待つ
            if (_systems.Length == 0)
            {
                // 軌跡だけのエフェクトは消えきったら戻す。どちらも無ければ一定時間で戻す
                if (_trails.Length > 0 || Time.time >= _deadline) EffectPlayer.Return(this);
                return;
            }
            foreach (var particles in _systems) if (particles.IsAlive(true)) return;
            EffectPlayer.Return(this);
        }

        private void OnDestroy()
        {
            EffectPlayer.Forget(this);
        }
    }
}
