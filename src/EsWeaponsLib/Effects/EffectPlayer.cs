using System.Collections.Generic;
using EWL.Besiege;
using UnityEngine;

namespace EWL.Effects
{
	// ponytail: エフェクトは都度 Instantiate/Destroy(プールなし)。機関砲連射で GC・負荷が目立ったらプール化。
	internal static class EffectPlayer
	{
		/// <summary>一回再生して、パーティクルが消えたら自動で破棄する。parent が null ならシミュ置き場に置く。</summary>
		internal static GameObject PlayOnce(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale)
		{
			var go = Spawn(prefab, parent, pos, rot, scale);
			if (go == null) return null;
			foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Play();
			go.AddComponent<EffectAutoDestroy>();
			return go;
		}

		/// <summary>付け外し用(弾に追従するトレイル等)。終わらせるときは <see cref="Fade"/> か Destroy。</summary>
		internal static GameObject Attach(GameObject prefab, Transform parent)
		{
			return Spawn(prefab, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		/// <summary>親から切り離して放出を止め、残りのパーティクルが消えたら破棄する。</summary>
		internal static void Fade(GameObject go)
		{
			if (go == null) return;
			go.transform.SetParent(Simulation.Root, true);
			foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Stop();
			go.AddComponent<EffectAutoDestroy>();
		}

		internal static void PlaySound(List<AudioClip> clips, Vector3 pos)
		{
			if (clips == null || clips.Count == 0) return;
			var clip = clips[Random.Range(0, clips.Count)];
			if (clip != null) AudioSource.PlayClipAtPoint(clip, pos);
		}

		private static GameObject Spawn(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale)
		{
			if (prefab == null) return null;
			var go = (GameObject)Object.Instantiate(prefab);
			go.transform.SetParent(parent != null ? parent : Simulation.Root, false);
			go.transform.localPosition = pos;
			go.transform.localRotation = rot;
			go.transform.localScale = scale;
			go.SetActive(true);
			return go;
		}
	}

	internal sealed class EffectAutoDestroy : MonoBehaviour
	{
		private const float NoParticleLifetime = 3f;
		private ParticleSystem[] _systems;
		private float _deadline;

		private void Start()
		{
			_systems = GetComponentsInChildren<ParticleSystem>();
			_deadline = Time.time + NoParticleLifetime;
		}

		private void Update()
		{
			if (_systems.Length == 0) { if (Time.time >= _deadline) Destroy(gameObject); return; }
			foreach (var ps in _systems) if (ps != null && ps.IsAlive(true)) return;
			Destroy(gameObject);
		}
	}
}
