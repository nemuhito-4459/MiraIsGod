// ================================================================
//  ミラHQの地図データ (ゲームの中から書き出した本物の座標)
//  座標はゲームの中の単位。x は右が大きい、y は上が大きい
//  作り方: プラグインに一時的な書き出し処理を入れて、ミラHQの試合で MiraMapData.txt を作った
// ================================================================
using System.Collections.Generic;
using System.Drawing;

namespace MiraIsGodInstaller
{
    static class MapData
    {
        // ベントの位置 (番号 → 座標)
        public static readonly Dictionary<int, PointF> Vents = new Dictionary<int, PointF>
        {
            { 11, new PointF(-6.18f, 3.56f) },
            { 1, new PointF(23.77f, -1.94f) },
            { 2, new PointF(23.9f, 7.18f) },
            { 3, new PointF(0.48f, 10.697f) },
            { 4, new PointF(11.606f, 13.816f) },
            { 10, new PointF(4.29f, 0.53f) },
            { 6, new PointF(22.39f, 17.23f) },
            { 5, new PointF(13.28f, 20.13f) },
            { 7, new PointF(17.85f, 25.23f) },
            { 8, new PointF(15.41f, -1.82f) },
            { 9, new PointF(6.83f, 3.145f) },
        };

        // 部屋の形 (日本語の名前, 角の座標の並び)。同じ名前が何個かある部屋もある (廊下など)
        public static readonly (string Name, PointF[] Shape)[] Rooms =
        {
            ("発射台", new[] { new PointF(-11.627f, -0.084f), new PointF(0.176f, -0.084f), new PointF(0.176f, 8.331f), new PointF(-11.627f, 8.331f) }),
            ("カフェテリア", new[] { new PointF(29.316f, 5.593f), new PointF(21.202f, 5.631f), new PointF(21.2f, 0.799f), new PointF(17.9f, 0.791f), new PointF(17.907f, -0.656f), new PointF(29.388f, -0.619f) }),
            ("倉庫", new[] { new PointF(17.821f, 1.138f), new PointF(21.118f, 1.138f), new PointF(21.118f, 5.801f), new PointF(17.821f, 5.801f) }),
            ("廊下", new[] { new PointF(23.2f, 8.003f), new PointF(19.033f, 15.821f), new PointF(19.027f, 21.117f), new PointF(16.684f, 21.111f), new PointF(16.696f, 15.776f), new PointF(12.569f, 7.865f), new PointF(14.248f, 6.275f), new PointF(21.313f, 6.265f) }),
            ("原子炉", new[] { new PointF(0.01f, 9.611f), new PointF(4.897f, 9.611f), new PointF(4.897f, 16.814f), new PointF(0.01f, 16.814f) }),
            ("研究室", new[] { new PointF(7.296f, 9.616f), new PointF(12.265f, 9.616f), new PointF(12.265f, 15.369f), new PointF(7.296f, 15.369f) }),
            ("ロッカールーム", new[] { new PointF(11.186f, 5.664f), new PointF(8.31f, 5.652f), new PointF(8.35f, 2.164f), new PointF(3.756f, 2.157f), new PointF(3.74f, 0.07f), new PointF(11.164f, 0.097f) }),
            ("管理室", new[] { new PointF(19.134f, 15.962f), new PointF(22.851f, 15.962f), new PointF(22.851f, 21.093f), new PointF(19.134f, 21.093f) }),
            ("オフィス", new[] { new PointF(12.72f, 16.511f), new PointF(16.38f, 16.511f), new PointF(16.38f, 21.041f), new PointF(12.72f, 21.041f) }),
            ("温室", new[] { new PointF(12.492f, 21.075f), new PointF(23.106f, 21.075f), new PointF(23.106f, 25.732f), new PointF(12.492f, 25.732f) }),
            ("通信室", new[] { new PointF(13.666f, 2.42f), new PointF(17.163f, 2.42f), new PointF(17.163f, 5.539f), new PointF(13.666f, 5.539f) }),
            ("バルコニー", new[] { new PointF(17.845f, -3.79f), new PointF(29.375f, -3.79f), new PointF(29.375f, -1.327f), new PointF(17.845f, -1.327f) }),
            ("医務室", new[] { new PointF(13.647f, -2.263f), new PointF(17.169f, -2.263f), new PointF(17.169f, 1.703f), new PointF(13.647f, 1.703f) }),
            ("除染室", new[] { new PointF(4.89f, 2.261f), new PointF(7.292f, 2.261f), new PointF(7.292f, 9.245f), new PointF(4.89f, 9.245f) }),
            ("廊下", new[] { new PointF(11.224f, -2.246f), new PointF(13.554f, -2.246f), new PointF(13.554f, 5.615f), new PointF(11.224f, 5.615f) }),
            ("廊下", new[] { new PointF(-6.321f, -2.818f), new PointF(11.19f, -2.818f), new PointF(11.19f, -0.144f), new PointF(-6.321f, -0.144f) }),
            ("廊下", new[] { new PointF(24.67f, 7.839f), new PointF(23.094f, 7.859f), new PointF(21.25f, 6.065f), new PointF(21.263f, 5.648f), new PointF(24.729f, 5.67f) }),
            ("廊下", new[] { new PointF(14.299f, 6.14f), new PointF(12.675f, 7.718f), new PointF(10.965f, 7.698f), new PointF(11.16f, 5.663f), new PointF(13.83f, 5.675f) }),
        };

