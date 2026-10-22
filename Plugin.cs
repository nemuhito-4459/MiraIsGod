// ================================================================
//  MiraIsGod
//  ミラHQのベント移動を制限するプラグイン
// ================================================================
//
//  【何をするもの？】
//   ミラHQで、決めた組み合わせのベント同士を移動できなくする。
//   行けない方向の矢印には「掃除中」のマークが出て、押しても移動しない。
//
//  【MOD を入れていない人にも効く理由】
//   Among Us には、もともと「ベント掃除中」というしくみがある。
//   掃除中のベントには、隣のベントから移動できない (Among Us 本体が止める)。
//   「どのベントが掃除中か」はホストが全員に送っている情報なので、
//   ホストが「その人にだけ、行けないベントを掃除中と送る」と、
//   MOD なしの人の画面でも、掃除中マークが出て移動できなくなる。
//
//  【人ごと・場所ごとに見せ方を変える理由】
//   ずっと掃除中にすると、そのベントにはどこからも行けなくなってしまう。
//   (例: 温室を掃除中にすると、オフィス → 温室 も行けなくなる)
//   なので「その人が今いるベントから行けないベントだけ」を、
//   「その人にだけ」掃除中に見せる。ベントから出たら元に戻す。
//
//  【ホスト自身は別のやり方】
//   ホストの画面は、ホストの中の「本当の情報」で動いているので、
//   掃除中の見せかけを送っても効かない。
//   なので、ホスト (このプラグインを入れている人) の画面だけは、
//   矢印に直接マークを付けて、押しても移動しないようにしている。
//
//  【ベントの番号 (ミラHQ)】 ※ログで調べた本物の番号
//    1 = バルコニー     2 = 廊下 (カフェテリア付近)   3 = 原子炉
//    4 = 研究室         5 = オフィス                 6 = 管理室
//    7 = 温室           8 = 医務室                   9 = 除染室
//   10 = ロッカールーム 11 = 発射台
//
//  【制限したあとに通れるところ】
//   発射台⇔原子炉 / 医務室⇔バルコニー / 廊下⇔管理室 /
//   温室⇔オフィス / 除染室⇔研究室
//   (ロッカールームのベントは、どこにも行けなくなる)
//
//  【プラグインを使っていることの知らせ方】
//   ・試合開始時 (役職紹介が終わったとき) に、ホストから全員のチャットへ知らせる
//     → MOD なしの人にも見える。ミラHQのときだけ。
//     メモ: 生きている人は、試合中はチャットを開けないので、
//           最初の会議でチャットを開いたときに読める (幽霊はすぐ読める)
//   ・ホストの画面の左下に「MiraIsGod」と常に出す
//     → ホストの画面だけ。ロビーでも試合中でも出る。
//
//  【一方通行】
//   設定ファイルの OneWay に「3>11」と書くと、原子炉(3) → 発射台(11) だけ行ける。
//   発射台のベントにいる人にだけ、原子炉を「掃除中」に見せるので、逆向きには行けない。
//   (もともと「今いるベントから行けないベント」を見せるしくみなので、向きを分けられる)
//
//  【入れないベント (封鎖)】
//   設定ファイルの SealedVents に書いたベントは、全員にずっと「掃除中」に見せる。
//   → 入ることも、隣のベントから移動してくることもできない。
//   ホストの画面は、そのベントの「使う」ボタンを押せないようにしている。
//
//  【設定ファイル】
//   通れなくする組み合わせ (BlockedPairs)・入れないベント (SealedVents)・一方通行 (OneWay) は BepInEx/config/com.nemuhito4459.miraisgod.cfg に書く。
//   インストーラー (MiraIsGodInstaller.exe) で選ぶと、ここに書かれる。
//
//  【歴史メモ】
//   最初は「禁止の移動が来たら、追い出して元のベントの前に戻す」方式だった。
//   掃除中に見せる方式にしたら、実際に遊んで問題なかったので、追い出しは消した。
//   もし「本人の画面とほかの人の画面で、いる場所がずれる」ことが起きたら、
//   追い出し (RpcBootFromVent) を戻すことを考える。
//
// ================================================================

using System.Collections.Generic;   // List や Dictionary (入れ物) を使うため
using BepInEx;                      // プラグインの名札 (BepInPlugin) を使うため
using BepInEx.Unity.IL2CPP;         // BasePlugin (プラグインのもと) を使うため
using HarmonyLib;                   // 「割り込み」(HarmonyPatch) を使うため
using Hazel;                        // 通信 (MessageWriter) を使うため
using UnityEngine;                  // Time (時間) を使うため

// ----------------------------------------------------------------
//  プラグインの本体
//  名札: ID・名前・バージョン。ID は変えないこと (ほかの MOD と区別するため)
// ----------------------------------------------------------------
[BepInPlugin("com.nemuhito4459.miraisgod", "MiraIsGod", "1.0.0")]
public class Plugin : BasePlugin
{
    // エラーをログ (BepInEx/LogOutput.log) に書くための入れ物
    public static BepInEx.Logging.ManualLogSource Logger;

