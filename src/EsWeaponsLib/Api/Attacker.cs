using EWL.Besiege;

namespace EWL.Api
{
    /// <summary>
    /// 攻撃者のタグ(誰の弾か・誰が起爆させたか)。発射・起爆の時点の値なので、撃った人がリスポーン・観戦・退出しても変わらない。
    /// 記録は実行権威側(SP・MP ホスト・ローカルシミュ)だけで行う。キル・デスに数えるかは受け取る側が決める。
    /// </summary>
    public sealed class Attacker
    {
        internal Attacker(ushort playerId, MPTeam team, Modding.Common.Player player)
        {
            PlayerId = playerId;
            Team = team;
            Player = player;
        }

        /// <summary>プレイヤーの NetworkId。SP は 0。退出した人の番号は後から参加した人に再利用されうる(同一人物かは <see cref="Player"/> で比べる)。</summary>
        public ushort PlayerId { get; private set; }

        /// <summary>チーム。撃ったブロックが分かればそのブロックのチーム(シミュ開始時の値)、分からなければその時点のプレイヤーのチーム。SP は <c>MPTeam.None</c>。</summary>
        public MPTeam Team { get; private set; }

        /// <summary>
        /// MP のプレイヤー。接続ごとに別の参照なので、番号が再利用されても <c>Equals</c> で別人と分かる。退出後も <c>Name</c> は読める。SP は null。
        /// </summary>
        public Modding.Common.Player Player { get; private set; }

        /// <summary>
        /// 爆発済みのバニラのボムを起爆させた人。分からない・まだ爆発していない・ボム以外・実行権威側でないときは null。
        /// 誘爆の連鎖は根本の原因までたどる。EWL の弾の直撃・爆風(その中で起きた誘爆を含む)はその撃ち手、
        /// バニラの矢・砲弾の直撃と発射済みロケットの衝突はその持ち主、落下・体当たり等のブロック衝突はボムの持ち主(自爆)。
        /// 位置では推定しないので、グレネード・ロケット・燃料樽の爆風、着火、他 Mod の弾で起爆したものは不明。
        /// </summary>
        public static Attacker GetDetonator(BlockBehaviour explosive)
        {
            return explosive != null ? ExplosiveTag.DetonatorOf(explosive) : null;
        }
    }
}
