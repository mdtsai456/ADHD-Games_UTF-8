using UnityEngine;

/// <summary>
/// 路口 Trigger 的三種模式：
///   EntryOnly   — 目前唯一啟用的模式，玩家進入路口時觸發 OnEnteredEntry 事件，
///                 並更新 ObstacleHitDetector.returnPoint（撞牆傳送點）。
///   LeftCommit  — 原本設計用來透過 Trigger 提交「選左」，已改由 TPlayerInput 鍵盤輸入處理，
///                 相關代碼已 comment out，保留供未來參考。
///   RightCommit — 同上，已停用。
/// </summary>
public enum EntryTriggerType { EntryOnly, LeftCommit, RightCommit }

/// <summary>玩家選擇方向的列舉（左/右）。</summary>
public enum BranchDir
{
    Left,
    Right
}

/// <summary>
/// 附加在路口入口 Trigger 上的腳本。
/// 玩家進入觸發區時執行四個功能：
///   1. 通知 TGameJunctionLoop（EntryOnly 模式）
///   2. 提交方向選擇（LeftCommit / RightCommit，已停用，改由鍵盤處理）
///   3. 更新 ObstacleHitDetector.returnPoint，確保撞牆時傳送回正確位置
///   4. 切換 A/B 物件的顯示狀態（路口 UI 切換）
/// </summary>
public class EntryTrigger : MonoBehaviour
{
    public TGameJunctionLoop loop;
    public TJunction owner;
    public string playerTag = "Player";

    // A：玩家進入此路口後要「顯示」的物件（通常是路口提示 UI 或場景物件）
    public GameObject A;
    // B：玩家進入此路口後要「隱藏」的物件（通常是上一段的過渡物件）
    public GameObject B;

    [Header("可選：這個 Trigger 是否為左右提交？")]
    public EntryTriggerType triggerType = EntryTriggerType.EntryOnly;


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // ── 功能 1：EntryOnly 模式 ── 通知 Loop 玩家進入此路口
        if (triggerType == EntryTriggerType.EntryOnly)
        {
            loop.OnEnteredEntry(owner);
        }

        // ── 功能 2：LeftCommit / RightCommit 模式（已停用）──
        // 此段邏輯已全面移至 TPlayerInput 鍵盤輸入處理，保留供參考或日後還原。
        // else
        // {
        //     int chosen = (triggerType == EntryTriggerType.LeftCommit) ? 0 : 1;
        //
        //     var plan = loop.plans[loop.CurrentIndex];
        //     bool isCorrect = (chosen == plan.correctDir);
        //
        //     var dir = triggerType == EntryTriggerType.LeftCommit ? BranchDir.Left : BranchDir.Right;
        //
        //     if (!isCorrect)
        //     {
        //         Debug.Log($"[EntryTrigger] WRONG → {triggerType}");
        //
        //         loop.tgame.Wrong();
        //         loop.OnWrongBranchCommitted(owner, dir);
        //     }
        //     else
        //     {
        //         Debug.Log($"[EntryTrigger] RIGHT → {triggerType}");
        //
        //         loop.OnBranchCommitted(owner, dir);
        //     }
        // }

        // ── 功能 3：更新 ObstacleHitDetector.returnPoint ──
        // 讓撞牆時的傳送位置始終指向玩家「最後進入的路口」起始點
        var detector = other.GetComponent<ObstacleHitDetector>();
        if (detector != null)
        {
            detector.returnPoint = owner.startPoint != null ? owner.startPoint : owner.entry;
            Debug.Log($"[EntryTrigger] returnPoint → {detector.returnPoint.name}");
        }

        // ── 功能 4：A/B 物件顯示切換 ──
        if (A) A.SetActive(true);
        if (B) B.SetActive(false);
    }
}