    // ゲームの起動時に1回だけ呼ばれる
    // PatchAll() で、このファイルの中の [HarmonyPatch] が付いたものを全部登録する
    // メモ: 読み込めたかは、ログの「Loading [MiraIsGod 1.0.0]」で確かめられる
    //
    // 設定ファイル: BepInEx/config/com.nemuhito4459.miraisgod.cfg
    //  ・はじめて起動したときに、BepInEx が自動で作る (中身はおすすめの組み合わせ)
    //  ・インストーラーで選んだ組み合わせも、このファイルに書かれる
    //  ・メモ帳で開いて、BlockedPairs = の後ろを書き換えてもいい
    //  ・変えたら、Among Us を起動し直すと反映される
    public override void Load()
    {
        Logger = Log;

        // 設定ファイルから「通れなくする組み合わせ」を読む (なければおすすめの組み合わせで作る)
        string pairs = Config.Bind(
            "Vent",                 // 設定のグループの名前
            "BlockedPairs",         // 設定の名前
            VentLock.DefaultPairs,  // 最初の値
            "通れなくするベントの組み合わせ。「番号-番号」をカンマで区切って書く (例: 6-7,1-2)。空にすると制限なし。\n" +
            "番号: 1=バルコニー 2=廊下 3=原子炉 4=研究室 5=オフィス 6=管理室 7=温室 8=医務室 9=除染室 10=ロッカールーム 11=発射台"
        ).Value;
        VentLock.LoadPairs(pairs);

        // 設定ファイルから「入れないベント」を読む (最初は空 = 全部入れる)
        string sealedVents = Config.Bind(
            "Vent",
            "SealedVents",
            "",
            "入れないベントの番号。カンマで区切って書く (例: 10,1)。空にすると全部入れる。\n" +
            "入れないベントは、全員にずっと「掃除中」に見えて、入ることも、隣から移動してくることもできない。"
        ).Value;
        VentLock.LoadSealed(sealedVents);

        // 設定ファイルから「一方通行」を読む (最初は空 = 一方通行なし)
        string oneWay = Config.Bind(
            "Vent",
            "OneWay",
            "",
            "一方通行のベント。「行ける方向」を「番号>番号」で書いて、カンマで区切る (例: 3>11 = 原子炉→発射台だけ行ける)。空にすると一方通行なし。"
        ).Value;
        VentLock.LoadOneWay(oneWay);

        // 強化ドアログの設定
        DoorLogNotice.Enabled = Config.Bind("DoorLog", "Enabled", true, "強化ドアログを使うか (true = 使う / false = 使わない)").Value;
        DoorLogNotice.LoadSensors(Config.Bind(
            "DoorLog",
            "Sensors",
            "",
            "センサーの場所。セミコロンで区切って書く。丸は「名前:x,y,半径」、四角は「名前:x,y,幅,高さ,角度」(例: 北:16.5,12.0,1.0;南:7.0,-1.5,2,0.5,30)。空にすると本物のセンサー3か所を使う。"
        ).Value);

        new Harmony("com.nemuhito4459.miraisgod").PatchAll();
    }
}

// ----------------------------------------------------------------
//  ベント制限の中身 (ルールと、掃除中の見せかけを送る処理)
// ----------------------------------------------------------------
public static class VentLock
{
    // ===== 設定: 通れなくするベントの組み合わせ =====
    // (A, B) と書くと、A → B も B → A も通れなくなる (向きは関係なし)
    // 中身は、起動時に設定ファイルから読む (上の Plugin.Load を見てね)
    // メモ: 隣どうしでない組み合わせを書いても、何も起きない (もともと行けないので)
    static readonly List<(int, int)> BlockedPairs = new();

    // 設定ファイルがまだないときの、最初の組み合わせ (おすすめ)
    //  6-7 管理室⇔温室 / 6-5 管理室⇔オフィス / 5-4 オフィス⇔研究室 / 4-3 研究室⇔原子炉 /
    //  3-9 原子炉⇔除染室 / 9-10 除染室⇔ロッカールーム / 10-11 ロッカールーム⇔発射台 /
    //  10-8 ロッカールーム⇔医務室 / 1-2 バルコニー⇔廊下
    public const string DefaultPairs = "6-7,6-5,5-4,4-3,3-9,9-10,10-11,10-8,1-2";

    // 「6-7,1-2」のような文字を、組み合わせの一覧に直す
    // 書き間違えたところ (数字じゃない など) は飛ばして、ログに書く
    public static void LoadPairs(string text)
    {
        BlockedPairs.Clear();
        foreach (string part in text.Split(','))
        {
            string[] ab = part.Trim().Split('-');
            if (ab.Length == 2 && int.TryParse(ab[0], out int a) && int.TryParse(ab[1], out int b))
                BlockedPairs.Add((a, b));
            else if (part.Trim() != "")
                Plugin.Logger.LogWarning($"設定の書き方がおかしいので飛ばした: {part}");
        }
        Plugin.Logger.LogInfo($"通れなくする組み合わせ: {BlockedPairs.Count}個 ({text})");
    }

