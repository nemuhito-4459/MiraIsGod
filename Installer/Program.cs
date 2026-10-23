// ================================================================
//  MiraIsGod インストーラー
// ================================================================
//
//  【何をするもの？】
//   1. ミラHQの地図 (ゲームから取り出した本物の部屋の形・壁・ベントの位置) を出す
//        ・つながり (線) を左クリックすると「通れる (緑) ⇔ 通れない (赤)」が切り替わる
//        ・つながり (線) を右クリックすると「一方通行 (▶) → 逆向き (◀) → 通れる」と切り替わる
//        ・ベントの丸を左クリックすると「入れる / 入れない (×)」が切り替わる
//   2. 「インストール」を押すと、Among Us の BepInEx フォルダに直接入れる
//        ・BepInEx/plugins/MiraIsGod.dll                     … プラグイン本体 (この .exe の中に入っている)
//        ・BepInEx/config/com.nemuhito4459.miraisgod.cfg     … 選んだ「通れない組み合わせ」と「入れないベント」
//   3. 「アンインストール」を押すと、その2つを消す
//   4. 強化ドアログ: オン・オフと、センサーの場所 (「センサーを置く」で地図をクリック。右クリックで消す)
//        置いたセンサーは、左クリックで選ぶと、ドラッグで動かせる。形 (丸・四角)・幅・高さ・角度も変えられる
//        1つも置かないときは、本物のセンサー3か所を使う
//
//  【メモ】
//   ・Among Us を起動したままだと、DLL を上書きできないことがある → 先に閉じる
//   ・Among Us のフォルダは、ふつうは
//       C:\Program Files (x86)\Steam\steamapps\common\Among Us
//     違う場所に入れている人は「参照...」で選ぶ
//   ・設定ファイルは、プラグイン (Plugin.cs の Load) が起動時に読む
//     書き方は「番号-番号」をカンマで区切ったもの (例: 6-7,1-2)
//
// ================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace MiraIsGodInstaller
{
    static class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            // 画面の拡大 (125% など) を Windows に任せず、自分できれいに描く (ぼやけ防止)
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ----------------------------------------------------------------
    //  ミラHQのベントのデータ
    // ----------------------------------------------------------------
    static class Mira
    {
        // 図の大きさ (画面の拡大が 100% のときのピクセル数)
        public const int CanvasW = 700, CanvasH = 500;

        // ゲームの中の座標で、図に入れる範囲 (ミラHQ全体が入るように少し余白をとっている)
        const float MinX = -12.5f, MaxX = 30.2f, MinY = -4.6f, MaxY = 26.5f;
        static readonly float Scale = System.Math.Min(CanvasW / (MaxX - MinX), CanvasH / (MaxY - MinY));
        static readonly float OffX = (CanvasW - (MaxX - MinX) * Scale) / 2f;
        static readonly float OffY = (CanvasH - (MaxY - MinY) * Scale) / 2f;

        // ゲームの中の座標 → 図の上の位置
        // ゲームは「上ほど y が大きい」、画面は「下ほど y が大きい」ので、上下をひっくり返す
        // 図の上の位置 → ゲームの中の座標 (ToCanvas の逆)
        public static PointF ToWorld(PointF c) =>
            new PointF(MinX + (c.X - OffX) / Scale, MaxY - (c.Y - OffY) / Scale);

        // ゲームの中の 1 マス (1.0) が、図の上で何ピクセルか
        public static float PixelsPerUnit => Scale;

        public static PointF ToCanvas(PointF w) =>
            new PointF(OffX + (w.X - MinX) * Scale, OffY + (MaxY - w.Y) * Scale);

        // ベントの番号 → 部屋の名前 と 図の上の位置 (位置は MapData.cs の本物の座標から計算)
        public static readonly Dictionary<int, (string Name, PointF Pos)> Vents = new Dictionary<int, (string, PointF)>
        {
            { 11, ("発射台",         ToCanvas(MapData.Vents[11])) },
            {  3, ("原子炉",         ToCanvas(MapData.Vents[3])) },
            {  4, ("研究室",         ToCanvas(MapData.Vents[4])) },
            {  9, ("除染室",         ToCanvas(MapData.Vents[9])) },
            { 10, ("ロッカールーム", ToCanvas(MapData.Vents[10])) },
            {  8, ("医務室",         ToCanvas(MapData.Vents[8])) },
            {  5, ("オフィス",       ToCanvas(MapData.Vents[5])) },
            {  7, ("温室",           ToCanvas(MapData.Vents[7])) },
            {  6, ("管理室",         ToCanvas(MapData.Vents[6])) },
            {  2, ("廊下",           ToCanvas(MapData.Vents[2])) },
            {  1, ("バルコニー",     ToCanvas(MapData.Vents[1])) },
        };

        // 本物のドアログのセンサー (名前, 中心, 大きさ)。ゲームのログで調べたもの
        public static readonly (string Name, PointF Center, SizeF Size)[] RealSensors =
        {
            ("北",   new PointF(17.83f, 15.75f), new SizeF(2.27f, 0.15f)),
            ("南西", new PointF(13.30f,  7.12f), new SizeF(1.21f, 1.21f)),
            ("南東", new PointF(22.36f,  7.12f), new SizeF(1.21f, 1.21f)),
        };

        // ドアログの机の場所と、使える距離 (ゲームのログで調べたもの)
        public static readonly PointF DoorLogDesk = new PointF(16.22f, 5.82f);
        public const float DoorLogRange = 1.5f;

        // ベントのつながり (ゲームのログで調べた本物の14本)
        public static readonly (int A, int B)[] Links =
        {
            (11, 3), (11, 10),
            (3, 4), (3, 9),
            (4, 9), (4, 5),
            (10, 8), (10, 9),
            (6, 7), (6, 2), (6, 5),
            (5, 7),
            (8, 1), (1, 2),
        };

        // おすすめ (最初に選ばれている「通れない」組み合わせ)
        public const string DefaultPairs = "6-7,6-5,5-4,4-3,3-9,9-10,10-11,10-8,1-2";

        // つながりを「小さい番号-大きい番号」の文字にする (向きをそろえて比べやすくする)
        public static string Key(int a, int b) => a < b ? $"{a}-{b}" : $"{b}-{a}";

        // 「6-7,1-2」のような文字を、つながりの集まりに直す
        public static HashSet<string> Parse(string text)
        {
            var set = new HashSet<string>();
            foreach (string part in (text ?? "").Split(','))
            {
                string[] ab = part.Trim().Split('-');
                if (ab.Length == 2 && int.TryParse(ab[0], out int a) && int.TryParse(ab[1], out int b))
                    set.Add(Key(a, b));
            }
            return set;
        }
    }

    // ----------------------------------------------------------------
    //  画面
    // ----------------------------------------------------------------
    class MainForm : Form
    {
        const string PluginFile = "MiraIsGod.dll";
        const string ConfigFile = "com.nemuhito4459.miraisgod.cfg";

        // 前の名前 (MiraVentLock) のときのファイル。インストールのときに消す (2つ動かないように)
        const string OldPluginFile = "MiraVentLock.dll";
        const string OldConfigFile = "com.nemuhito4459.miraventlock.cfg";
        const string DefaultGameDir = @"C:\Program Files (x86)\Steam\steamapps\common\Among Us";

        // 今「通れない」になっているつながり
        HashSet<string> blocked = Mira.Parse(Mira.DefaultPairs);

        // 入れないベント (封鎖) の番号
        HashSet<int> sealedVents = new HashSet<int>();

        // 一方通行: つながり ("3-11") → 行ける方向 (From, To)
        Dictionary<string, (int From, int To)> oneWay = new Dictionary<string, (int, int)>();

        // プラグイン全体の ON / OFF (ゲームのロビーで切り替えるもの。インストールし直しても消えないように、読んで書き戻す)
        bool pluginEnabled = true;

        // 強化ドアログ
        bool doorLogEnabled = true;                                        // 使うか
        List<Sensor> sensors = new List<Sensor>();                         // 自分で置いたセンサー (ゲームの座標)。空なら本物の3か所
        bool placing;                                                      // 「センサーを置く」モード中か
        int selected = -1;                                                 // 選んでいるセンサーの番号 (-1 = なし)
        bool dragging;                                                     // ドラッグで動かしている最中か
        PointF dragOffset;                                                 // つかんだ場所とセンサーの中心のずれ (ゲームの座標)
        bool loadingEditor;                                                // 編集欄に値を入れている最中 (このときは変更を反映しない)
        TextBox nameBox;
        ComboBox shapeBox;
        NumericUpDown numW, numH, numA;
        Label lblH, lblA;
        CheckBox doorLogCheck;
        Button placeBtn;
        Label sensorLabel;

        // マウスが乗っているもの (なければ null)
        // つながりなら "6-7"、ベントなら "v6" のような文字
        string hover;

        readonly MapPanel map;
        readonly TextBox dirBox;
        readonly Label dirStatus;
        readonly Label summary;

        public MainForm()
        {
            Text = "MiraIsGod インストーラー";
            // 画面の拡大 (125% など) に合わせて、ボタンや文字を大きくする (ぼやけないように)
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Yu Gothic UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(732, 830);
            BackColor = Color.FromArgb(245, 246, 248);

            var title = new Label
            {
                Text = "ミラHQのベント制限",
                Font = new Font("Yu Gothic UI", 14f, FontStyle.Bold),
                Location = new Point(16, 10),
                AutoSize = true,
            };
            var help = new Label
            {
                Text = "線を左クリック: 通れる ⇔ 通れない　／　線を右クリック: 一方通行　／　丸を左クリック: 入れない (×)",
                Location = new Point(18, 44),
                AutoSize = true,
            };

            // ベントの図
            map = new MapPanel { Location = new Point(16, 70), Size = new Size(Mira.CanvasW, Mira.CanvasH) };
            map.Paint += DrawMap;
            map.MouseDown += (s, e) =>
            {
                // センサーを左ボタンで押したら、選んで、ドラッグを始める
                string h = HitTest(e.Location);
                if (e.Button == MouseButtons.Left && h != null && h.StartsWith("s"))
                {
                    Select(int.Parse(h.Substring(1)));
                    PointF w = MouseToWorld(e.Location);
                    dragOffset = new PointF(sensors[selected].Pos.X - w.X, sensors[selected].Pos.Y - w.Y);
                    dragging = true;
                }
            };
            map.MouseUp += (s, e) => dragging = false;
            map.MouseMove += (s, e) =>
            {
                if (dragging && selected >= 0)
                {
                    // ドラッグ中: マウスに合わせてセンサーを動かす
                    PointF w = MouseToWorld(e.Location);
                    sensors[selected].Pos = new PointF(w.X + dragOffset.X, w.Y + dragOffset.Y);
                    map.Invalidate();
                    return;
                }
                string h = HitTest(e.Location);
                if (h != hover) { hover = h; map.Cursor = h != null ? Cursors.Hand : (placing ? Cursors.Cross : Cursors.Default); map.Invalidate(); }
            };
            map.MouseLeave += (s, e) => { hover = null; map.Invalidate(); };
            map.MouseClick += (s, e) =>
            {
                string h = HitTest(e.Location);

                // センサー: 右クリックで消す (左クリックは MouseDown で「選ぶ」をしている)
                if (h != null && h.StartsWith("s"))
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        sensors.RemoveAt(int.Parse(h.Substring(1)));
                        Select(-1);
                    }
                    UpdateSummary();
                    map.Invalidate();
                    return;
                }

                // 何もないところを左クリックしたら、選ぶのをやめる
                if (!placing && e.Button == MouseButtons.Left && h == null) Select(-1);

                // 「センサーを置く」モード中の左クリック: その場所にセンサーを置く
                if (placing && e.Button == MouseButtons.Left)
                {
                    var c = new PointF(e.X * (float)Mira.CanvasW / map.Width, e.Y * (float)Mira.CanvasH / map.Height);
                    string name = Microsoft.VisualBasic.Interaction.InputBox("センサーの名前を入れてください (例: 北、管理室前)", "センサーを置く", $"センサー{sensors.Count + 1}");
                    name = name.Replace(":", "").Replace(";", "").Trim(); // 設定ファイルの区切りの文字は使えない
                    if (name == "") return;                                // キャンセル
                    sensors.Add(new Sensor { Name = name, Pos = Mira.ToWorld(c) }); // 最初は丸 (直径2)
                    Select(sensors.Count - 1);
                    UpdateSummary();
                    map.Invalidate();
                    return;
                }

                if (h == null) return;
                if (h.StartsWith("v"))
                {
                    if (e.Button != MouseButtons.Left) return; // ベントの丸は左クリックだけ
                    // ベントの丸: 入れる ⇔ 入れない
                    int id = int.Parse(h.Substring(1));
                    if (!sealedVents.Remove(id)) sealedVents.Add(id);
                }
                else if (e.Button == MouseButtons.Left)
                {
                    // つながりを左クリック: 通れる (緑) ⇔ 通れない (赤)
                    // 一方通行だったときは、通れる (緑) に戻す
                    if (oneWay.Remove(h)) { }
                    else if (!blocked.Remove(h)) blocked.Add(h);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    // つながりを右クリック: 一方通行 (▶) → 逆向きの一方通行 (◀) → 通れる (緑)
                    string[] ab = h.Split('-');
                    int a = int.Parse(ab[0]), b = int.Parse(ab[1]); // a < b になっている
                    blocked.Remove(h);
                    if (!oneWay.TryGetValue(h, out var ow)) oneWay[h] = (a, b);
                    else if (ow.From == a) oneWay[h] = (b, a);
                    else oneWay.Remove(h);
                }
                UpdateSummary();
                map.Invalidate();
            };

            // 図の下のボタン
            var resetBtn = new Button { Text = "おすすめに戻す", Location = new Point(16, 578), Size = new Size(130, 30) };
            resetBtn.Click += (s, e) => { blocked = Mira.Parse(Mira.DefaultPairs); sealedVents.Clear(); oneWay.Clear(); UpdateSummary(); map.Invalidate(); };
            var openBtn = new Button { Text = "全部通れる", Location = new Point(152, 578), Size = new Size(110, 30) };
            openBtn.Click += (s, e) => { blocked.Clear(); sealedVents.Clear(); oneWay.Clear(); UpdateSummary(); map.Invalidate(); };
            summary = new Label { Location = new Point(272, 584), AutoSize = true };

            // 強化ドアログ
            doorLogCheck = new CheckBox { Text = "強化ドアログを使う", Location = new Point(16, 622), AutoSize = true, Checked = true };
            doorLogCheck.CheckedChanged += (s, e) => { doorLogEnabled = doorLogCheck.Checked; map.Invalidate(); };
            placeBtn = new Button { Text = "センサーを置く", Location = new Point(190, 616), Size = new Size(150, 30) };
            placeBtn.Click += (s, e) =>
            {
                placing = !placing;
                placeBtn.Text = placing ? "置くのをやめる" : "センサーを置く";
                placeBtn.BackColor = placing ? Color.FromArgb(250, 220, 120) : SystemColors.Control;
                map.Cursor = placing ? Cursors.Cross : Cursors.Default;
            };
            var clearSensorsBtn = new Button { Text = "本物の3か所に戻す", Location = new Point(346, 616), Size = new Size(150, 30) };
            clearSensorsBtn.Click += (s, e) => { sensors.Clear(); Select(-1); UpdateSummary(); map.Invalidate(); };
            sensorLabel = new Label { Location = new Point(506, 622), AutoSize = true };

            // 選んだセンサーの編集欄 (名前・形・幅・高さ・角度)
            var selLabel = new Label { Text = "選んだセンサー:", Location = new Point(16, 662), AutoSize = true };
            nameBox = new TextBox { Location = new Point(118, 658), Size = new Size(110, 26) };
            shapeBox = new ComboBox { Location = new Point(236, 658), Size = new Size(64, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            shapeBox.Items.AddRange(new object[] { "丸", "四角" });
            var lblW = new Label { Text = "幅", Location = new Point(308, 662), AutoSize = true };
            numW = new NumericUpDown { Location = new Point(330, 658), Size = new Size(64, 26), DecimalPlaces = 1, Increment = 0.1m, Minimum = 0.2m, Maximum = 30m };
            lblH = new Label { Text = "高さ", Location = new Point(402, 662), AutoSize = true };
            numH = new NumericUpDown { Location = new Point(436, 658), Size = new Size(64, 26), DecimalPlaces = 1, Increment = 0.1m, Minimum = 0.2m, Maximum = 30m };
            lblA = new Label { Text = "角度", Location = new Point(508, 662), AutoSize = true };
            numA = new NumericUpDown { Location = new Point(542, 658), Size = new Size(64, 26), Increment = 5m, Minimum = -180m, Maximum = 180m };
            var dragHint = new Label { Text = "ドラッグで移動", Location = new Point(614, 662), AutoSize = true, ForeColor = Color.Gray };
            nameBox.TextChanged += (s, e) => EditorChanged();
            shapeBox.SelectedIndexChanged += (s, e) => EditorChanged();
            numW.ValueChanged += (s, e) => EditorChanged();
            numH.ValueChanged += (s, e) => EditorChanged();
            numA.ValueChanged += (s, e) => EditorChanged();

            // Among Us のフォルダ
            var dirLabel = new Label { Text = "Among Us のフォルダ:", Location = new Point(16, 702), AutoSize = true };
            dirBox = new TextBox { Location = new Point(16, 724), Size = new Size(612, 26) };
            dirBox.TextChanged += (s, e) => CheckDir();
            var browseBtn = new Button { Text = "参照...", Location = new Point(636, 722), Size = new Size(80, 28) };
            browseBtn.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog { Description = "Among Us のフォルダ (Among Us.exe があるところ) を選んでください" })
                {
                    if (Directory.Exists(dirBox.Text)) dlg.SelectedPath = dirBox.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK) dirBox.Text = dlg.SelectedPath;
                }
            };
            dirStatus = new Label { Location = new Point(16, 754), AutoSize = true };

            // インストール・アンインストール
            var installBtn = new Button
            {
                Text = "インストール",
                Font = new Font("Yu Gothic UI", 11f, FontStyle.Bold),
                Location = new Point(476, 780),
                Size = new Size(240, 40),
                BackColor = Color.FromArgb(46, 160, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            installBtn.FlatAppearance.BorderSize = 0;
            installBtn.Click += (s, e) => Install();
            var uninstallBtn = new Button { Text = "アンインストール", Location = new Point(16, 786), Size = new Size(130, 30) };
            uninstallBtn.Click += (s, e) => Uninstall();

            Controls.AddRange(new Control[] { title, help, map, resetBtn, openBtn, summary, doorLogCheck, placeBtn, clearSensorsBtn, sensorLabel, selLabel, nameBox, shapeBox, lblW, numW, lblH, numH, lblA, numA, dragHint, dirLabel, dirBox, browseBtn, dirStatus, installBtn, uninstallBtn });

            // 最初の状態: いつもの場所に Among Us があれば入れておく
            dirBox.Text = Directory.Exists(DefaultGameDir) ? DefaultGameDir : "";
            LoadExistingConfig();
            doorLogCheck.Checked = doorLogEnabled;
            Select(-1);
            UpdateSummary();
            CheckDir();
        }

        // ===== 図を描く =====
        void DrawMap(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            // 画面の拡大に合わせて、図全体を大きくして描く (図の中の座標は 700x500 のまま)
            g.ScaleTransform(map.Width / (float)Mira.CanvasW, map.Height / (float)Mira.CanvasH);
            g.Clear(Color.FromArgb(24, 28, 38));

            // 部屋 (本物の形で塗る)
            using (var roomFill = new SolidBrush(Color.FromArgb(52, 60, 78)))
            using (var roomLine = new Pen(Color.FromArgb(78, 88, 110), 1f))
                foreach (var (_, shape) in MapData.Rooms)
                {
                    PointF[] pts = shape.Select(Mira.ToCanvas).ToArray();
                    g.FillPolygon(roomFill, pts);
                    g.DrawPolygon(roomLine, pts);
                }

            // 壁の線
            using (var wall = new Pen(Color.FromArgb(120, 132, 158), 1.5f))
                foreach (PointF[] w in MapData.Walls)
                    if (w.Length >= 2) g.DrawLines(wall, w.Select(Mira.ToCanvas).ToArray());

            // 部屋の名前 (いちばん大きい形の真ん中に、うすく書く。廊下は書かない)
            using (var roomText = new SolidBrush(Color.FromArgb(150, 160, 185)))
            using (var roomFont = new Font("Yu Gothic UI", 8f))
            {
                var c = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                foreach (var grp in MapData.Rooms.Where(r => r.Name != "廊下").GroupBy(r => r.Name))
                {
                    PointF[] pts = grp.OrderByDescending(r => Area(r.Shape)).First().Shape.Select(Mira.ToCanvas).ToArray();
                    var center = new PointF(pts.Average(p => p.X), pts.Average(p => p.Y));
                    g.DrawString(grp.Key, roomFont, roomText, new RectangleF(center.X - 50, center.Y - 30, 100, 16), c);
                }
            }

            // つながり (線)
            foreach (var (a, b) in Mira.Links)
            {
                string key = Mira.Key(a, b);
                bool isBlocked = blocked.Contains(key);
                bool isHover = key == hover;
                bool touchesSealed = sealedVents.Contains(a) || sealedVents.Contains(b); // 入れないベントにつながる線は、どちらにしても通れない
                Color color = touchesSealed ? Color.FromArgb(110, 110, 120)
                            : isBlocked ? Color.FromArgb(230, 80, 80) : Color.FromArgb(90, 210, 120);
                if (touchesSealed) isBlocked = true; // 点線で描く
                bool isOneWay = !touchesSealed && oneWay.TryGetValue(key, out var ow);
                if (isOneWay) color = Color.FromArgb(240, 170, 60); // 一方通行はオレンジ
                using (var pen = new Pen(color, isHover ? 7f : 4f))
                {
                    if (isBlocked) pen.DashStyle = DashStyle.Dash;
                    g.DrawLine(pen, Mira.Vents[a].Pos, Mira.Vents[b].Pos);
                }
                if (isOneWay) DrawArrowHead(g, Mira.Vents[oneWay[key].From].Pos, Mira.Vents[oneWay[key].To].Pos, color);
            }

            // ベント (丸) と部屋の名前
            using (var fill = new SolidBrush(Color.FromArgb(40, 44, 56)))
            using (var ring = new Pen(Color.White, 2f))
            using (var white = new SolidBrush(Color.White))
            using (var numFont = new Font("Yu Gothic UI", 9f, FontStyle.Bold))
                        {
                var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                foreach (var kv in Mira.Vents)
                {
                    PointF p = kv.Value.Pos;
                    bool isSealed = sealedVents.Contains(kv.Key);
                    bool isHover = hover == "v" + kv.Key;
                    float size = isHover ? 15 : 13;
                    var r = new RectangleF(p.X - size, p.Y - size, size * 2, size * 2);
                    using (var f = new SolidBrush(isSealed ? Color.FromArgb(150, 40, 40) : Color.FromArgb(40, 44, 56)))
                        g.FillEllipse(f, r);
                    g.DrawEllipse(ring, r);
                    if (isSealed)
                    {
                        // 入れないベントは「×」
                        using (var x = new Pen(Color.White, 2.5f))
                        {
                            g.DrawLine(x, p.X - 7, p.Y - 7, p.X + 7, p.Y + 7);
                            g.DrawLine(x, p.X + 7, p.Y - 7, p.X - 7, p.Y + 7);
                        }
                    }
                    else g.DrawString(kv.Key.ToString(), numFont, white, r, center);
                }
            }

            // センサー (自分で置いたもの)。強化ドアログを使わないときは、うすく描く
            using (var sensorFont = new Font("Yu Gothic UI", 8.5f, FontStyle.Bold))
            {
                int alpha = doorLogEnabled ? 255 : 90;
                for (int i = 0; i < sensors.Count; i++)
                {
                    Sensor sn = sensors[i];
                    PointF c = Mira.ToCanvas(sn.Pos);
                    bool isHover = hover == "s" + i;
                    bool isSel = i == selected;
                    float labelX;
                    using (var f = new SolidBrush(Color.FromArgb(isHover || isSel ? 140 : 90, 250, 210, 60)))
                    using (var o = new Pen(isSel ? Color.White : Color.FromArgb(alpha, 250, 210, 60), isSel ? 2.5f : 2f))
                    {
                        if (sn.IsRect)
                        {
                            // 四角: 4つの角 (ゲームの座標) を図の上の位置にして描く (斜めもそのまま描ける)
                            PointF[] pts = sn.Corners().Select(Mira.ToCanvas).ToArray();
                            g.FillPolygon(f, pts);
                            g.DrawPolygon(o, pts);
                            labelX = pts.Max(p => p.X);
                        }
                        else
                        {
                            float pr = Math.Max(sn.W / 2 * Mira.PixelsPerUnit, 6);
                            g.FillEllipse(f, c.X - pr, c.Y - pr, pr * 2, pr * 2);
                            g.DrawEllipse(o, c.X - pr, c.Y - pr, pr * 2, pr * 2);
                            labelX = c.X + pr;
                        }
                    }
                    using (var t = new SolidBrush(Color.FromArgb(alpha, 255, 235, 150)))
                        g.DrawString(sn.Name, sensorFont, t, labelX + 2, c.Y - 8);
                }
            }

            // 本物のセンサー (センサーを置いていないときに使われる) と、ドアログの机
            // 座標はゲームのログで調べたもの
            using (var realFont = new Font("Yu Gothic UI", 8f))
            {
                bool useReal = sensors.Count == 0;
                int alpha = doorLogEnabled && useReal ? 230 : 70;
                using (var f = new SolidBrush(Color.FromArgb(alpha / 2, 120, 200, 255)))
                using (var o = new Pen(Color.FromArgb(alpha, 120, 200, 255), 2f))
                using (var t = new SolidBrush(Color.FromArgb(alpha, 170, 220, 255)))
                    foreach (var (name, center, size) in Mira.RealSensors)
                    {
                        PointF a = Mira.ToCanvas(new PointF(center.X - size.Width / 2, center.Y + size.Height / 2)); // 左上
                        PointF b = Mira.ToCanvas(new PointF(center.X + size.Width / 2, center.Y - size.Height / 2)); // 右下
                        var r = RectangleF.FromLTRB(a.X, a.Y, b.X, Math.Max(b.Y, a.Y + 3));
                        g.FillRectangle(f, r);
                        g.DrawRectangle(o, r.X, r.Y, r.Width, r.Height);
                        g.DrawString(name, realFont, t, r.Right + 3, r.Y - 4);
                    }

                // ドアログの机と、使う範囲 (この丸の中に1秒いると、チャットが届く)
                PointF d = Mira.ToCanvas(Mira.DoorLogDesk);
                float dr = Mira.DoorLogRange * Mira.PixelsPerUnit;
                using (var o = new Pen(Color.FromArgb(doorLogEnabled ? 220 : 70, 200, 160, 255), 2f) { DashStyle = DashStyle.Dot })
                using (var t = new SolidBrush(Color.FromArgb(doorLogEnabled ? 230 : 70, 220, 200, 255)))
                {
                    g.DrawEllipse(o, d.X - dr, d.Y - dr, dr * 2, dr * 2);
                    g.FillRectangle(t, d.X - 4, d.Y - 4, 8, 8);
                    g.DrawString("ドアログ", realFont, t, d.X - 20, d.Y + dr + 1);
                }
            }

            // 説明 (左上)
            using (var hint = new SolidBrush(Color.FromArgb(180, 190, 210)))
                g.DrawString("緑 = 通れる\n赤い点線 = 通れない\nオレンジ▶ = 一方通行\n× = 入れないベント\n青い四角 = 本物のセンサー\n黄色の丸 = 自分で置いたセンサー\n紫の点線 = ドアログを使う範囲", Font, hint, 10, 10);
        }

        // マウスの位置 (画面) → ゲームの中の座標
        PointF MouseToWorld(Point m) =>
            Mira.ToWorld(new PointF(m.X * (float)Mira.CanvasW / map.Width, m.Y * (float)Mira.CanvasH / map.Height));

        // センサーを選ぶ (-1 で選ぶのをやめる)。編集欄に、そのセンサーの値を入れる
        void Select(int index)
        {
            selected = index;
            bool has = index >= 0 && index < sensors.Count;
            loadingEditor = true;
            foreach (Control c in new Control[] { nameBox, shapeBox, numW, numH, numA }) c.Enabled = has;
            if (has)
            {
                Sensor sn = sensors[index];
                nameBox.Text = sn.Name;
                shapeBox.SelectedIndex = sn.IsRect ? 1 : 0;
                numW.Value = (decimal)Math.Max(0.2f, Math.Min(30f, sn.W));
                numH.Value = (decimal)Math.Max(0.2f, Math.Min(30f, sn.H));
                numA.Value = (decimal)Math.Max(-180f, Math.Min(180f, sn.Angle));
            }
            else nameBox.Text = "";
            // 丸のときは、高さと角度は使わない (幅 = 直径)
            bool rect = has && sensors[index].IsRect;
            numH.Enabled = lblH.Enabled = rect;
            numA.Enabled = lblA.Enabled = rect;
            loadingEditor = false;
            map.Invalidate();
        }

        // 編集欄が変わったら、選んでいるセンサーに反映する
        void EditorChanged()
        {
            if (loadingEditor || selected < 0 || selected >= sensors.Count) return;
            Sensor sn = sensors[selected];
            string name = nameBox.Text.Replace(":", "").Replace(";", "").Trim();
            if (name != "") sn.Name = name;
            sn.IsRect = shapeBox.SelectedIndex == 1;
            sn.W = (float)numW.Value;
            sn.H = (float)numH.Value;
            sn.Angle = (float)numA.Value;
            numH.Enabled = lblH.Enabled = sn.IsRect;
            numA.Enabled = lblA.Enabled = sn.IsRect;
            map.Invalidate();
        }

        // 一方通行の矢印 (線の真ん中に、行ける方向へ向いた三角を描く)
        static void DrawArrowHead(Graphics g, PointF from, PointF to, Color color)
        {
            float dx = to.X - from.X, dy = to.Y - from.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;
            dx /= len; dy /= len;                                    // 向き (長さ1)
            var tip = new PointF((from.X + to.X) / 2 + dx * 9, (from.Y + to.Y) / 2 + dy * 9); // 三角の先
            var back = new PointF(tip.X - dx * 18, tip.Y - dy * 18);                         // 三角の根元の真ん中
            var left = new PointF(back.X - dy * 9, back.Y + dx * 9);
            var right = new PointF(back.X + dy * 9, back.Y - dx * 9);
            using (var b = new SolidBrush(color))
                g.FillPolygon(b, new[] { tip, left, right });
        }

        // 形の広さ (部屋の名前をどこに書くか決めるのに使う)
        static float Area(PointF[] pts)
        {
            float a = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                PointF p = pts[i], q = pts[(i + 1) % pts.Length];
                a += p.X * q.Y - q.X * p.Y;
            }
            return System.Math.Abs(a) / 2f;
        }

        // マウスの位置に近いつながりを探す (8ピクセル以内)
        string HitTest(Point m)
        {
            // 画面の拡大を戻して、図の中の座標にする
            m = new Point((int)(m.X * (float)Mira.CanvasW / map.Width), (int)(m.Y * (float)Mira.CanvasH / map.Height));

            // センサーをいちばん先に調べる
            PointF mw = Mira.ToWorld(m);
            for (int i = sensors.Count - 1; i >= 0; i--)          // 上に描いたもの (後ろのもの) を先に
                if (sensors[i].Contains(mw, 8f / Mira.PixelsPerUnit)) return "s" + i; // 小さくても8ピクセルまでは押せる

            // 「センサーを置く」モード中は、ベントや線は選ばない
            if (placing) return null;

            // ベントの丸を先に調べる (丸の上なら、線よりベントを優先)
            foreach (var kv in Mira.Vents)
            {
                double dx = m.X - kv.Value.Pos.X, dy = m.Y - kv.Value.Pos.Y;
                if (dx * dx + dy * dy <= 15 * 15) return "v" + kv.Key;
            }

            string best = null;
            double bestDist = 8;
            foreach (var (a, b) in Mira.Links)
            {
                double d = DistanceToSegment(m, Mira.Vents[a].Pos, Mira.Vents[b].Pos);
                if (d < bestDist) { bestDist = d; best = Mira.Key(a, b); }
            }
            return best;
        }

        // 点 p と、線分 a-b のいちばん近いところまでの距離
        static double DistanceToSegment(Point p, PointF a, PointF b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double x = a.X + t * dx - p.X, y = a.Y + t * dy - p.Y;
            return Math.Sqrt(x * x + y * y);
        }

        // 「通れない: 9本」のような表示を更新
        void UpdateSummary()
        {
            summary.Text = $"通れない: {blocked.Count}本　一方通行: {oneWay.Count}本　入れないベント: {sealedVents.Count}個";
            if (sensorLabel != null)
                sensorLabel.Text = sensors.Count == 0 ? "センサー: 本物の3か所" : $"センサー: {sensors.Count}個 (右クリックで消す)";
        }

        // ===== フォルダ =====
        string BepInExDir => Path.Combine(dirBox.Text.Trim(), "BepInEx");

        // そのフォルダに BepInEx があるか確かめて、下に表示する
        bool CheckDir()
        {
            bool ok = Directory.Exists(BepInExDir);
            dirStatus.Text = ok ? "✔ BepInEx が見つかりました" : "✘ このフォルダに BepInEx がありません (MOD を入れた Among Us のフォルダを選んでください)";
            dirStatus.ForeColor = ok ? Color.FromArgb(30, 130, 70) : Color.FromArgb(200, 60, 60);
            return ok;
        }

        // もう設定ファイルがあれば、その組み合わせを読んで図に反映する (前に選んだものを覚えておくため)
        void LoadExistingConfig()
        {
            try
            {
                string path = Path.Combine(BepInExDir, "config", ConfigFile);
                // 新しい設定ファイルがなければ、前の名前のときの設定ファイルを読む (選んだものを引き継ぐ)
                if (!File.Exists(path)) path = Path.Combine(BepInExDir, "config", OldConfigFile);
                if (!File.Exists(path)) return;
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    int eq = t.IndexOf('=');
                    if (eq < 0) continue;
                    string value = t.Substring(eq + 1);
                    if (t.StartsWith("BlockedPairs")) blocked = Mira.Parse(value);
                    if (t.StartsWith("OneWay"))
                    {
                        oneWay.Clear();
                        foreach (string part in value.Split(','))
                        {
                            string[] ab = part.Trim().Split('>');
                            if (ab.Length == 2 && int.TryParse(ab[0], out int a) && int.TryParse(ab[1], out int b))
                                oneWay[Mira.Key(a, b)] = (a, b);
                        }
                    }
                    if (t.StartsWith("PluginEnabled")) pluginEnabled = value.Trim().ToLower() != "false";
                    if (t.StartsWith("Enabled")) doorLogEnabled = value.Trim().ToLower() != "false";
                    if (t.StartsWith("Sensors"))
                    {
                        sensors.Clear();
                        var inv = System.Globalization.CultureInfo.InvariantCulture;
                        foreach (string part in value.Split(';'))
                        {
                            string p = part.Trim();
                            int colon = p.LastIndexOf(':');
                            if (colon <= 0) continue;
                            string[] nums = p.Substring(colon + 1).Split(',');
                            Sensor sn = Sensor.Parse(p.Substring(0, colon), nums);
                            if (sn != null) sensors.Add(sn);
                        }
                    }
                    if (t.StartsWith("SealedVents"))
                        sealedVents = new HashSet<int>(value.Split(',').Select(v => int.TryParse(v.Trim(), out int n) ? n : -1).Where(n => n > 0));
                }
            }
            catch { /* 読めなくても、おすすめのままで続ける */ }
        }

        // Among Us が起動中なら true
        static bool GameRunning() => Process.GetProcessesByName("Among Us").Length > 0;

        // ===== インストール =====
        void Install()
        {
            if (!CheckDir())
            {
                MessageBox.Show(this, "Among Us のフォルダに BepInEx が見つかりません。\n「参照...」で、MOD を入れた Among Us のフォルダを選んでください。", "インストールできません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (GameRunning())
            {
                MessageBox.Show(this, "Among Us が起動しています。\nAmong Us を閉じてから、もう一度「インストール」を押してください。", "インストールできません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 1) プラグイン本体を plugins に書き出す (この .exe の中に入っているものを取り出す)
                string pluginsDir = Path.Combine(BepInExDir, "plugins");
                Directory.CreateDirectory(pluginsDir);
                using (Stream src = Assembly.GetExecutingAssembly().GetManifestResourceStream(PluginFile))
                using (FileStream dst = File.Create(Path.Combine(pluginsDir, PluginFile)))
                    src.CopyTo(dst);

                // 前の名前 (MiraVentLock) のファイルが残っていたら消す (同じプラグインが2つ動かないように)
                string oldDll = Path.Combine(pluginsDir, OldPluginFile);
                if (File.Exists(oldDll)) File.Delete(oldDll);
                string oldCfg = Path.Combine(BepInExDir, "config", OldConfigFile);
                if (File.Exists(oldCfg)) File.Delete(oldCfg);

                // 2) 設定ファイルを config に書き出す (BepInEx の設定ファイルの書き方)
                string configDir = Path.Combine(BepInExDir, "config");
                Directory.CreateDirectory(configDir);
                string pairs = string.Join(",", blocked.OrderBy(k => k));
                var sb = new StringBuilder();
                sb.AppendLine("## MiraIsGod インストーラーで作った設定ファイル");
                sb.AppendLine();
                sb.AppendLine("[General]");
                sb.AppendLine();
                sb.AppendLine("## プラグイン全体を使うか (true = ON / false = OFF)。ロビーで左下の表示をクリックしても切り替えられる。");
                sb.AppendLine("# Setting type: Boolean");
                sb.AppendLine("# Default value: true");
                sb.AppendLine("PluginEnabled = " + (pluginEnabled ? "true" : "false"));
                sb.AppendLine();
                sb.AppendLine("[Vent]");
                sb.AppendLine();
                sb.AppendLine("## 通れなくするベントの組み合わせ。「番号-番号」をカンマで区切って書く (例: 6-7,1-2)。空にすると制限なし。");
                sb.AppendLine("## 番号: 1=バルコニー 2=廊下 3=原子炉 4=研究室 5=オフィス 6=管理室 7=温室 8=医務室 9=除染室 10=ロッカールーム 11=発射台");
                sb.AppendLine("# Setting type: String");
                sb.AppendLine("# Default value: " + Mira.DefaultPairs);
                sb.AppendLine("BlockedPairs = " + pairs);
                sb.AppendLine();
                string sealedText = string.Join(",", sealedVents.OrderBy(n => n));
                sb.AppendLine("## 入れないベントの番号。カンマで区切って書く (例: 10,1)。空にすると全部入れる。");
                sb.AppendLine("# Setting type: String");
                sb.AppendLine("# Default value: ");
                sb.AppendLine("SealedVents = " + sealedText);
                sb.AppendLine();
                string oneWayText = string.Join(",", oneWay.Values.Select(v => $"{v.From}>{v.To}"));
                sb.AppendLine("## 一方通行のベント。「行ける方向」を「番号>番号」で書いて、カンマで区切る (例: 3>11)。空にすると一方通行なし。");
                sb.AppendLine("# Setting type: String");
                sb.AppendLine("# Default value: ");
                sb.AppendLine("OneWay = " + oneWayText);
                sb.AppendLine();
                sb.AppendLine("[DoorLog]");
                sb.AppendLine();
                sb.AppendLine("## 強化ドアログを使うか (true = 使う / false = 使わない)");
                sb.AppendLine("# Setting type: Boolean");
                sb.AppendLine("# Default value: true");
                sb.AppendLine("Enabled = " + (doorLogEnabled ? "true" : "false"));
                sb.AppendLine();
                var inv2 = System.Globalization.CultureInfo.InvariantCulture;
                string sensorText = string.Join(";", sensors.Select(x => x.ToConfig()));
                sb.AppendLine("## センサーの場所。セミコロンで区切って書く。丸は「名前:x,y,半径」、四角は「名前:x,y,幅,高さ,角度」。空にすると本物のセンサー3か所を使う。");
                sb.AppendLine("# Setting type: String");
                sb.AppendLine("# Default value: ");
                sb.AppendLine("Sensors = " + sensorText);
                File.WriteAllText(Path.Combine(configDir, ConfigFile), sb.ToString(), new UTF8Encoding(false));

                MessageBox.Show(this,
                    "インストールしました！\n\n" +
                    "通れない組み合わせ: " + (pairs == "" ? "なし" : pairs) + "\n" +
                    "入れないベント: " + (sealedText == "" ? "なし" : sealedText) + "\n" +
                    "一方通行: " + (oneWayText == "" ? "なし" : oneWayText) + "\n" +
                    "強化ドアログ: " + (doorLogEnabled ? $"使う (センサー: {(sensors.Count == 0 ? "本物の3か所" : sensors.Count + "個")})" : "使わない") + "\n\n" +
                    "Among Us を起動すると反映されます。",
                    "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "インストールに失敗しました。\n\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===== アンインストール =====
        void Uninstall()
        {
            if (!CheckDir()) return;
            if (GameRunning())
            {
                MessageBox.Show(this, "Among Us が起動しています。\nAmong Us を閉じてから、もう一度押してください。", "アンインストールできません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show(this, "MiraIsGod のプラグインと設定ファイルを消します。よろしいですか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                string dll = Path.Combine(BepInExDir, "plugins", PluginFile);
                string cfg = Path.Combine(BepInExDir, "config", ConfigFile);
                if (File.Exists(dll)) File.Delete(dll);
                if (File.Exists(cfg)) File.Delete(cfg);
                // 前の名前 (MiraVentLock) のファイルも消す
                foreach (string old in new[] { Path.Combine(BepInExDir, "plugins", OldPluginFile), Path.Combine(BepInExDir, "config", OldConfigFile) })
                    if (File.Exists(old)) File.Delete(old);
                MessageBox.Show(this, "アンインストールしました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "アンインストールに失敗しました。\n\n" + ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ----------------------------------------------------------------
    //  自分で置くセンサー (座標はゲームの中の単位)
    // ----------------------------------------------------------------
    class Sensor
    {
        public string Name;
        public bool IsRect;          // false = 丸、true = 四角
        public PointF Pos;           // 中心
        public float W = 2f;         // 丸: 直径 / 四角: 幅
        public float H = 2f;         // 四角: 高さ
        public float Angle;          // 四角: 傾き (度。反時計回り)

        // 四角の4つの角 (ゲームの座標)
        public PointF[] Corners()
        {
            double rad = Angle * Math.PI / 180;
            float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);
            var local = new[] { new PointF(-W / 2, -H / 2), new PointF(W / 2, -H / 2), new PointF(W / 2, H / 2), new PointF(-W / 2, H / 2) };
            return local.Select(p => new PointF(Pos.X + p.X * cos - p.Y * sin, Pos.Y + p.X * sin + p.Y * cos)).ToArray();
        }

        // その点がセンサーの中か (margin = 少しはみ出してもOKな幅。クリックしやすくするため)
        public bool Contains(PointF p, float margin = 0f)
        {
            float dx = p.X - Pos.X, dy = p.Y - Pos.Y;
            if (!IsRect) return Math.Sqrt(dx * dx + dy * dy) <= Math.Max(W / 2, margin);
            double rad = -Angle * Math.PI / 180; // 傾きを戻して、ふつうの四角として調べる
            float lx = (float)(dx * Math.Cos(rad) - dy * Math.Sin(rad));
            float ly = (float)(dx * Math.Sin(rad) + dy * Math.Cos(rad));
            return Math.Abs(lx) <= Math.Max(W / 2, margin) && Math.Abs(ly) <= Math.Max(H / 2, margin);
        }

        // 設定ファイルに書く形: 丸「名前:x,y,半径」 / 四角「名前:x,y,幅,高さ,角度」
        public string ToConfig()
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string F(float v) => v.ToString("0.##", inv);
            return IsRect
                ? $"{Name}:{F(Pos.X)},{F(Pos.Y)},{F(W)},{F(H)},{F(Angle)}"
                : $"{Name}:{F(Pos.X)},{F(Pos.Y)},{F(W / 2)}";
        }

        // 設定ファイルの形から作る (数字が3つなら丸、5つなら四角)
        public static Sensor Parse(string name, string[] nums)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var v = new List<float>();
            foreach (string n in nums)
            {
                if (!float.TryParse(n, System.Globalization.NumberStyles.Float, inv, out float f)) return null;
                v.Add(f);
            }
            if (v.Count == 3) return new Sensor { Name = name, Pos = new PointF(v[0], v[1]), W = v[2] * 2 };
            if (v.Count == 5) return new Sensor { Name = name, IsRect = true, Pos = new PointF(v[0], v[1]), W = v[2], H = v[3], Angle = v[4] };
            return null;
        }
    }

    // ちらつかないように、二重に描く設定をしたパネル
    class MapPanel : Panel
    {
        public MapPanel() { DoubleBuffered = true; }
    }
}
