using System.ComponentModel;
using System.Xml.Serialization;
using Modding.Modules;
using Modding.Serialization;

namespace EwlSampleCSharp
{
    // このサンプル独自の XML カスタムモジュール。値の出どころ(XML・スライダー・計算)は利用側が自由に決める。
    // FireKey 以外は省略可(DefaultValue と初期化子を同じ値にする)。
    [XmlRoot("SampleCannon")]
    public class SampleCannonModule : BlockModule
    {
        [XmlElement, RequireToValidate]
        public MKeyReference FireKey;

        /// <summary>発射の強さ。初速 [m/s] にそのまま使う。</summary>
        [XmlElement, DefaultValue(null)]
        public MSliderReference PowerSlider;

        [XmlElement, DefaultValue(0.5f)] public float Interval = 0.5f;
        [XmlElement, DefaultValue(4f)] public float ExplosionRadius = 4f;
    }
}
