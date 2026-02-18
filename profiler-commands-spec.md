# UniCli Profiler コマンド追加仕様書

## 背景

UniCli の既存プロファイラーコマンド群（`Profiler.StartRecording` / `StopRecording` / `GetFrameData`）を使ったパフォーマンスチューニングにおいて、以下の課題がある。

1. **フレーム単位の逐次取得** — `Profiler.GetFrameData` は1フレームずつしか取得できず、記録範囲全体の傾向を把握するには N 回の呼び出し＋手動集計が必要
2. **問題フレームの発見困難** — 2000フレームの記録中、スパイクや GC が発生したフレームを特定するには全フレームの個別確認が必要

これを解消するため、以下の 2 コマンドを追加する。

---

## 1. `Profiler.AnalyzeFrames`

### 概要

記録済みフレーム範囲を走査し、フレーム時間・GC アロケーション・サンプル別コストの集計統計を返す。

### CLI 使用例

```bash
# 記録範囲全体を分析（デフォルト）
unicli exec Profiler.AnalyzeFrames --json

# フレーム範囲を指定
unicli exec Profiler.AnalyzeFrames --startFrame 44000 --endFrame 45000 --json

# 上位サンプル数を指定（デフォルト: 10）
unicli exec Profiler.AnalyzeFrames --topSampleCount 20 --json

# サンプル名でフィルタ（部分一致）
unicli exec Profiler.AnalyzeFrames --sampleNameFilter "BehaviourUpdate" --json
```

### Request

```csharp
[Serializable]
public class ProfilerAnalyzeFramesRequest
{
    /// <summary>分析開始フレーム。-1 の場合は記録範囲の先頭。</summary>
    public int startFrame = -1;

    /// <summary>分析終了フレーム。-1 の場合は記録範囲の末尾。</summary>
    public int endFrame = -1;

    /// <summary>レスポンスに含める上位サンプルの数。</summary>
    public int topSampleCount = 10;

    /// <summary>
    /// サンプル名のフィルタ文字列（部分一致、大文字小文字区別なし）。
    /// 指定した場合、topSamples にはこの文字列を含むサンプルのみが含まれる。
    /// </summary>
    public string sampleNameFilter = "";
}
```

### Response

```csharp
[Serializable]
public class ProfilerAnalyzeFramesResponse
{
    /// <summary>分析したフレーム数。</summary>
    public int analyzedFrameCount;

    /// <summary>分析したフレーム範囲の先頭。</summary>
    public int startFrame;

    /// <summary>分析したフレーム範囲の末尾。</summary>
    public int endFrame;

    /// <summary>フレーム時間の統計（ミリ秒）。</summary>
    public FrameTimeStats frameTime;

    /// <summary>GC アロケーションの統計。</summary>
    public GcAllocStats gcAlloc;

    /// <summary>selfTimeMs の合計が大きい順にソートされたサンプル統計。</summary>
    public SampleStats[] topSamples;
}

[Serializable]
public class FrameTimeStats
{
    public float avgMs;
    public float minMs;
    public float maxMs;
    public float medianMs;
    public float p95Ms;
    public float p99Ms;

    /// <summary>maxMs を記録したフレームインデックス。</summary>
    public int maxFrameIndex;
}

[Serializable]
public class GcAllocStats
{
    /// <summary>全フレーム合計の GC アロケーション（バイト）。</summary>
    public long totalBytes;

    /// <summary>1フレームあたりの平均 GC アロケーション（バイト）。</summary>
    public float avgBytesPerFrame;

    /// <summary>1フレームあたりの最大 GC アロケーション（バイト）。</summary>
    public long maxBytesInFrame;

    /// <summary>maxBytesInFrame を記録したフレームインデックス。</summary>
    public int maxFrameIndex;

    /// <summary>GC > 0 のフレーム数。</summary>
    public int framesWithGc;
}

[Serializable]
public class SampleStats
{
    /// <summary>サンプル名（MergeSamplesWithTheSameName 後の名前）。</summary>
    public string name;

    /// <summary>selfTimeMs の全フレーム平均。</summary>
    public float avgSelfMs;

    /// <summary>selfTimeMs の全フレーム中の最大値。</summary>
    public float maxSelfMs;

    /// <summary>totalTimeMs の全フレーム平均。</summary>
    public float avgTotalMs;

    /// <summary>1フレームあたりの平均呼び出し回数。</summary>
    public float avgCalls;

    /// <summary>1フレームあたりの平均 GC アロケーション（バイト）。</summary>
    public float avgGcAllocBytes;

    /// <summary>このサンプルが出現したフレーム数。</summary>
    public int presentInFrames;
}
```

### 実装方針

