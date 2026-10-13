using System;
using System.Collections.Generic;
using System.IO;
using EWL.Besiege;
using EWL.Weapons;
using Modding;
using Modding.Blocks;
using UnityEngine;

namespace EWL.Net
{
    [Flags]
    internal enum WeaponStateFlags : byte
    {
        None = 0,
        Reloading = 1,
        Fired = 2,
        ReloadStarted = 4,
    }

    // 弾の MP 同期。MP ホストのグローバルシミュで出た弾だけを配信し、クライアントは同じ武装の定義から見た目だけのプロキシを出す。
    // 武装は (ブロック, ブロック上の並び順) で対応付ける(Weapon は全ピアで同じ順に生成される契約)。
    // ModNetworking は Reliable 固定なので、位置は 10Hz に抑え、スナップショットを生存一覧として兼用する。
    // 設計: .internal-docs/project/notes/mp-sync.md
    internal sealed class ProjectileSync : MonoBehaviour
    {
        private const float SnapshotInterval = 0.1f; // 本体の sendRate 既定と同じ 10Hz

        private static MessageType _spawnType, _despawnType, _snapshotType, _weaponStateType;

        private static readonly Dictionary<int, Projectile> Published = new Dictionary<int, Projectile>(); // ホスト
        private static readonly Dictionary<int, Projectile> Proxies = new Dictionary<int, Projectile>();   // クライアント
        private static readonly List<int> Removing = new List<int>();
        private static readonly HashSet<int> Listed = new HashSet<int>();
        private static int _nextId = 1;
        private static int _frame;
        private static int _lastReceivedFrame = -1;
        private static bool _snapshotPending;
        private static float _nextSnapshotTime;

        private static readonly MemoryStream WriteBuffer = new MemoryStream();
        private static readonly BinaryWriter Writer = new BinaryWriter(WriteBuffer);

        /// <summary>Mod のロード時に1回。メッセージ種別 ID は作成順で決まるので、全ピアで同じ順に作る。
        /// 購読はゲーム終了まで続く(Mod はアンロードされない)。</summary>
        internal static void Initialize()
        {
            if (_spawnType != null) return;
            _spawnType = ModNetworking.CreateMessageType(DataType.Block, DataType.ByteArray);
            _despawnType = ModNetworking.CreateMessageType(DataType.ByteArray);
            _snapshotType = ModNetworking.CreateMessageType(DataType.ByteArray);
            _weaponStateType = ModNetworking.CreateMessageType(DataType.Block, DataType.ByteArray);
            ModNetworking.Callbacks[_spawnType] += Receive(OnSpawn);
            ModNetworking.Callbacks[_despawnType] += Receive(OnDespawn);
            ModNetworking.Callbacks[_snapshotType] += Receive(OnSnapshot);
            ModNetworking.Callbacks[_weaponStateType] += Receive(OnWeaponState);
            ModRoot.CreateChild("ProjectileSync").gameObject.AddComponent<ProjectileSync>();
        }

        // ---- ホスト ----

        internal static void Spawned(WeaponDriver weapon, Projectile projectile, Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            if (!Simulation.PublishesProjectiles) return;
            int id = _nextId++;
            projectile.NetworkId = id;
            Published[id] = projectile;
            _snapshotPending = true;
            BeginWrite();
            Writer.Write((byte)weapon.NetworkIndex);
            Writer.Write(id);
            Writer.Write(_frame);
            WriteVector(position);
            WriteRotation(rotation);
            WriteVector(velocity);
            Send(_spawnType.CreateMessage(weapon.Block, EndWrite()));
        }

        /// <summary>配信中の弾が消えた。exploded なら位置と向きで爆発エフェクトを出させる。</summary>
        internal static void Despawned(Projectile projectile, bool exploded, Vector3 position, Quaternion effectRotation)
        {
            int id = projectile.NetworkId;
            projectile.NetworkId = 0;
            if (!Published.Remove(id) || !Simulation.PublishesProjectiles) return;
            BeginWrite();
            Writer.Write(id);
            Writer.Write(exploded);
            WriteVector(position);
            WriteRotation(effectRotation);
            Send(_despawnType.CreateMessage(EndWrite()));
        }

        internal static void WeaponStateChanged(WeaponDriver weapon, WeaponStateFlags events)
        {
            if (!Simulation.PublishesProjectiles) return;
            var flags = events;
            if (weapon.IsReloading) flags |= WeaponStateFlags.Reloading;
            BeginWrite();
            Writer.Write((byte)weapon.NetworkIndex);
            Writer.Write(weapon.Ammo);
            Writer.Write(weapon.ReserveAmmo);
            Writer.Write((byte)flags);
            Send(_weaponStateType.CreateMessage(weapon.Block, EndWrite()));
        }

        private void Update()
        {
            if (!Simulation.IsLevelSimulating) _lastReceivedFrame = -1; // 次のシミュ(別のホストのこともある)は番号が戻る
            if (!_snapshotPending || Time.unscaledTime < _nextSnapshotTime) return;
            _nextSnapshotTime = Time.unscaledTime + SnapshotInterval;
            if (!Simulation.PublishesProjectiles)
            {
                Published.Clear();
                _snapshotPending = false;
                return;
            }
            // 空になった直後の1回はクライアントの掃除用に空で送る
            _snapshotPending = Published.Count > 0;
            SendSnapshot();
        }

