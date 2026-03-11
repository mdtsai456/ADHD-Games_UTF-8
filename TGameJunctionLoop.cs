using UnityEngine;
using System.Collections.Generic;
using static TGame;

/// <summary>
/// 管理 A/B 雙路口的交替輪換機制，以及連勝 Tier 難度系統。
///
/// 核心概念：
///   - active   = 玩家當前所在的路口（已建置好提示與障礙）
///   - standby  = 預先建置好的下一個路口（玩家移動中在背景生成）
///   兩者在 CommitMove() 時互換身份，實現無縫路口切換。
///
/// 作答流程：
///   玩家按鍵 → PrepareMove()（預備 standby，不提交）
///           → 玩家走完 MoveRoute()
///           → CommitMove()（正式提交，交換 active/standby，推進題目）
///   撞牆時 → CancelPending()（隱藏 standby，idx 不推進，玩家重答同一題）
/// </summary>
public class TGameJunctionLoop : MonoBehaviour
{
    public GameObject firstAisle;

    [Header("Two Chunks")]
    public TJunction A;
    public TJunction B;

    [Header("Plan from TGame")]
    public TGame tgame;                      // 指到場上的 TGame
    public List<JunctionPlan> plans = new(); // 由 TGame.BuildPlans() 填入的題目列表

    // active：玩家當前所在路口；standby：預先建置好的下一個路口
    private TJunction active;
    private TJunction standby;

    // 當前題目在 plans 列表中的索引；答對推進，答錯（撞牆）維持不動
    private int idx = 0;

    // 由 PrepareMove() 寫入、CommitMove() 消費；記錄玩家「預備中」的選擇是否正確
    // 在兩次呼叫之間不應被任何其他邏輯修改
    private bool pendingIsCorrect;

    // 連續答對次數，驅動難度 Tier 上升；答錯時降至當前 Tier 的最低門檻（不歸零）
    private int consecutiveCorrect = 0;
    // 當前難度層級索引（0–4），映射到 TJunction 中提示子物件的索引（第幾個圖示）
    private int currentTierIndex = 0;

    // 連勝門檻陣列：索引對應 Tier，值代表進入該 Tier 所需的最低連勝數
    // 例：連勝 0 次 = Tier 0，連勝 3 次 = Tier 1，連勝 5 次 = Tier 2，依此類推
    private static readonly int[] TierThresholds = { 0, 3, 5, 8, 10 };

    private TJunction previousJunction;

    public int CurrentIndex => idx;
    public TJunction CurrentActive => active;

    void Start()
    {
        active = A; standby = B;
        A.gameObject.SetActive(true);
        B.gameObject.SetActive(false);
        tgame.BuildTestPlans();
        // 由 TGame 取得規劃（也可在別處時機呼叫 Init）
        if (tgame != null && (plans == null || plans.Count == 0))
            plans = tgame.Plans;

        SetupChunk(active, plans[idx], currentTierIndex);

        var detector = FindObjectOfType<ObstacleHitDetector>();
        if (detector != null)
        {
            detector.returnPoint = firstAisle != null
                                   ? firstAisle.transform
                                   : active.startPoint != null ? active.startPoint : active.entry;

            Debug.Log("[Loop] 首次 returnPoint 設為 " + detector.returnPoint.name);
        }

        Debug.Log("SetupChunk Success");
    }

    /// <summary>
    /// 保留空方法供 BranchCommitTrigger（EntryOnly）呼叫，避免編譯錯誤。
    /// 路口切換邏輯已全移至 PrepareMove + CommitMove。
    /// </summary>
    public void OnEnteredEntry(TJunction entered) { }

    // 設置路口：指定提示難度（tierIndex）並啟動障礙
    private void SetupChunk(TJunction j, JunctionPlan p, int tierIndex)
    {
        p.goodIndex = tierIndex;
        p.badIndex = tierIndex;
        j.SetupHintsFromChildren(p);
        j.SetupObstacle(p);
    }

    public TJunction GetPreviousJunction()
    {
        return previousJunction != null ? previousJunction : active;
    }

    /// <summary>
    /// 給定連勝數，回傳對應的 Tier 索引。
    /// 從 TierThresholds 從高往低搜尋，找到第一個「門檻 ≤ streak」的位置即為對應 Tier。
    /// </summary>
    private int GetTierForStreak(int streak)
    {
        for (int i = TierThresholds.Length - 1; i >= 0; i--)
            if (streak >= TierThresholds[i]) return i;
        return 0;
    }

