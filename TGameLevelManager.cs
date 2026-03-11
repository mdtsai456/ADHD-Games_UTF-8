using static TGame;
using System.Collections.Generic;

/// <summary>
/// 關卡計畫產生器（Singleton）。
/// 根據 TGameLevelConfig 建立路口題目序列（JunctionPlan 列表），
/// 並決定每題的正解方向、提示配置與 mirror 設定。
/// </summary>
public class TGameLevelManager
{
    public static TGameLevelManager Instance { get; } = new TGameLevelManager();
    private System.Random r = new System.Random();

    /// <summary>
    /// 根據設定建立完整的關卡計畫。
    /// 回傳 answerSeq（每題正解方向 0/1）和 junctions（JunctionPlan 列表）。
    /// </summary>
    public (List<int> answerSeq, List<JunctionPlan> junctions) BuildPlan(
        TGameLevelConfig cfg, int goodCount, int badCount)
    {
        var answerSeq = new List<int>(cfg.numberOfJunctions);
        var junctions = new List<JunctionPlan>(cfg.numberOfJunctions);

        for (int i = 0; i < cfg.numberOfJunctions; i++)
        {
            // 1) 先決定正解方向（0 = 左 / 1 = 右）
            int dir = r.NextDouble() < 0.5 ? 0 : 1;

            // 2) 決定本題是否 mirror
            //    mirror = false（一般模式）：好提示放在正確方向，壞提示放在錯誤方向
            //    mirror = true （鏡像模式）：好提示故意放在「錯誤方向」，玩家必須靠辨識壞提示反推正解
            //    這是一種增加認知難度的反直覺設計：提示越「好看」反而代表「走那邊是錯的」
            bool mirror = r.NextDouble() < cfg.mirrorProbability;

            // goodOnLeft 決定好提示要放在哪一側
            bool goodOnLeft;
            if (!mirror)
                goodOnLeft = (dir == 0); // 正常：正解在左（dir=0）→ 好提示在左
            else
                goodOnLeft = (dir != 0); // 鏡像：正解在左（dir=0）→ 好提示故意放在右側

            // 3) 決定顯示模式（目前全局固定，可在此擴充為 per-junction 隨機混合）
            var mode = cfg.clueMode; // 例：加入 30% 機率只顯示壞提示（OnlyBad），增加挑戰性

            // 4) 從提示池選擇具體圖示（避免連續重複可在此加防重複邏輯）
            int gi = r.Next(goodCount);
            int bi = r.Next(badCount);

            answerSeq.Add(dir);
            junctions.Add(new JunctionPlan
            {
                correctDir  = dir,
                goodOnLeft  = goodOnLeft,
                goodIndex   = gi,
                badIndex    = bi,
                displayMode = mode
            });
        }
        return (answerSeq, junctions);
    }
}
