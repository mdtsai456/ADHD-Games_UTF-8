using UnityEngine;
using static TGame;

/// <summary>
/// 單個路口（Junction）的 MonoBehaviour。
/// 負責：
///   1. 與前一路口的出口對齊（AlignEntryTo）
///   2. 根據 JunctionPlan 顯示好/壞提示圖示（SetupHintsFromChildren）
///   3. 啟動並配置障礙物（SetupObstacle）
///   4. 清除提示與障礙（ClearHints / ClearObstacle）供路口循環重用
/// </summary>
public class TJunction : MonoBehaviour
{
    [Header("Custom Start Point")]
    public Transform startPoint; // 手動在 Inspector 指定的玩家回歸起始點

    [Header("Sockets")]
    public Transform entry;      // 本路口的入口（對齊前一路口的 exit 時使用）
    public Transform exitLeft;   // 左側出口
    public Transform exitRight;  // 右側出口

    [Header("Hint Anchors")]
    public Transform leftGoodParent;   // 左側「好」提示的掛載點
    public Transform leftBadParent;    // 左側「壞」提示的掛載點
    public Transform rightGoodParent;  // 右側「好」提示的掛載點
    public Transform rightBadParent;   // 右側「壞」提示的掛載點

    [Header("Obstacle")]
    public Transform obstacleRoot; // 障礙物容器，子物件按 ObstacleType 排列（index = obstacleType - 1）

    // 倒數計時顯示容器物件（名稱 A 不直覺，實際用途是顯示障礙物的倒數 UI）
    public GameObject A;

    private IObstacle currentObstacle;

    /// <summary>
    /// 將本路口的 entry 對齊到目標 Transform（通常是前一路口的 exitLeft 或 exitRight）。
    /// 使用 delta 旋轉 + delta 位置的方式移動整個路口 chunk。
    /// </summary>
    public void AlignEntryTo(Transform target)
    {
        // 計算 entry 需要旋轉多少才能對齊 target 的朝向（delta 旋轉）
        Quaternion deltaRot = target.rotation * Quaternion.Inverse(entry.rotation);
        // 將 delta 旋轉套用到整個 transform，使 entry 的朝向與 target 一致
        transform.rotation = deltaRot * transform.rotation;

        // 計算 entry 需要位移多少才能與 target 位置重合，並平移整個 chunk
        Vector3 deltaPos = target.position - entry.position;
        transform.position += deltaPos;
    }