    // ===== 記録 =====
    // その人に最後に送った「掃除中に見せたベント」(例: "7,5")
    // 前と同じなら送らない (毎フレーム送ると通信が多すぎるため)
    // 何も送っていない人は、記録なし (= 空 "" と同じ扱い)
    static readonly Dictionary<byte, string> LastSent = new();

    // 次に「念のため送り直す」時刻 (1秒ごと)
    static float nextRefresh;

    // true になったら、次のフレームでベントの中にいる人全員に送り直す
    // (下の SerializePatch が true にする)
    public static bool ResendAll;

    // ===== 道具 =====

    // 今のマップがミラHQか (マップの番号: 0=スケルド 1=ミラHQ 2=ポーラス 4=エアシップ 5=ファングル)
    // GameOptionsManager.Instance が null (まだ準備できていない) のときは「違う」扱い
    public static bool IsMira() =>
        GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions.MapId == 1;

    // ===== 設定: 入れないベント (封鎖) =====
    // ここに入っているベントは、全員にずっと「掃除中」に見せる
    // → Among Us 本体が「掃除中のベントには入れない・移動してこられない」ようにしてくれる
    static readonly HashSet<int> Sealed = new();

    // 「10,1」のような文字を、入れないベントの一覧に直す
    public static void LoadSealed(string text)
    {
        Sealed.Clear();
        foreach (string part in text.Split(','))
        {
            if (int.TryParse(part.Trim(), out int id)) Sealed.Add(id);
            else if (part.Trim() != "") Plugin.Logger.LogWarning($"設定の書き方がおかしいので飛ばした: {part}");
        }
        Plugin.Logger.LogInfo($"入れないベント: {Sealed.Count}個 ({text})");
    }

    // ===== 設定: 一方通行 =====
    // (A, B) = 「A → B だけ行ける」(B → A は行けない)
    static readonly List<(int From, int To)> OneWay = new();

    // 「3>11,5>7」のような文字を、一方通行の一覧に直す
    public static void LoadOneWay(string text)
    {
        OneWay.Clear();
        foreach (string part in text.Split(','))
        {
            string[] ab = part.Trim().Split('>');
            if (ab.Length == 2 && int.TryParse(ab[0], out int a) && int.TryParse(ab[1], out int b))
                OneWay.Add((a, b));
            else if (part.Trim() != "")
                Plugin.Logger.LogWarning($"設定の書き方がおかしいので飛ばした: {part}");
        }
        Plugin.Logger.LogInfo($"一方通行: {OneWay.Count}個 ({text})");
    }

    // そのベントが入れない (封鎖) なら true
    public static bool IsSealed(int id) => Sealed.Contains(id);

    // ベント a から b へ移動できないなら true (向きあり: a が今いるベント、b が行き先)
    // ・どちらかが「入れないベント」なら、通れない
    // ・一方通行で「b → a だけ行ける」になっていたら、a → b は通れない
    // ・BlockedPairs を1つずつ見て、(a, b) か (b, a) があれば、通れない
    public static bool IsBlocked(int a, int b)
    {
        if (Sealed.Contains(a) || Sealed.Contains(b)) return true;
        foreach (var (from, to) in OneWay)
            if (from == b && to == a) return true; // 逆向きしか行けない
        foreach (var (x, y) in BlockedPairs)
            if ((x == a && y == b) || (x == b && y == a)) return true;
        return false;
    }

    // 試合が始まったときに、前の試合の記録を消す
    // (消さないと「前と同じだから送らない」と勘違いすることがある)
    public static void Reset()
    {
        LastSent.Clear();
        nextRefresh = 0f;
    }

    // ===== 毎フレーム呼ばれる (ホストだけ・ミラHQだけ) =====
    // その人が今いるベントから行けないベントを、その人にだけ「掃除中」と送る
    public static void Update()
    {
        // ベントのしくみ (VentilationSystem) を取り出す
        // 「誰がどのベントにいるか」「どのベントが掃除中か」をここで管理している
        // ロビーではマップ (ShipStatus) がまだないので、何もしない
        if (ShipStatus.Instance == null) return;
        if (!ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out ISystemType sys)) return;
        VentilationSystem vs = sys.TryCast<VentilationSystem>();
        if (vs == null) return;

        // 送り直すかどうか
        //  ・ほかの人がベントに出入りして、本当の情報が全員に送られたとき (ResendAll)
        //    → こちらの見せかけが上書きされているので、送り直す
        //  ・念のため1秒ごと (通信の取りこぼしや、ほかの MOD が上書きした場合のため)
        bool refresh = ResendAll || Time.time >= nextRefresh;
        if (Time.time >= nextRefresh) nextRefresh = Time.time + 1f;
        ResendAll = false;

        foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
        {
            // 飛ばす人:
            //  ・いない人 (null)
            //  ・ホスト自身 (AmOwner) … ホストは下の ArrowPatch / MovePatch で対応
            //  ・抜けた人 (Disconnected)
            if (pc == null || pc.AmOwner || pc.Data == null || pc.Data.Disconnected) continue;

            // この人に「掃除中」と見せるベントの一覧を作る
            // ① 入れないベント (封鎖) は、ベントの外にいる人にも、ずっと見せる
            //    (ただし、今そのベントの中にいる場合は除く)
            // ② ベントの中にいるなら、今いるベントから行けない隣のベントも見せる
            bool inside = vs.PlayersInsideVents.TryGetValue(pc.PlayerId, out byte inVent);  // 今いるベントの番号
            List<byte> fake = new();
            foreach (int id in Sealed)
                if (!(inside && id == inVent)) fake.Add((byte)id);
            if (inside)
                foreach (Vent v in ShipStatus.Instance.AllVents)
                    if (v.Id == inVent)                                         // 今いるベントを探して
                        foreach (Vent next in v.NearbyVents)                    // その隣のベントのうち
                            if (next != null && IsBlocked(inVent, next.Id)      // 行けないもので
                                && !fake.Contains((byte)next.Id))               // まだ入っていないものを
                                fake.Add((byte)next.Id);                        // 一覧に入れる

            // 前に送ったものと比べる
            //  ・違う → ベントに入った / 移動した / 出た ので送る
            //  ・同じ → 送らない。ただし「送り直す」タイミングで、見せかけがある人には送る
            //  メモ: 見せかけがない人には、送り直しはしない
            //        (本当の情報がそのまま届いていて、それで正しいため)
            //  メモ: 入れないベントがあるときは、全員に見せかけがあるので、全員に送り直す
            string now = string.Join(",", fake);
            LastSent.TryGetValue(pc.PlayerId, out string last);
            if (now == (last ?? "") && !(refresh && fake.Count > 0)) continue;

            Send(vs, pc, fake);
            LastSent[pc.PlayerId] = now;
        }
    }

    // ===== その人にだけ、ベントの情報を送る =====
    // 送る中身は、Among Us がもともと送っている形と同じ:
    //   ① 掃除中のベントの一覧 (掃除している人の番号, ベントの番号) × 人数分
    //   ② ベントの中にいる人の一覧 (その人の番号, ベントの番号) × 人数分
    // ①に、本当の掃除中 + 見せかけの掃除中 を入れて送る
    static void Send(VentilationSystem vs, PlayerControl target, List<byte> fake)
    {
        // 本当に掃除中のベントは、そのまま入れておく (消すと本当の掃除が見えなくなる)
        Dictionary<byte, byte> cleaning = new();
        foreach (var kv in vs.PlayersCleaningVents) cleaning[kv.Key] = kv.Value;

        // 見せかけの掃除中を足す
        // 「掃除している人の番号」が1つのベントに1つ必要なので、
        // 実際にはいない番号 (250, 249, ...) を借りる
        // (プレイヤーの番号はふつう 0〜14 くらいなので、ぶつからない)
        // 本人の番号や、本当に掃除中の人の番号は避ける
        byte id = 250;
        foreach (byte vent in fake)
        {
            while (cleaning.ContainsKey(id) || id == target.PlayerId) id--;
            cleaning[id] = vent;
        }

        // ここから通信の組み立て
        // メッセージは「箱の中に箱」の形 (StartMessage で箱を開けて、EndMessage で閉じる)
        MessageWriter w = MessageWriter.Get(SendOption.Reliable);   // Reliable = 確実に届ける
        w.StartMessage(6);                                         // 箱1: 6 = 特定の1人にだけ送る
        w.Write(AmongUsClient.Instance.GameId);                    //   部屋の番号
        w.WritePacked(target.OwnerId);                             //   送る相手
        w.StartMessage(1);                                         //   箱2: 1 = データの更新
        w.WritePacked(ShipStatus.Instance.NetId);                  //     マップ (ShipStatus) の番号
        w.StartMessage((byte)SystemTypes.Ventilation);             //     箱3: ベントのしくみ
        w.WritePacked(cleaning.Count);                             //       ① 掃除中の数
        foreach (var kv in cleaning) { w.Write(kv.Key); w.Write(kv.Value); }               //       ① 掃除中 (人, ベント)
        w.WritePacked(vs.PlayersInsideVents.Count);                //       ② ベントの中にいる人の数
        foreach (var kv in vs.PlayersInsideVents) { w.Write(kv.Key); w.Write(kv.Value); }  //       ② 中にいる人 (人, ベント)
        w.EndMessage();                                            //     箱3 を閉じる
        w.EndMessage();                                            //   箱2 を閉じる
        w.EndMessage();                                            // 箱1 を閉じる
        AmongUsClient.Instance.SendOrDisconnect(w);                // 送る
        w.Recycle();                                               // 入れ物を片づける
    }
}