- `ProfilerDriver.firstFrameIndex` / `lastFrameIndex` から有効範囲を決定し、リクエストの `startFrame`/`endFrame` でクランプ
- 範囲内の各フレームで `ProfilerDriver.GetHierarchyFrameDataView` を開き、既存の `ProfilerGetFrameDataHandler.CollectSamplesRecursive` と同様の方法でサンプルを収集
- フレーム時間は `HierarchyFrameDataView.frameTimeMs` から取得
- GC はフレーム内全サンプルの `gcAllocBytes` を合計
- サンプル統計は `Dictionary<string, SampleAccumulator>` で名前をキーに集計し、最終的に `selfTimeMs` の合計降順でソート
- パーセンタイル計算は `frameTimeMs` の配列をソートしてインデックスアクセス
- `sampleNameFilter` が空でない場合、`topSamples` の出力をフィルタリングする（集計自体は全サンプルに対して行う。GC 統計等に影響させないため）
- `HierarchyFrameDataView` は `IDisposable` なので各フレームで `using` する

### テキスト出力（`TryWriteFormatted`）

```
Analyzed 2000 frames [44000..45999]

Frame Time:
  Avg: 1.13ms  Min: 0.89ms  Max: 3.21ms  Median: 1.10ms  P95: 1.82ms  P99: 2.51ms
  Worst frame: #44523

GC Allocation:
  Total: 0 B  Avg/frame: 0 B  Max/frame: 0 B (frame #-)
  Frames with GC: 0 / 2000

Top Samples (by avg self time):
Name                                               Avg Self   Max Self  Avg Total  Avg Calls  Avg GC     Frames
------------------------------------------------------------------------------------------------------------------
EditorLoop                                          0.598ms    1.200ms    0.598ms        3.0      0 B      2000
Inl_On Record Render Graph                          0.069ms    0.150ms    0.084ms        1.0      0 B      2000
BehaviourUpdate                                     0.006ms    0.012ms    0.008ms        1.0      0 B      2000
```

### 注意事項

- フレーム数が多い場合（数千フレーム）、処理時間が長くなる可能性がある。CLI 側のデフォルトタイムアウト（120秒）内に収まることを確認する。必要に応じて進捗ログを出力する
- `HierarchyFrameDataView` を毎フレーム開閉するため、メモリ圧力に注意。1フレーム処理ごとに `using` で確実に Dispose する

---

## 2. `Profiler.FindSpikes`

### 概要

記録済みフレーム範囲を走査し、フレーム時間または GC アロケーションが閾値を超えたフレームを検出して返す。

### CLI 使用例

```bash
# フレーム時間 16.6ms 超え（60fps 割れ）を検出
unicli exec Profiler.FindSpikes --frameTimeThresholdMs 16.6 --json

# GC アロケーション 1KB 超えを検出
unicli exec Profiler.FindSpikes --gcThresholdBytes 1024 --json

# 両方の条件（OR）で検出、上位 5 件
unicli exec Profiler.FindSpikes --frameTimeThresholdMs 16.6 --gcThresholdBytes 1024 --limit 5 --json

# フレーム範囲を指定
unicli exec Profiler.FindSpikes --startFrame 44000 --endFrame 45000 --frameTimeThresholdMs 8.3 --json
```

### Request

```csharp
[Serializable]
public class ProfilerFindSpikesRequest
{
    /// <summary>検索開始フレーム。-1 の場合は記録範囲の先頭。</summary>
    public int startFrame = -1;

    /// <summary>検索終了フレーム。-1 の場合は記録範囲の末尾。</summary>
    public int endFrame = -1;

    /// <summary>
    /// フレーム時間の閾値（ミリ秒）。0 以下の場合はフレーム時間による検出を行わない。
    /// この値を超えたフレームがスパイクとして報告される。
    /// </summary>
    public float frameTimeThresholdMs;

    /// <summary>
    /// GC アロケーションの閾値（バイト）。0 以下の場合は GC による検出を行わない。
    /// この値を超えたフレームがスパイクとして報告される。
    /// </summary>
    public long gcThresholdBytes;

    /// <summary>返すスパイクフレームの最大数。デフォルト 20。</summary>
    public int limit = 20;

    /// <summary>
    /// スパイクフレームごとに含めるサンプルの数。デフォルト 5。
    /// フレーム時間が大きい順（totalTimeMs 降順）で上位 N 件を含める。
    /// </summary>
    public int samplesPerFrame = 5;
}
```

### Response

