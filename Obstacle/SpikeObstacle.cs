/// <summary>
/// 尖刺型障礙物（對應 ObstacleType.Spike）。
/// 繼承 TimedObstacle，障礙行為（隱藏→顯示倒數→縮回→關閉）完全由基類處理。
/// 此類別可在此覆寫動畫、音效等子類別專屬邏輯。
/// </summary>
public class SpikeObstacle : TimedObstacle { }
