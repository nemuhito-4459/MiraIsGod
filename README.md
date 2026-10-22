# MiraIsGod

Among Us のミラHQを改造するプラグイン（自分用）。ホストだけが入れれば、MOD を入れていない人にも効く。

## できること
- **ベントの制限**: 通れない組み合わせ・一方通行・入れないベント（全員に「ベント掃除中」に見せて止める）
- **強化ドアログ**: ドアログの前に1秒いると、センサーの反応と「前回から何人死んだか」を、その人にだけ見えるチャットで送る
- **お知らせ**: 試合開始時のチャットと、ホストの画面の左下の表示

## ファイル
| ファイル | 中身 |
|---|---|
| `Plugin.cs` | プラグイン本体（説明はファイルの先頭に書いてある） |
| `MIRA_HQ-MOD.csproj` | プラグインのビルドの設定（できる DLL の名前は `MiraIsGod.dll`） |
| `nuget.config` | ビルドに使う部品の取り寄せ先 |
| `installer/Program.cs` | インストーラーの画面と、インストールの処理 |
| `installer/MapData.cs` | ミラHQの地図データ（ゲームから取り出した本物の座標） |
| `installer/MiraIsGodInstaller.csproj` | インストーラーのビルドの設定 |
| `.github/workflows/build.yml` | GitHub Actions でビルドする手順 |

## ビルドとダウンロード
1. プッシュすると、自動でビルドされる（Actions のページの「Run workflow」でも動かせる）
2. Actions の実行結果のページの下の「Artifacts」から、`MiraIsGodInstaller` をダウンロード
3. zip の中の `MiraIsGodInstaller.exe` を開いて、選んで「インストール」

## インストールされるもの
- `BepInEx/plugins/MiraIsGod.dll`
- `BepInEx/config/com.nemuhito4459.miraisgod.cfg`（選んだ設定。メモ帳でも直せる）

前の名前（MiraVentLock）のファイルが残っていたら、インストールのときに自動で消す。