```csharp
[Serializable]
public class ProfilerFindSpikesResponse
{
    /// <summary>走査したフレーム数。</summary>
    public int searchedFrameCount;

    /// <summary>走査したフレーム範囲の先頭。</summary>
    public int startFrame;

    /// <summary>走査したフレーム範囲の末尾。</summary>
    public int endFrame;

    /// <summary>検出されたスパイクの総数（limit で切られる前の値）。</summary>
    public int totalSpikeCount;

    /// <summary>検出されたスパイクフレーム（frameTimeMs 降順）。</summary>
    public SpikeFrame[] spikes;
}

[Serializable]
public class SpikeFrame
{
    /// <summary>フレームインデックス。</summary>
    public int frameIndex;

    /// <summary>フレーム時間（ミリ秒）。</summary>
    public float frameTimeMs;

    /// <summary>フレーム内の合計 GC アロケーション（バイト）。</summary>
    public long gcAllocBytes;

    /// <summary>フレーム内の合計サンプル数。</summary>
    public int totalSampleCount;

    /// <summary>
    /// スパイク判定の理由。"frameTime", "gcAlloc", "both" のいずれか。
    /// </summary>
    public string reason;

    /// <summary>
    /// フレーム内の上位サンプル（totalTimeMs 降順）。
    /// 既存の ProfilerSampleInfo を再利用。
    /// </summary>
    public ProfilerSampleInfo[] topSamples;
}
```

### 実装方針

- `startFrame` / `endFrame` の決定は `Profiler.AnalyzeFrames` と同様
- 各フレームで `HierarchyFrameDataView` を開き、`frameTimeMs` と GC 合計を取得
- 閾値判定:
  - `frameTimeThresholdMs > 0` かつ `frameTimeMs > frameTimeThresholdMs` → フレーム時間スパイク
  - `gcThresholdBytes > 0` かつ `totalGcInFrame > gcThresholdBytes` → GC スパイク
  - 両方の閾値が 0 以下の場合はエラーを返す（`"At least one threshold must be specified"`）
  - 両方の条件を満たす場合は `reason = "both"`
- スパイクフレームは `frameTimeMs` 降順でソートし、`limit` 件に制限
- 各スパイクフレームについて、`samplesPerFrame` 件の上位サンプルを含める（既存の `CollectTopSamples` 相当のロジック）
- GC 合計の計算は、フレーム内全サンプルの `gcAllocBytes` を合算（既存 `GetFrameData` と同様に `HierarchyFrameDataView.columnGcMemory` を使用）

### テキスト出力（`TryWriteFormatted`）

```
Searched 2000 frames [44000..45999]
Found 3 spikes (showing top 3)

#1  Frame 44523  22.30ms  GC: 4.0 KB  [both]
    GC.Alloc                         18.50ms self
    Loading.ReadObject                2.10ms self
    BehaviourUpdate                   0.01ms self

#2  Frame 44801  18.10ms  GC: 0 B  [frameTime]
    Shader.CreateGPUProgram          15.20ms self
    BehaviourUpdate                   0.01ms self

#3  Frame 45102   1.20ms  GC: 2.1 KB  [gcAlloc]
    MyScript.Update                   0.50ms self  GC: 2.1 KB
    BehaviourUpdate                   0.01ms self
```

### 注意事項

- 閾値が両方とも未指定（0以下）の場合はエラーとする。少なくとも1つの閾値が必要
- スパイクが大量にある場合を考慮し、`limit` のデフォルトは 20 とする
- `samplesPerFrame` が 0 の場合はサンプル情報を省略し、フレームレベルの情報のみ返す（高速化）

---

## CommandNames への追加

```csharp
public static class Profiler
{
    // ... 既存 ...
    public const string AnalyzeFrames = "Profiler.AnalyzeFrames";
    public const string FindSpikes = "Profiler.FindSpikes";
}
```

---

## 共通ユーティリティの検討

両コマンドとも以下の処理を共有する：

1. フレーム範囲の決定（`startFrame` / `endFrame` のバリデーションとクランプ）
2. フレームごとの `HierarchyFrameDataView` の取得と走査
3. サンプルの再帰的収集

既存の `ProfilerGetFrameDataHandler` にある `CollectSamplesRecursive` と同等のロジックが必要となるため、共通のヘルパーメソッドとして切り出すことを推奨する。

```csharp
// Editor/Handlers/Profiler/ProfilerFrameHelper.cs（案）
internal static class ProfilerFrameHelper
{
    /// <summary>有効なフレーム範囲を返す。startFrame/endFrame が -1 の場合は記録範囲の先頭/末尾を使用。</summary>
    public static (int start, int end) ResolveFrameRange(int startFrame, int endFrame);

    /// <summary>指定フレームの全サンプルを収集する。</summary>
    public static List<ProfilerSampleInfo> CollectAllSamples(HierarchyFrameDataView frameData);

    /// <summary>指定フレームの GC アロケーション合計を返す。</summary>
    public static long GetFrameGcAlloc(HierarchyFrameDataView frameData);
}
```

---

## CLI スキル定義への追加

`unicli commands` の出力に表示される説明文:

| Command | Description |
|---|---|
| `Profiler.AnalyzeFrames` | Analyze recorded frames and return aggregate statistics |
| `Profiler.FindSpikes` | Find frames exceeding frame time or GC allocation thresholds |