// ================================================================
//  割り込み (HarmonyPatch)
//  Among Us の決まった処理の「前」(Prefix) や「後」(Postfix) に、自分の処理を差し込む
//  メモ: Postfix は、もとの処理のあとに動く (もとの処理は止めない)
//        Prefix で false を返すと、もとの処理をしない
// ================================================================

// 試合 (マップ) が始まったら、記録をリセット
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class ResetPatch
{
    public static void Postfix() => VentLock.Reset();
}

// 本当のベントの情報が全員に送られたら、「送り直して」の合図を出す
// (ほかの人がベントに出入りするたびに、ホストは本当の情報を全員に送る。
//  そのとき、こちらの見せかけが上書きされてしまうため)
[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Serialize))]
public static class SerializePatch
{
    public static void Postfix() => VentLock.ResendAll = true;
}

// 毎フレーム、VentLock.Update を呼ぶ
// PlayerControl.FixedUpdate は「プレイヤーごと」に呼ばれるので、
// 自分 (LocalPlayer) のときだけにして、1フレームに1回にしている
// 条件: ホストだけ (判断するのはホスト) ・ ミラHQだけ
// エラーが起きても、ゲームが止まらないように try で囲んで、ログに書く
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class UpdatePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance != PlayerControl.LocalPlayer || !AmongUsClient.Instance.AmHost || !VentLock.IsMira()) return;
        try { VentLock.Update(); }
        catch (System.Exception e) { Plugin.Logger.LogError(e); }
    }
}

// 自分の画面 (ホスト): 矢印を更新したあとに、行けない矢印へ掃除中マークを付ける
// NearbyVents[i] (隣のベント) と CleaningIndicators[i] (その矢印の掃除中マーク) は、
// 同じ番号 i どうしが対応している
// メモ: ホストでなくても、このプラグインを入れている人の画面なら効く
[HarmonyPatch(typeof(Vent), nameof(Vent.UpdateArrows))]
public static class ArrowPatch
{
    public static void Postfix(Vent __instance)
    {
        if (!VentLock.IsMira()) return;
        var nearby = __instance.NearbyVents;       // 隣のベント
        var marks = __instance.CleaningIndicators; // 矢印の掃除中マーク
        for (int i = 0; i < nearby.Length && i < marks.Length; i++)
            if (nearby[i] != null && marks[i] != null && VentLock.IsBlocked(__instance.Id, nearby[i].Id))
                marks[i].SetActive(true);          // マークを表示
    }
}

// 自分の画面 (ホスト): 行けない矢印を押しても移動しない
// TryMoveToVent は「矢印を押したとき」に呼ばれる処理
// 行けない組み合わせなら、「移動できなかった」(false) を返して、もとの処理 (移動) をしない
[HarmonyPatch(typeof(Vent), nameof(Vent.TryMoveToVent))]
public static class MovePatch
{
    public static bool Prefix(Vent __instance, Vent otherVent, ref bool __result)
    {
        if (!VentLock.IsMira() || otherVent == null || !VentLock.IsBlocked(__instance.Id, otherVent.Id)) return true;
        __result = false; // 移動できなかった
        return false;     // もとの処理 (移動) をしない
    }
}

// 自分の画面 (ホスト): 入れないベントの「使う」ボタンを押せなくする
// Vent.CanUse = 「このベントを使えるか」を調べる処理。結果を「使えない」に書き換える
// (ほかの人は、上の「掃除中」の見せかけで入れなくなっている)
[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
public static class SealedUsePatch
{
    public static void Postfix(Vent __instance, ref float __result, ref bool canUse, ref bool couldUse)
    {
        if (!VentLock.IsMira() || !VentLock.IsSealed(__instance.Id)) return;
        canUse = false;            // 使えない
        couldUse = false;          // ボタンも光らせない
        __result = float.MaxValue; // 距離は「とても遠い」扱い
    }
}

// ================================================================
//  プラグインを使っていることを知らせる
// ================================================================

// 試合開始時 (役職紹介が終わったとき) に、全員のチャットへ知らせる
// 条件: ホストだけ (ホストが送る) ・ ミラHQだけ
// RpcSendChat = ホストがふつうにチャットを書くのと同じ。送り主はホストの名前になる
// メモ: 文の最初を「/」にしないこと (コマンド扱いになって送られないことがある)
// メモ: 長すぎるチャットは嫌がられることがあるので、100文字以内くらいにしておく
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
public static class NoticePatch
{
    // ===== 設定: 試合開始時に流す文 =====
    const string Notice = "【MiraIsGod】このミラHQでは、一部のベントがつながっていません。行けない方向の矢印には掃除中マークが出ます。";

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost || !VentLock.IsMira()) return;
        PlayerControl.LocalPlayer.RpcSendChat(Notice);
    }
}

