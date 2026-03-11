using UnityEngine;

/// <summary>
/// 使用 OnTriggerEnter 偵測玩家碰到障礙物的腳本（障礙物 Collider 需設為 isTrigger = true）。
///
/// 與 PlayerObstacleCollision.cs 的差異：
///   - 本腳本：障礙物設為 Trigger（isTrigger = true），使用 OnTriggerEnter
///   - PlayerObstacleCollision.cs：障礙物為實體碰撞（isTrigger = false），使用 OnCollisionEnter
/// 兩者根據場景中障礙物的 Collider 設定選擇其一使用。
/// </summary>
public class ObstacleHitDetector : MonoBehaviour
{
    // 玩家撞牆後要傳送回的位置（始終指向「最後進入的路口」起始點）。
    // 由 EntryTrigger.OnTriggerEnter() 在玩家每次進入路口時動態更新。
    public Transform returnPoint;

    [SerializeField] private TGameJunctionLoop loop;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Obstacle")) return;

        // 步驟順序說明：必須先停止移動再傳送，
        // 若順序相反（先傳送再停止），協程會在新位置繼續執行移動，導致位置錯誤。

        // 步驟 1：停止玩家移動（停止 MoveRoute 協程 + 解鎖 + 重置動畫）
        var input = FindObjectOfType<TPlayerInput>();
        if (input != null)
            input.ForceStopMovement();

        // 步驟 2：取消預備（隱藏 standby 路口，idx 不推進，玩家需重答同一題）
        if (loop != null)
            loop.CancelPending();

        // 步驟 3：傳送回路口起點（使用 returnPoint，由 EntryTrigger 動態維護）
        if (returnPoint != null)
        {
            transform.position = returnPoint.position;
            transform.rotation = returnPoint.rotation;
        }

        // 步驟 4：回報撞牆懲罰（此扣分與答題對錯無關，純屬碰到障礙的懲罰）
        var game = FindObjectOfType<TGame>();
        if (game != null)
            game.Wrong();
    }
}
