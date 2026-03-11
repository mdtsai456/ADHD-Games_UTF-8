// IObstacle.cs
using UnityEngine;

/// <summary>
/// 所有障礙物的共用介面。
/// 障礙物具備三個依序執行的時間階段：
///   1. 隱藏倒數（hiddenSeconds）：不顯示 UI，但仍擋住通道（製造不確定感）
///   2. 顯示倒數（showSeconds）  ：顯示倒數 UI，仍擋住通道（玩家可預測開門時機）
///   3. 開放通行（openSeconds）  ：障礙物打開，IsPassable = true
/// 三個階段結束後循環重來。
/// </summary>
public interface IObstacle
{
    /// <summary>
    /// 當前是否可以通行（僅在 Open 狀態為 true）。
    /// </summary>
    bool IsPassable { get; }

    /// <summary>
    /// 套用外部傳入的時間配置（hiddenSeconds / showSeconds / openSeconds）。
    /// </summary>
    void Configure(ObstacleConfig cfg);

    /// <summary>
    /// 啟動障礙物的狀態機循環（由 TJunction.SetupObstacle 呼叫）。
    /// </summary>
    void StartCycle();

    /// <summary>
    /// 停止障礙物狀態機（路口被替換時呼叫，讓物件靜止等待下次使用）。
    /// </summary>
    void StopCycle();
}

//[System.Serializable]
//public struct ObstacleConfig
//{
//    public float cycleSeconds;   // 完整週期秒數（例：3秒一輪）
//    [Range(0f, 1f)]
//    public float passableRatio;  // 週期中可通行比例（例：0.5 = 一半時間能通行）
//    [Range(0f, 1f)]
//    public float phaseOffset;    // 相位偏移（讓不同路口不同步）
//}

/// <summary>
/// 傳遞給 IObstacle.Configure() 的時間參數結構。
/// 三個時間欄位依序對應障礙物的三個行為階段（參見 IObstacle 的說明）。
/// </summary>
[System.Serializable]
public struct ObstacleConfig
{
    /// <summary>隱藏倒數計時 UI、且擋住通道的秒數（玩家看不到剩餘時間）。</summary>
    public float hiddenSeconds;

    /// <summary>
    /// 顯示倒數計時 UI、且擋住通道的秒數。
    /// ⚠️ 注意命名差異：此欄位在 TimedObstacle 內部叫做 visibleSeconds。
    /// </summary>
    public float showSeconds;

    /// <summary>障礙物完全開啟、可自由通行的秒數（IsPassable = true）。</summary>
    public float openSeconds;

    /// <summary>相位偏移，讓不同路口的障礙不完全同步（目前保留供未來使用）。</summary>
    public float phaseOffset;
}