    /// <summary>
    /// 關閉所有提示掛載點下的子物件（使用 SetActive(false)，保留物件供下次重用）。
    /// 從後往前迭代以避免 childCount 在迴圈中改變的問題。
    /// </summary>
    public void ClearHints()
    {
        void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) t.GetChild(i).gameObject.SetActive(false);
        }
        if (leftGoodParent)  Clear(leftGoodParent);
        if (leftBadParent)   Clear(leftBadParent);
        if (rightGoodParent) Clear(rightGoodParent);
        if (rightBadParent)  Clear(rightBadParent);
    }

    public void SetupHints(JunctionPlan jp, GameObject good, GameObject bad)
    {
        ClearHints();
        jp.goodOnLeft = jp.correctDir == 0;
        Transform goodAnchor = jp.goodOnLeft ? leftGoodParent : rightGoodParent;
        Transform badAnchor = jp.goodOnLeft ? rightBadParent : leftBadParent;

        if (jp.displayMode == ClueMode.Both || jp.displayMode == ClueMode.OnlyGood)
        {
            if (good) { good.transform.SetParent(goodAnchor, false); good.transform.localPosition = Vector3.zero; good.SetActive(true); }
        }
        if (jp.displayMode == ClueMode.Both || jp.displayMode == ClueMode.OnlyBad)
        {
            if (bad) { bad.transform.SetParent(badAnchor, false); bad.transform.localPosition = Vector3.zero; bad.SetActive(true); }
        }
    }

    /// <summary>
    /// 根據 JunctionPlan 啟用對應的提示子物件。
    /// goodOnLeft 決定好提示落在左側或右側，壞提示落在另一側。
    /// goodIndex / badIndex 對應提示難度 Tier（索引越高圖示越難辨識）。
    /// </summary>
    public void SetupHintsFromChildren(JunctionPlan jp)
    {
        ClearHints();

        // 根據 goodOnLeft 決定好/壞提示的掛載位置
        // 注意：badAnchor 使用「對側的 BadParent」而非「對側的 GoodParent」
        Transform goodAnchor = jp.goodOnLeft ? leftGoodParent  : rightGoodParent;
        Transform badAnchor  = jp.goodOnLeft ? rightBadParent  : leftBadParent;

        // 顯示好提示（ClueMode 為 Both 或 OnlyGood 時）
        if (jp.displayMode == ClueMode.Both || jp.displayMode == ClueMode.OnlyGood)
        {
            if (goodAnchor && goodAnchor.childCount > 0)
            {
                // Clamp 防止 goodIndex（Tier）超出場景中實際放置的提示數量
                int gi = Mathf.Clamp(jp.goodIndex, 0, goodAnchor.childCount - 1);
                goodAnchor.GetChild(gi).gameObject.SetActive(true);
            }
        }
        // 顯示壞提示（ClueMode 為 Both 或 OnlyBad 時）
        if (jp.displayMode == ClueMode.Both || jp.displayMode == ClueMode.OnlyBad)
        {
            if (badAnchor && badAnchor.childCount > 0)
            {
                int bi = Mathf.Clamp(jp.badIndex, 0, badAnchor.childCount - 1);
                badAnchor.GetChild(bi).gameObject.SetActive(true);
            }
        }

        string leftName = "無", rightName = "無";
        if (jp.goodOnLeft)
        {
            if (jp.displayMode != ClueMode.OnlyBad && goodAnchor && goodAnchor.childCount > 0)
                leftName = goodAnchor.GetChild(Mathf.Clamp(jp.goodIndex, 0, goodAnchor.childCount - 1)).name;
            if (jp.displayMode != ClueMode.OnlyGood && badAnchor && badAnchor.childCount > 0)
                rightName = badAnchor.GetChild(Mathf.Clamp(jp.badIndex, 0, badAnchor.childCount - 1)).name;
        }
        else
        {
            if (jp.displayMode != ClueMode.OnlyBad && goodAnchor && goodAnchor.childCount > 0)
                rightName = goodAnchor.GetChild(Mathf.Clamp(jp.goodIndex, 0, goodAnchor.childCount - 1)).name;
            if (jp.displayMode != ClueMode.OnlyGood && badAnchor && badAnchor.childCount > 0)
                leftName = badAnchor.GetChild(Mathf.Clamp(jp.badIndex, 0, badAnchor.childCount - 1)).name;
        }
        Debug.Log($"[TJunction] 左={leftName} | 右={rightName} | tier={jp.goodIndex} | mode={jp.displayMode}");
    }

    /// <summary>
    /// 停止並清除當前障礙物，讓所有障礙子物件回到隱藏狀態（供路口循環重用）。
    /// </summary>
    public void ClearObstacle()
    {
        currentObstacle?.StopCycle();
        currentObstacle = null;
        if (!obstacleRoot) return;
        for (int i = 0; i < obstacleRoot.childCount; i++)
            obstacleRoot.GetChild(i).gameObject.SetActive(false);
    }

    /// <summary>
    /// 根據 JunctionPlan 啟動對應的障礙物，並套用時間配置。
    /// obstacleRoot 下的子物件依 ObstacleType 排列：
    ///   index = (int)obstacleType - 1（ObstacleType 從 1 開始，0 = None 表示無障礙）
    ///   GateUp=0, GateDown=1, Spike=2, Dog=3（對應 obstacleRoot 的子物件順序）
    /// </summary>
    public void SetupObstacle(JunctionPlan p)
    {
        ClearObstacle();
        if (!obstacleRoot)
        {
            Debug.LogWarning("[TJunction] ObstacleRoot 未設定！");
            return;
        }

        if (p.obstacleType == ObstacleType.None)
        {
            Debug.Log("[TJunction] 這題沒有障礙");
            A.SetActive(false); // 隱藏倒數計時顯示容器
            return;
        }

        // ObstacleType 從 1 開始，-1 換算為 0-based 子物件索引
        int index = (int)p.obstacleType - 1;
        if (index < 0 || index >= obstacleRoot.childCount)
        {
            Debug.LogWarning($"[TJunction] ObstacleType={p.obstacleType} 找不到對應 child，index={index}");
            return;
        }

        var child = obstacleRoot.GetChild(index).gameObject;
        child.SetActive(true);
        A.SetActive(true); // 顯示倒數計時容器（A 是倒數 UI 的容器，欄位名不直覺）
        Debug.Log($"[TJunction] 生成障礙 {child.name} (Type={p.obstacleType})");

        currentObstacle = child.GetComponentInChildren<IObstacle>();

        if (currentObstacle != null)
        {
            currentObstacle.Configure(new ObstacleConfig
            {
                hiddenSeconds = p.hiddenSeconds,
                showSeconds   = p.showSeconds,
                openSeconds   = p.openSeconds,
                phaseOffset   = p.phaseOffset
            });

            currentObstacle.StartCycle();

            Debug.Log($"[TJunction] 啟動障礙: {child.name} | " +
                      $"hidden={p.hiddenSeconds}s, show={p.showSeconds}s, open={p.openSeconds}s, phase={p.phaseOffset}");
        }
        else
        {
            Debug.LogWarning($"[TJunction] {child.name} 上沒有 IObstacle 元件！");
        }
    }

    public IObstacle GetCurrentObstacle() => currentObstacle;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var loop = FindObjectOfType<TGameJunctionLoop>();
        if (loop != null)
        {
            loop.OnEnteredEntry(this);
            Debug.Log("[TJunction] 玩家進入路口: " + this.name);
        }
    }
}
