# PipeServer リファクタリングタスク

## 概要

PipeServer 周辺のコード品質・パフォーマンス・テスタビリティを改善するためのタスク一覧。

## タスク

### 高優先度（独立して着手可能）

- [ ] **CommandResponse 構築ロジックをヘルパーに抽出**
  - `CommandDispatcher.DispatchAsync` 内で `new CommandResponse { ... }` が7箇所に散在
  - 成功/失敗 × JSON/Text × Unit/通常 の分岐を整理し、構築ヘルパーメソッドに抽出

- [ ] **ReadExact / マジックバイト検証を Protocol 層に共通化**
  - `PipeServer.cs` と `PipeClient.cs` で重複する ReadExactAsync パターン（クライアントは3箇所インライン展開）
  - マジックバイト検証ロジック（`PipeServer.cs:184-187` vs `PipeClient.cs:105-108`）
  - ハンドシェイクバッファ構築の重複も統一
  - **ブロック**: #3, #8, #9 の前提

- [ ] **レスポンス送信に ArrayPool を適用** （ReadExact 共通化の後）
  - `PipeServer.cs:140-142` でレスポンス送信時に string → byte[] → byte[4] の3回アロケーション
  - リクエスト受信側は ArrayPool を使っているのにレスポンス側は未使用
  - `PipeClient.cs` 側（146-148, 155, 173, 204）も同様

### 中優先度

- [ ] **commandCts の Dispose 漏れを修正**
  - `PipeServer.cs:121` で `new CancellationTokenSource()` が using も Dispose() もなし
  - `MonitorDisconnect` の `new byte[1]` も毎コマンドで生成

- [ ] **PipeServer / UniCliServer の二重ループを整理**
  - `UniCliServer.RunServerLoopAsync` が PipeServer を作成 → `WaitForShutdownAsync` → 例外で再作成のループ
  - `PipeServer.RunLoopAsync` も内部でループ — 二重のリトライ/エラーハンドリング
  - リトライ間隔が不統一（UniCliServer: 2秒、PipeServer: 1秒）
  - **ブロック**: Start() 分離の前提

- [ ] **コンストラクタ即起動を Start() メソッドに分離** （二重ループ整理の後）
  - UniCliServer と PipeServer がコンストラクタで即サーバーループ起動
  - テスト時にモックパイプを差し込めない

- [ ] **CommandInfo のキャッシュを追加**
  - `CommandDispatcher.GetAllCommandInfo()` が毎回 List + ToArray() を生成
  - `commands` コマンドは頻繁に呼ばれるがキャッシュなし

- [ ] **PipeClient の接続失敗時リソースリーク修正** （ReadExact 共通化の後）
  - `ConnectAsync` で `_pipeStream` を new した後に例外で catch に入った場合、Dispose されずリーク

### 低優先度

- [ ] **1MB マジックナンバーを定数化**
  - `PipeServer.cs:100` と `PipeClient.cs:195` で `1024 * 1024` がハードコード
  - `ProtocolConstants.MaxMessageSize` として定数化

- [ ] **ハンドシェイクバージョン検証の非対称性を修正** （ReadExact 共通化の後）
  - クライアントはバージョン不一致時にエラーで接続拒否
  - サーバーは LogWarning して接続を続行
  - 振る舞いを統一する