        private static void SendSnapshot()
        {
            Removing.Clear();
            foreach (var pair in Published)
                if (pair.Value == null) Removing.Add(pair.Key); // 親ごと破棄された
            foreach (int id in Removing) Published.Remove(id);

            _frame++;
            BeginWrite();
            Writer.Write(_frame);
            Writer.Write(Published.Count);
            foreach (var pair in Published)
            {
                Writer.Write(pair.Key);
                WriteVector(pair.Value.transform.position);
                WriteVector(pair.Value.Velocity);
            }
            Send(_snapshotType.CreateMessage(EndWrite()));
        }

        // ---- クライアント ----

        private static void OnSpawn(Message message, BinaryReader reader)
        {
            var block = message.GetData(0) as Block;
            int index = reader.ReadByte();
            int id = reader.ReadInt32();
            int frame = reader.ReadInt32();
            var position = ReadVector(reader);
            var rotation = ReadRotation(reader);
            var velocity = ReadVector(reader);
            var weapon = block != null ? WeaponDriver.Find(block.InternalObject, index) : null;
            if (weapon == null) return; // シミュ前後・ブロック破壊済み
            var proxy = weapon.SpawnProxy(position, rotation, velocity);
            if (proxy == null) return;
            proxy.NetworkId = id;
            proxy.SpawnFrame = frame;
            Proxies[id] = proxy;
        }

        private static void OnDespawn(Message message, BinaryReader reader)
        {
            int id = reader.ReadInt32();
            bool exploded = reader.ReadBoolean();
            var position = ReadVector(reader);
            var effectRotation = ReadRotation(reader);
            Projectile proxy;
            if (Proxies.TryGetValue(id, out proxy)) proxy.EndProxy(exploded, position, effectRotation);
        }

        private static void OnSnapshot(Message message, BinaryReader reader)
        {
            int frame = reader.ReadInt32();
            // 古い(順序が入れ替わった)ものは捨てる。大きく戻ったらホストの再起動とみなして受け入れる
            if (frame <= _lastReceivedFrame && frame > _lastReceivedFrame - 1000) return;
            _lastReceivedFrame = frame;
            int count = reader.ReadInt32();
            Listed.Clear();
            for (int i = 0; i < count; i++)
            {
                int id = reader.ReadInt32();
                var position = ReadVector(reader);
                var velocity = ReadVector(reader);
                Listed.Add(id);
                Projectile proxy;
                if (Proxies.TryGetValue(id, out proxy)) proxy.ReceiveSnapshot(position, velocity);
            }
            // 一覧に無く、この一覧より前に生成されたプロキシは、Despawn を取りこぼしたもの
            Removing.Clear();
            foreach (var pair in Proxies)
                if (!Listed.Contains(pair.Key) && pair.Value.SpawnFrame < frame) Removing.Add(pair.Key);
            foreach (int id in Removing)
            {
                Projectile proxy;
                if (Proxies.TryGetValue(id, out proxy)) proxy.Despawn();
            }
        }

        private static void OnWeaponState(Message message, BinaryReader reader)
        {
            var block = message.GetData(0) as Block;
            int index = reader.ReadByte();
            int ammo = reader.ReadInt32();
            int reserveAmmo = reader.ReadInt32();
            var flags = (WeaponStateFlags)reader.ReadByte();
            var weapon = block != null ? WeaponDriver.Find(block.InternalObject, index) : null;
            if (weapon != null) weapon.ReceiveState(ammo, reserveAmmo, flags);
        }

        /// <summary>プロキシが消えた(Despawn・破棄)。</summary>
        internal static void ProxyRemoved(Projectile proxy)
        {
            int id = proxy.NetworkId;
            proxy.NetworkId = 0;
            Projectile registered;
            if (Proxies.TryGetValue(id, out registered) && registered == proxy) Proxies.Remove(id);
        }

        // ---- 共通 ----

        private static Action<Message> Receive(Action<Message, BinaryReader> handler)
        {
            return message =>
            {
                // ホストは自分の弾しか持たない。ローカルシミュ中のクライアントはホストのシミュを表示しない
                if (!Simulation.ShowsRemoteProjectiles) return;
                try
                {
                    var bytes = (byte[])message.GetData(message.Type == _spawnType || message.Type == _weaponStateType ? 1 : 0);
                    using (var reader = new BinaryReader(new MemoryStream(bytes))) handler(message, reader);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[EWL] ProjectileSync receive failed: " + ex);
                }
            };
        }

        private static void Send(Message message)
        {
            try { ModNetworking.SendToAll(message); }
            catch (Exception ex) { Debug.LogError("[EWL] ProjectileSync send failed: " + ex); }
        }

        private static void BeginWrite()
        {
            WriteBuffer.SetLength(0);
        }

        private static byte[] EndWrite()
        {
            Writer.Flush();
            return WriteBuffer.ToArray();
        }

        private static void WriteVector(Vector3 value)
        {
            Writer.Write(value.x);
            Writer.Write(value.y);
            Writer.Write(value.z);
        }

        private static void WriteRotation(Quaternion value)
        {
            Writer.Write(value.x);
            Writer.Write(value.y);
            Writer.Write(value.z);
            Writer.Write(value.w);
        }

        private static Vector3 ReadVector(BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        private static Quaternion ReadRotation(BinaryReader reader)
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
