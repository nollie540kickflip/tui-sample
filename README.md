# TuiSample — C# Terminal.Gui ユーザ管理 TUI ショーケース

C# (.NET 10) と **Terminal.Gui** を用いて構築した、ターミナル上でマウスおよびキーボードショートカットの両方で操作できる本格的な TUI（Text-based User Interface）アプリケーションのサンプルです。

画面（UI）とビジネスロジックを **MVVM + DI パターン** で疎結合に分離し、さらに独自の宣言的レイアウトヘルパー（`TuiLayout`）によって **画面コードから `X` / `Y` の座標指定を完全になくした設計** になっています。

---

## 主な機能

1. **ユーザ一覧データグリッド (`TableView`) & リアルタイム絞り込み**
   - キーワード検索（ID・氏名・メールアドレス・部署・ロール・ステータス）および状態フィルタ（`All` / `Active` / `Inactive`）でリアルタイムに一覧を絞り込み。
   - **条件付き色分け**: `Admin` ロールを強調色でハイライトし、`Inactive` ユーザの行をグレーアウト表示。
   - **ソート切替 (`F6`)**: `ID ▲` → `ID ▼` → `氏名 ▲` → `部署 ▲` → `登録日 ▼` の順でソートを切替。
2. **左ペイン階層ツリー (`TreeView`) による 2 ペイン連動ナビゲーション**
   - `すべて` / `部署別` / `ロール別` の階層ツリーを選択すると、右ペインのグリッドが連動してフィルタリングされます。
3. **モーダルダイアログによる CRUD 操作 & 右クリックメニュー (`ContextMenu`)**
   - 新規追加 (`F2`)、編集 (`F3` / `Enter`)、削除 (`F4`) をモーダルダイアログで実行。
   - グリッド上で **マウス右クリック** するとコンテキストメニューが開き、その場で「ステータス切替 (`Active` ⇔ `Inactive`)」や「ユーザ複製」が可能。
4. **双方向 CSV ダウンロード & インポート (`SaveDialog` / `OpenDialog`)**
   - **CSV 出力 (`F5`)**: 「現在表示中のユーザのみ」または「全ユーザ」を選択し、Excel でも文字化けしない **UTF-8 (BOM付き)**・RFC 4180 準拠形式で出力。
   - **CSV 取込**: 出力した CSV ファイルを選択して一括インポート。
   - **CLI ヘッドレス出力**: `--export-csv <path>` 引数で TUI を開かずにバッチ出力も可能。
5. **統計ダッシュボード (`GraphView`) & リアルタイム操作ログ (`TextView`)**
   - タブ2「統計ダッシュボード」では、部署別・ロール別の集計表とターミナル棒グラフ (`GraphView`) を表示。
   - タブ3「操作ログ」では、アプリ内の全操作履歴をタイムスタンプ付きでリアルタイム表示。
6. **カラーテーマ切替 (`F7`) & 非同期プログレスバー一括生成 (`F8`)**
   - `F7` キーで 4 種類のテーマ（`Classic Blue` / `Dark Cyan` / `Matrix Green` / `Retro Amber`）を即時切替。
   - `F8` キーでアニメーションする `ProgressBar` とキャンセル機能付きの非同期ダミーユーザ一括生成（30件）を体験可能。

---

## 技術スタック

| カテゴリ | 使用技術・ライブラリ |
| :--- | :--- |
| ランタイム / 言語 | .NET 10 (`net10.0`) / C# |
| TUI フレームワーク | `Terminal.Gui` (`1.19.0`) |
| MVVM フレームワーク | `CommunityToolkit.Mvvm` (`8.4.0`) |
| DI コンテナ | `Microsoft.Extensions.DependencyInjection` (`9.0.0`) |
| 単体テスト | `xUnit` (`2.9.2`) |

---

## ディレクトリ構成

```text
tui-sample/
├── TuiSample.sln                           # ソリューションファイル
├── README.md                               # 本ドキュメント
├── SPEC.md                                 # 詳細システム仕様書
├── nuget.config                            # ローカルパッケージキャッシュ設定
├── src/
│   └── TuiSample/                          # アプリケーション本体
│       ├── TuiSample.csproj
│       ├── Program.cs                      # エントリポイント & DI コンテナ構成
│       ├── Models/                         # ドメインモデル & Enum 定義
│       ├── Services/                       # リポジトリ・CSV入出力・操作ロガー
│       ├── ViewModels/                     # UI非依存の ViewModel 層
│       └── UI/
│           ├── Layout/                     # 座標指定ゼロの宣言的レイアウト (TuiLayout)
│           ├── Themes/                     # カラーテーマ管理 (ThemeManager)
│           ├── Views/                      # メイン画面 & 統計ダッシュボード
│           └── Dialogs/                    # 各種モーダルダイアログ
└── tests/
    └── TuiSample.Tests/                    # xUnit 単体テストプロジェクト
        ├── TuiSample.Tests.csproj
        ├── UI/                             # レイアウトエンジンのテスト
        └── ViewModels/                     # ViewModel & Service の単体テスト
```

---

## 起動方法・コマンド一覧

### 1. TUI アプリケーションの起動
プロジェクトルートで以下のコマンドを実行します：

```bash
dotnet run --project src/TuiSample
```

> **macOS (Homebrew 版 .NET) でビルド済みバイナリを直接実行する場合の補足**  
> `./src/TuiSample/bin/Debug/net10.0/TuiSample` を直接実行する場合は、`~/.zshrc` に以下を設定しておくとランタイムが自動認識されます：
> ```bash
> export DOTNET_ROOT="/opt/homebrew/opt/dotnet/libexec"
> ```

### 2. ヘッドレス CSV エクスポート（CLI バッチモード）
TUI 画面を立ち上げずに初期データを CSV 出力する場合は、`--export-csv` オプションを使用できます：

```bash
dotnet run --project src/TuiSample -- --export-csv users_output.csv
```

### 3. ビルド & 単体テストの実行
UI（`Terminal.Gui`）を起動せずに、ViewModel・Service・レイアウト構築の単体テスト（全15件）を高速に実行できます：

```bash
dotnet build
dotnet test
```

---

## キーボードショートカット・操作一覧

| キー / マウス操作 | 動作 |
| :--- | :--- |
| **矢印キー (↑/↓/←/→)** / **左クリック** | ツリーやグリッドの項目選択・移動 |
| **右クリック（グリッド上）** | コンテキストメニュー（編集 / 状態切替 / 複製 / 削除）を表示 |
| **`F2`** | 新規ユーザ追加ダイアログを開く |
| **`F3`** / **`Enter`** | 選択中のユーザ編集ダイアログを開く |
| **`F4`** | 選択中のユーザを削除（確認ダイアログ付き） |
| **`F5`** | CSV ダウンロード（出力設定）ダイアログを開く |
| **`F6`** | ソート順を切り替え（`Id ▲` → `Id ▼` → `Name ▲` → `Department ▲` → `CreatedAt ▼`） |
| **`F7`** | カラーテーマを切り替え（`Classic Blue` → `Dark Cyan` → `Matrix Green` → `Retro Amber`） |
| **`F8`** | 非同期プログレスバーによるダミーユーザ一括生成ダイアログを開く |
| **`F9`** | 上部メニューバーにフォーカス |
| **`Ctrl + Q`** | アプリケーションを終了 |

