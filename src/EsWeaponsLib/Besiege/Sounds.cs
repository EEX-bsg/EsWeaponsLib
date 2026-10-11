using System;
using UnityEngine;
using UnityEngine.Audio;

namespace EWL.Besiege
{
    // 本体の音の作法への入口。本体は AudioSource の 3D 設定とミキサーグループをプレハブ側で持ち、コードに共通値が無いので、
    // バニラの大砲・爆弾の AudioSource を見本として借りる。AudioSource.PlayClipAtPoint はミキサーを通らず、
    // 音量スライダー・水中処理・スロー時のピッチが効かないので使わない。
    internal static class Sounds
    {
        private static AudioSource _fireTemplate;
        private static AudioSource _explosionTemplate;
        private static AudioSource _fallbackTemplate;

        /// <summary>発射音の見本(バニラ大砲)。取れなければ Physics ミキサーの 3D 音。</summary>
        internal static AudioSource FireTemplate
        {
            get
            {
                if (_fireTemplate == null) _fireTemplate = FindTemplate(BlockType.Cannon, block => ((CanonBlock)block).audioSource) ?? Fallback(); // 失敗も覚えて再探索しない
                return _fireTemplate;
            }
        }

        /// <summary>爆発音の見本(バニラ爆弾の爆発エフェクト)。取れなければ Physics ミキサーの 3D 音。</summary>
        internal static AudioSource ExplosionTemplate
        {
            get
            {
                if (_explosionTemplate == null)
                    _explosionTemplate = FindTemplate(BlockType.Bomb,
                        block => ((ExplodeOnCollideBlock)block).explosionEffect.GetComponentInChildren<AudioSource>(true)) ?? Fallback();
                return _explosionTemplate;
            }
        }

        /// <summary>本体と同じく、音源位置が水中なら水中用ミキサーへ差し替える(CanonBlock の発射と同じ判定)。</summary>
        internal static AudioMixerGroup MixerAt(AudioMixerGroup mixer, Vector3 pos)
        {
            return mixer != null && WaterController.IsUnderwater(pos) ? ReferenceMaster.GetWaterMixerFrom(mixer) : mixer;
        }

        /// <summary>見本の 3D 設定を写す(音量は写さない。クリップ側で決める)。</summary>
        internal static void CopySpatialSettings(AudioSource template, AudioSource target)
        {
            target.spatialBlend = template.spatialBlend;
            target.rolloffMode = template.rolloffMode;
            target.minDistance = template.minDistance;
            target.maxDistance = template.maxDistance;
            target.spread = template.spread;
            target.dopplerLevel = template.dopplerLevel;
            target.priority = template.priority;
            target.reverbZoneMix = template.reverbZoneMix;
            if (template.rolloffMode == AudioRolloffMode.Custom)
                target.SetCustomCurve(AudioSourceCurveType.CustomRolloff, template.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        }

        private static AudioSource FindTemplate(BlockType type, Func<BlockBehaviour, AudioSource> select)
        {
            try
            {
                BlockBehaviour block;
                if (PrefabMaster.GetBlock(type, out block) && block != null)
                {
                    var template = select(block);
                    if (template != null)
                        Debug.Log("[EWL] Sound template " + type + ": mixer=" + (template.outputAudioMixerGroup != null ? template.outputAudioMixerGroup.name : "none")
                            + " spatialBlend=" + template.spatialBlend + " rolloff=" + template.rolloffMode
                            + " distance=" + template.minDistance + "-" + template.maxDistance + " volume=" + template.volume);
                    return template;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EWL] Vanilla " + type + " sound template not found, using fallback: " + e.Message);
            }
            return null;
        }

        private static AudioSource Fallback()
        {
            if (_fallbackTemplate != null) return _fallbackTemplate;
            var holder = new GameObject("[EWL] SoundTemplate");
            UnityEngine.Object.DontDestroyOnLoad(holder);
            holder.SetActive(false);
            _fallbackTemplate = holder.AddComponent<AudioSource>();
            _fallbackTemplate.spatialBlend = 1f;
            _fallbackTemplate.outputAudioMixerGroup = ReferenceMaster.GetMixer("Physics"); // RandomSoundController の既定と同じ
            return _fallbackTemplate;
        }
    }
}
