using UnityEngine;

/// <summary>
/// 使用 OnCollisionEnter 偵測玩家碰到障礙物的腳本（障礙物 Collider 需為實體碰撞，isTrigger = false）。
///
/// 與 ObstacleHitDetector.cs 的差異：
///   - 本腳本：障礙物為實體碰撞（isTrigger = false），使用 OnCollisionEnter，
///             適合需要真實物理彈力的障礙物（如實體牆壁）。
///   - ObstacleHitDetector.cs：障礙物設為 Trigger，使用 OnTriggerEnter，
///             適合需要穿越偵測（不產生物理推力）的場景。
/// 兩者根據場景中障礙物的 Collider 設定選擇其一使用，通常不同時啟用。
/// </summary>
public class PlayerObstacleCollision : MonoBehaviour
{
    public Transform returnPoint; // 由外部傳入：通常是目前路口的 entry（玩家回歸位置）

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<TimedObstacle>() != null)
        {
            Debug.Log("玩家撞到障礙 → 傳回前一個路口");

            if (returnPoint != null)
            {
                Vector3 pos = returnPoint.position;
                // Y 值強制設為 -31f（場景地面 Y 座標，與 PlayerHeightFixer.fixedY 一致）。
                // 玩家被物理引擎碰撞後可能被彈起，強制設定 Y 確保落回正確的地面高度。
                transform.position = new Vector3(pos.x, -31f, pos.z);
                transform.rotation = returnPoint.rotation;
            }

            // 回報錯誤給 TGame
            var game = FindObjectOfType<TGame>();
            if (game != null)
                game.Wrong();
        }
    }
}
