/// <summary>
/// 閘門型障礙物（對應 ObstacleType.GateUp / GateDown）。
/// 繼承 TimedObstacle，障礙行為（隱藏→顯示倒數→開門→關門）完全由基類處理。
/// 此類別可在此覆寫動畫、音效等子類別專屬邏輯。
/// </summary>
public class GateObstacle : TimedObstacle { }