// ホストの画面の左下に「MiraIsGod」と常に表示する
// (左上は MOD の設定一覧やタスク一覧と重なるので、左下にした)
// HudManager = 画面の上に重なっている表示 (ボタンやタスク一覧など) をまとめているもの
// ロビーでも試合中でも、毎フレーム Update が呼ばれるので、
// 最初の1回だけ文字を作って、あとは表示し続ける
// (部屋を抜けると HudManager ごと消えるので、次に入ったときにまた作られる)
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class LabelPatch
{
    // ===== 設定 =====
    const string Text = "MiraIsGod";                  // 表示する文字
    static readonly Vector3 Position = new(0.3f, 0.3f, -10f); // 左下の角からの距離 (右へ, 上へ, 手前へ)
    const float FontSize = 1.8f;                          // 文字の大きさ

    static TMPro.TextMeshPro label; // 作った文字 (まだ作っていなければ null)

    public static void Postfix(HudManager __instance)
    {
        if (label != null) return; // もう作ってある

        // 新しい入れ物を作って、HudManager の子にする (画面に一緒にくっついて動く)
        GameObject go = new("MiraIsGodLabel");
        go.transform.SetParent(__instance.transform, false);
        go.layer = 5; // 5 = UI の層 (画面の表示として描かれる)

        // 文字を付ける
        label = go.AddComponent<TMPro.TextMeshPro>();
        PingTracker ping = Object.FindObjectOfType<PingTracker>();
        if (ping != null) label.font = ping.text.font; // Among Us の文字の形を借りる (ないと文字が出ないことがある)
        label.text = Text;
        label.fontSize = FontSize;
        label.alignment = TMPro.TextAlignmentOptions.BottomLeft; // 左下そろえ
        label.rectTransform.pivot = new Vector2(0f, 0f);      // 文字の左下を基準にする
        label.enableWordWrapping = false;                     // 折り返さない

        // 画面の左下の角にくっつける (画面の大きさが変わっても、角からの距離を保つ)
        AspectPosition ap = go.AddComponent<AspectPosition>();
        ap.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        ap.DistanceFromEdge = Position;
        ap.AdjustPosition();
    }
}

// ================================================================
//  強化ドアログ (ミラHQ・ホストだけ)
// ================================================================
//
//  【しくみ】
//   ・本物のドアログは、MOD の設定で使えないようにしておく前提。
//   ・ドアログの机の「使う」ボタンが光る範囲に入って 1 秒たったら、
//     その人にだけ見えるチャットで「記録開始 ─ 死亡なし」を送る (ここから数え始める)
//   ・範囲にいる間に、誰かがセンサー (北・南東・南西) を通ったら、
//     「〇〇のセンサーに反応」と「前回の通知から 全員生存 / n名死亡」を送る
//   ・送ったあと 3 秒は送らない。その間の反応は、3 秒たったらまとめて送る (最大 5 件)
//   ・範囲から出たら、数え直し。また入ったら、1 秒後に「記録開始」から
//
//  【メモ】
//   ・生きている人が試合中にチャットを読むには、試合中のチャットを開ける MOD の設定が必要
//   ・「使う」ボタンが光るかは、ホストが Among Us と同じ距離の計算をして判定している
//   ・センサーの場所は、設定ファイルの [DoorLog] Sensors で決める (インストーラーで置ける)
//     空のときは、ゲームの中の本物のセンサー (SecurityLogger) 3か所を使う
//   ・オン・オフは、設定ファイルの [DoorLog] Enabled
//   ・死亡の数は「死んでいる人の数」の差で数える (会議になると範囲から出るので、
//     数えるのは自然と、まだ通報されていない死体になる)
//
public static class DoorLogNotice
{
    // ===== 設定 =====
    const float FirstDelay = 1f;   // 範囲に入ってから最初に送るまで (秒)
    const float Cooldown = 3f;     // 送ったあと、次に送れるまで (秒)
    const int MaxEvents = 5;       // まとめて送る反応の数の上限

    // センサーの名前 (本物のセンサーの場所 → 表示する名前)。あとで変えるならここ
    static string SensorName(SecurityLogBehaviour.SecurityLogLocations loc) => loc switch
    {
        SecurityLogBehaviour.SecurityLogLocations.North => "北",
        SecurityLogBehaviour.SecurityLogLocations.Southeast => "南東",
        SecurityLogBehaviour.SecurityLogLocations.Southwest => "南西",
        _ => loc.ToString(),
    };

    // ===== 設定ファイルから読む値 =====
    public static bool Enabled = true;

    // 自分で置いたセンサー。空なら本物のセンサーを使う
    //  丸: W = 直径 / 四角: W = 幅, H = 高さ, Angle = 傾き (度。反時計回り)
    class CustomSensor
    {
        public string Name; public bool IsRect; public Vector2 Pos; public float W, H, Angle;

        // その位置がセンサーの中か
        public bool Contains(Vector2 p)
        {
            Vector2 d = p - Pos;
            if (!IsRect) return d.magnitude <= W / 2;
            float rad = -Angle * Mathf.Deg2Rad; // 傾きを戻して、ふつうの四角として調べる
            float lx = d.x * Mathf.Cos(rad) - d.y * Mathf.Sin(rad);
            float ly = d.x * Mathf.Sin(rad) + d.y * Mathf.Cos(rad);
            return Mathf.Abs(lx) <= W / 2 && Mathf.Abs(ly) <= H / 2;
        }
    }
    static readonly List<CustomSensor> Custom = new();