        // 壁の線 (当たり判定の線から、影などを除いたもの)
        public static readonly PointF[][] Walls =
        {
            new[] { new PointF(3.713f, -2.238f), new PointF(3.688f, -2.594f), new PointF(-6.193f, -2.624f), new PointF(-6.184f, -0.034f), new PointF(-6.714f, 0.028f), new PointF(-6.665f, 4.055f), new PointF(-2.5f, 4.009f), new PointF(-2.523f, 3.111f), new PointF(-2.509f, 2.238f), new PointF(-2.772f, 2.171f), new PointF(-2.802f, -0.754f), new PointF(3.695f, -0.667f) },
            new[] { new PointF(7.211f, 2.161f), new PointF(8.763f, 2.161f), new PointF(8.752f, 5.226f), new PointF(11.121f, 5.242f), new PointF(11.126f, 4.514f), new PointF(11.264f, 4.51f), new PointF(11.26f, 5.655f) },
            new[] { new PointF(3.696f, -2.227f), new PointF(13.574f, -2.236f), new PointF(13.574f, -0.387f), new PointF(13.715f, -0.395f), new PointF(13.707f, -2.223f), new PointF(17.078f, -2.222f), new PointF(17.086f, 1.597f), new PointF(13.696f, 1.582f), new PointF(13.689f, 0.693f), new PointF(13.572f, 0.692f), new PointF(13.564f, 3.416f), new PointF(13.71f, 3.405f), new PointF(13.699f, 2.461f), new PointF(16.672f, 2.455f), new PointF(16.671f, 5.129f), new PointF(16.251f, 4.925f), new PointF(15.902f, 5.099f), new PointF(14.853f, 5.113f), new PointF(14.641f, 4.892f), new PointF(14.307f, 5.121f), new PointF(14.091f, 5.118f), new PointF(14.058f, 5.477f), new PointF(13.718f, 5.463f), new PointF(13.713f, 4.504f), new PointF(13.561f, 4.517f), new PointF(13.554f, 5.709f) },
            new[] { new PointF(3.681f, -0.719f), new PointF(11.235f, -0.732f), new PointF(11.255f, 3.416f), new PointF(11.128f, 3.42f), new PointF(10.756f, 3.248f), new PointF(10.734f, 0.134f), new PointF(3.805f, 0.137f), new PointF(3.818f, 2.171f), new PointF(4.955f, 2.164f) },
            new[] { new PointF(24.392f, 5.647f), new PointF(24.404f, 5.443f), new PointF(29.133f, 5.464f), new PointF(29.132f, -0.574f), new PointF(26.708f, -0.568f), new PointF(26.731f, -1.519f), new PointF(26.96f, -1.377f), new PointF(28.654f, -1.375f), new PointF(28.657f, -2.749f), new PointF(20.334f, -2.765f), new PointF(20.339f, -3.77f), new PointF(17.934f, -3.754f), new PointF(17.97f, -1.38f), new PointF(20.87f, -1.529f), new PointF(20.884f, -0.576f), new PointF(17.957f, -0.569f), new PointF(17.971f, 0.804f), new PointF(18.939f, 0.78f), new PointF(18.933f, 1.599f), new PointF(17.981f, 1.602f), new PointF(17.962f, 5.457f), new PointF(22.089f, 5.465f) },
            new[] { new PointF(21.057f, 5.58f), new PointF(21.043f, 1.608f), new PointF(20.096f, 1.597f), new PointF(20.099f, 0.794f), new PointF(21.204f, 0.79f), new PointF(21.193f, 5.577f) },
            new[] { new PointF(11.255f, 5.621f), new PointF(11.251f, 7.638f), new PointF(12.73f, 7.648f), new PointF(16.707f, 11.604f), new PointF(16.704f, 15.951f) },
            new[] { new PointF(13.565f, 5.628f), new PointF(13.553f, 6.262f), new PointF(17.828f, 10.529f) },
            new[] { new PointF(22.092f, 5.628f), new PointF(22.101f, 6.269f), new PointF(17.827f, 10.532f) },
            new[] { new PointF(18.964f, 15.992f), new PointF(18.952f, 11.608f), new PointF(22.927f, 7.649f), new PointF(23.774f, 7.633f), new PointF(24.395f, 7.429f), new PointF(24.405f, 5.632f) },
            new[] { new PointF(7.221f, 8.834f), new PointF(7.222f, 13.321f), new PointF(7.355f, 13.315f), new PointF(7.348f, 9.647f), new PointF(12.081f, 9.675f), new PointF(12.108f, 14.145f), new PointF(7.977f, 14.132f), new PointF(7.966f, 14.568f), new PointF(5.895f, 14.569f), new PointF(5.34f, 13.755f), new PointF(4.995f, 13.867f), new PointF(4.972f, 10.771f), new PointF(4.854f, 10.773f), new PointF(4.83f, 14.374f), new PointF(3.731f, 14.373f), new PointF(2.888f, 13.485f), new PointF(2.055f, 13.479f), new PointF(1.204f, 14.394f), new PointF(0.082f, 14.397f), new PointF(0.097f, 9.667f), new PointF(4.987f, 9.656f), new PointF(4.975f, 8.83f) },
            new[] { new PointF(16.709f, 15.85f), new PointF(16.7f, 16.771f), new PointF(12.872f, 16.761f), new PointF(12.869f, 20.661f), new PointF(13.455f, 20.447f), new PointF(13.977f, 20.662f), new PointF(13.987f, 20.991f), new PointF(15.446f, 20.983f), new PointF(15.451f, 20.652f), new PointF(15.757f, 20.464f), new PointF(16.557f, 20.665f), new PointF(16.56f, 17.771f), new PointF(16.699f, 17.789f), new PointF(16.7f, 21.053f), new PointF(17.284f, 21.048f) },
            new[] { new PointF(17.29f, 21.039f), new PointF(17.288f, 21.875f), new PointF(12.746f, 21.876f), new PointF(12.76f, 22.574f), new PointF(13.107f, 23.375f), new PointF(13.734f, 24.108f), new PointF(15.409f, 25.057f), new PointF(16.663f, 25.383f), new PointF(17.844f, 25.459f), new PointF(19.556f, 25.264f), new PointF(21.223f, 24.611f), new PointF(22.234f, 23.814f), new PointF(22.852f, 22.846f), new PointF(22.946f, 21.837f), new PointF(18.395f, 21.886f), new PointF(18.396f, 21.045f) },
            new[] { new PointF(18.393f, 21.069f), new PointF(19.001f, 21.059f), new PointF(18.979f, 19.369f), new PointF(19.092f, 19.368f), new PointF(19.079f, 20.874f), new PointF(19.937f, 20.855f), new PointF(19.938f, 21.016f), new PointF(20.604f, 20.996f), new PointF(20.599f, 20.858f), new PointF(22.448f, 20.864f), new PointF(22.439f, 20.048f), new PointF(22.788f, 20.044f), new PointF(22.817f, 16.901f), new PointF(19.541f, 16.916f), new PointF(19.544f, 16.771f), new PointF(19.103f, 16.76f), new PointF(19.101f, 18.363f), new PointF(18.955f, 18.358f), new PointF(18.975f, 15.833f) },
            new[] { new PointF(7.212f, 9.849f), new PointF(7.219f, 2.166f) },
            new[] { new PointF(4.976f, 9.639f), new PointF(4.948f, 2.164f) },
        };
    }
}