    /// <summary>
    /// 給定 Tier 索引，回傳進入該 Tier 所需的最低連勝數（即 TierThresholds[tier]）。
    /// 答錯降 Tier 後，consecutiveCorrect 會被設為此值，避免從零重算。
    /// </summary>
    private int GetTierMinStreak(int tier)
    {
        return TierThresholds[Mathf.Clamp(tier, 0, TierThresholds.Length - 1)];
    }

    public void ResetLoop()
    {
        Debug.Log("[Loop] ResetLoop()");

        idx = 0;
        pendingIsCorrect = false;
        consecutiveCorrect = 0;
        currentTierIndex = 0;

        A.gameObject.SetActive(true);
        B.gameObject.SetActive(false);

        active = A;
        standby = B;

        if (plans != null && plans.Count > 0)
            SetupChunk(active, plans[0], 0);
    }

    /// <summary>
    /// 【按鍵時呼叫】預備 standby 路口、計算並儲存 pendingIsCorrect。
    /// 本方法「只預備、不提交」：idx、active、standby 在此方法中不改變。
    /// 在玩家按鍵後立刻呼叫，讓 standby 路口可在玩家移動途中同時在背景建置完成。
    /// </summary>
    public void PrepareMove(BranchDir dir)
    {
        int chosen = (dir == BranchDir.Left) ? 0 : 1;
        pendingIsCorrect = (chosen == plans[idx].correctDir);

        // 預估答題後的 Tier，確保 standby 路口的提示難度與預期結果一致，
        // 而非等到 CommitMove 才決定（避免路口建置延遲）
        int projectedTier;
        if (pendingIsCorrect)
            projectedTier = GetTierForStreak(consecutiveCorrect + 1);
        else
        {
            int newTier = Mathf.Max(0, currentTierIndex - 1);
            projectedTier = newTier;
        }

        Transform exit = (dir == BranchDir.Left) ? active.exitLeft : active.exitRight;
        standby.AlignEntryTo(exit);

        // 若答對，standby 顯示下一題（idx+1）；若答錯，standby 顯示同一題（idx）
        int targetIdx = pendingIsCorrect ? (idx + 1) % plans.Count : idx;
        SetupChunk(standby, plans[targetIdx], projectedTier);
        standby.gameObject.SetActive(true);

        Debug.Log($"[Loop] PrepareMove dir={dir}, pendingIsCorrect={pendingIsCorrect}, targetIdx={targetIdx}, projectedTier={projectedTier}");
    }

    /// <summary>
    /// 【MoveRoute 結尾呼叫】正式提交答案、切換路口。
    /// 答錯才在此扣分，撞牆扣分由 ObstacleHitDetector 負責。
    /// </summary>
    public void CommitMove()
    {
        firstAisle?.SetActive(false);

        if (pendingIsCorrect)
        {
            // 題目循環：超過 plans.Count 後回到第 0 題，避免遊戲因題目耗盡而中斷
            idx = (idx + 1) % plans.Count;
            consecutiveCorrect++;
        }
        else
        {
            tgame.Wrong();
            currentTierIndex = Mathf.Max(0, currentTierIndex - 1);
            // 答錯後連勝數降至當前（已降級的）Tier 的最低門檻，而非歸零，
            // 避免玩家因一次答錯就被懲罰回到最低難度
            consecutiveCorrect = GetTierMinStreak(currentTierIndex);
        }
        currentTierIndex = GetTierForStreak(consecutiveCorrect);

        previousJunction = active;
        // 交換 active / standby：原本的 standby 成為新的 active（玩家即將進入的路口）
        var tmp = active; active = standby; standby = tmp;

        // 清除舊路口（現在的 standby）的提示與障礙，讓它準備成為下一道題的空白路口
        standby.ClearHints();
        standby.ClearObstacle();

        Debug.Log($"[Loop] CommitMove pendingIsCorrect={pendingIsCorrect}, idx={idx}, tier={currentTierIndex}, streak={consecutiveCorrect}");
    }

    /// <summary>
    /// 【撞牆時呼叫】取消預備，隱藏 standby，idx 與 active 維持不變。
    /// idx 不推進意味著玩家需要重新作答同一題。
    /// </summary>
    public void CancelPending()
    {
        standby.gameObject.SetActive(false);

        // TODO：此行被注解掉，導致 active 路口的提示在撞牆後不會被還原。
        // 若玩家在移動中已觸發提示隱藏，撞牆後 active 提示不會重現。
        // 確認此行為是否符合設計意圖，若需還原請取消注解：
        // SetupChunk(active, plans[idx], currentTierIndex)

        Debug.Log("[Loop] CancelPending → standby 隱藏，題目不推進");
    }
}