    // 「北:16.5,12.0,1.0;南:7.0,-1.5,2,0.5,30」のような文字を、センサーの一覧に直す
    //  数字が3つ (x,y,半径) なら丸、5つ (x,y,幅,高さ,角度) なら四角
    public static void LoadSensors(string text)
    {
        Custom.Clear();
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (string part in text.Split(';'))
        {
            string t = part.Trim();
            if (t == "") continue;
            int colon = t.LastIndexOf(':');
            string[] nums = colon >= 0 ? t.Substring(colon + 1).Split(',') : new string[0];
            var v = new List<float>();
            foreach (string n in nums)
                if (float.TryParse(n, System.Globalization.NumberStyles.Float, inv, out float f)) v.Add(f);
            string name = colon > 0 ? t.Substring(0, colon) : "";
            if (name != "" && v.Count == nums.Length && v.Count == 3)
                Custom.Add(new CustomSensor { Name = name, Pos = new Vector2(v[0], v[1]), W = v[2] * 2 });
            else if (name != "" && v.Count == nums.Length && v.Count == 5)
                Custom.Add(new CustomSensor { Name = name, IsRect = true, Pos = new Vector2(v[0], v[1]), W = v[2], H = v[3], Angle = v[4] });
            else
                Plugin.Logger.LogWarning($"センサーの書き方がおかしいので飛ばした: {t}");
        }
        Plugin.Logger.LogInfo(Custom.Count == 0 ? "強化ドアログのセンサー: 本物の3か所" : $"強化ドアログのセンサー: {Custom.Count}個");
    }

    // ===== その人ごとの状態 =====
    class Viewer
    {
        public float EnterTime;          // 範囲に入った時刻
        public bool Started;             // 「記録開始」を送ったか
        public float LastSend;           // 最後に送った時刻
        public int BaseDead;             // 前回送ったときの「死んでいる人の数」
        public List<string> Pending = new(); // まだ送っていない反応
    }
    static readonly Dictionary<byte, Viewer> Viewers = new();

    // センサーの上に今いる人 (センサーの番号ごと)。「入った瞬間」を見分けるため
    static readonly Dictionary<int, HashSet<byte>> OnSensor = new();

    // ドアログの机
    static SystemConsole console;
    static SecurityLogger[] sensors;

    public static void Reset()
    {
        Viewers.Clear();
        OnSensor.Clear();
        console = null;
        sensors = null;
    }

    // 机とセンサーを探す (最初の1回だけ)
    // 本物のセンサーの場所は、ログに書いておく (インストーラーの地図に出すため)
    static bool Find()
    {
        if (console == null)
            foreach (SystemConsole c in Object.FindObjectsOfType<SystemConsole>())
                if (c.name.Contains("SurvLog")) { console = c; break; }
        if (sensors == null || sensors.Length == 0)
        {
            sensors = Object.FindObjectsOfType<SecurityLogger>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (SecurityLogger s in sensors)
                if (s != null && s.Sensor != null)
                {
                    Bounds b = s.Sensor.bounds;
                    Plugin.Logger.LogInfo($"本物のセンサー: {s.MyLocation} 中心 {b.center.x.ToString("F2", inv)},{b.center.y.ToString("F2", inv)} 大きさ {b.size.x.ToString("F2", inv)},{b.size.y.ToString("F2", inv)}");
                }
            if (console != null)
                Plugin.Logger.LogInfo($"ドアログの机: {console.transform.position.x.ToString("F2", inv)},{console.transform.position.y.ToString("F2", inv)} 距離 {console.usableDistance.ToString("F2", inv)}");
        }
        return console != null && sensors != null && sensors.Length > 0;
    }

    // 今使うセンサーの一覧 (名前と「その位置がセンサーの中か」を調べる道具)
    static List<(string Name, System.Func<Vector2, bool> Contains)> ActiveSensors()
    {
        var list = new List<(string, System.Func<Vector2, bool>)>();
        if (Custom.Count > 0)
            foreach (CustomSensor c in Custom)
            {
                CustomSensor sensor = c;
                list.Add((sensor.Name, p => sensor.Contains(p))); // 丸 (四角) の中にいるか
            }
        else
            foreach (SecurityLogger s in sensors)
                if (s != null && s.Sensor != null)
                {
                    SecurityLogger sensor = s;
                    list.Add((SensorName(sensor.MyLocation), p => sensor.Sensor.OverlapPoint(p))); // 本物の四角の中にいるか
                }
        return list;
    }

    static int DeadCount()
    {
        int n = 0;
        foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            if (pc != null && pc.Data != null && !pc.Data.Disconnected && pc.Data.IsDead) n++;
        return n;
    }

    // ===== 毎フレーム (ホストだけ・ミラHQだけ・会議中でないとき) =====
    public static void Update()
    {
        if (MeetingHud.Instance != null || ExileController.Instance != null)
        {
            Viewers.Clear(); // 会議になったら、全員数え直し
            return;
        }
        if (!Find()) return;

        float now = Time.time;
        int dead = DeadCount();
        Vector2 consolePos = console.transform.position;

        // 1) センサーに「入った」人を探す → 範囲にいる人全員に、反応を積む
        var active = ActiveSensors();
        for (int i = 0; i < active.Count; i++)
        {
            var (name, contains) = active[i];
            if (!OnSensor.TryGetValue(i, out var on)) OnSensor[i] = on = new HashSet<byte>();
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected || pc.inVent) { on.Remove(pc?.PlayerId ?? 255); continue; }
                bool inside = contains(pc.GetTruePosition());
                if (inside && on.Add(pc.PlayerId))               // 今入った
                    foreach (Viewer v in Viewers.Values)
                        if (v.Started && v.Pending.Count < MaxEvents) v.Pending.Add(name);
                if (!inside) on.Remove(pc.PlayerId);
            }
        }

        // 2) ドアログの範囲にいる人を調べて、送る
        foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;
            bool inRange = !pc.Data.IsDead && !pc.inVent
                && Vector2.Distance(pc.GetTruePosition(), consolePos) <= console.usableDistance;

            if (!inRange) { Viewers.Remove(pc.PlayerId); continue; } // 出たら数え直し

            if (!Viewers.TryGetValue(pc.PlayerId, out Viewer v))
                Viewers[pc.PlayerId] = v = new Viewer { EnterTime = now };

            if (!v.Started)
            {
                if (now - v.EnterTime < FirstDelay) continue;
                v.Started = true;
                v.LastSend = now;
                v.BaseDead = dead;
                Send(pc, "【ドアログ】記録開始 ─ 死亡なし");
                continue;
            }

            if (v.Pending.Count == 0 || now - v.LastSend < Cooldown) continue;

            int diff = dead - v.BaseDead;
            var sb = new System.Text.StringBuilder();
            foreach (string name in v.Pending) sb.AppendLine($"【ドアログ】{name}のセンサーに反応");
            sb.Append(diff > 0 ? $"─ 前回から {diff}名死亡" : "─ 全員生存");
            Send(pc, sb.ToString());
            v.Pending.Clear();
            v.LastSend = now;
            v.BaseDead = dead;
        }
    }

    // その人にだけ見えるチャットを送る
    // 送り主を「その人自身」にしている (送り主が幽霊だと、生きている人には見えないため)
    static void Send(PlayerControl target, string text)
    {
        if (target.AmOwner)
        {
            // ホスト自身には、自分の画面のチャットに直接足す
            if (HudManager.Instance != null) HudManager.Instance.Chat.AddChat(target, text);
            return;
        }
        MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, target.OwnerId);
        w.Write(text);
        AmongUsClient.Instance.FinishRpcImmediately(w);
    }
}

// 試合が始まったらリセット
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class DoorLogResetPatch
{
    public static void Postfix() => DoorLogNotice.Reset();
}

// 毎フレーム (ホストだけ・ミラHQだけ)
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class DoorLogUpdatePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance != PlayerControl.LocalPlayer || !AmongUsClient.Instance.AmHost || !VentLock.IsMira() || !DoorLogNotice.Enabled) return;
        if (ShipStatus.Instance == null) return; // ロビーではマップがまだないので、何もしない
        try { DoorLogNotice.Update(); }
        catch (System.Exception e) { Plugin.Logger.LogError($"強化ドアログの処理に失敗: {e}"); }
    }
}


// タイトル画面の右上 (hamo のバージョンの下) に名前とバージョンを出す
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class VersionLabelPatch
{
    // ===== 設定 =====
    public const string DisplayVersion = "v1.0a";          // 好きに決めてOK
    static readonly Vector3 Position = new(0.3f, 0.9f, -10f); // 右上の角からの距離 (左へ, 下へ, 手前へ)
    const float FontSize = 2f;

    public static void Postfix(MainMenuManager __instance)
    {
        GameObject go = new("MiraIsGodVersion");
        go.transform.SetParent(__instance.transform, false);
        go.layer = 5;

        var label = go.AddComponent<TMPro.TextMeshPro>();
        var vs = Object.FindObjectOfType<VersionShower>();
        if (vs != null) label.font = vs.text.font;
        label.text = $"Mira Is God {DisplayVersion}";
        label.fontSize = FontSize;
        label.alignment = TMPro.TextAlignmentOptions.TopRight;
        label.rectTransform.pivot = new Vector2(1f, 1f);
        label.enableWordWrapping = false;

        var ap = go.AddComponent<AspectPosition>();
        ap.Alignment = AspectPosition.EdgeAlignments.RightTop;
        ap.DistanceFromEdge = Position;
        ap.AdjustPosition();
    }
}
