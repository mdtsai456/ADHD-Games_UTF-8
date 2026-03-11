using UnityEngine;

/// <summary>
/// 每幀強制鎖定玩家的 Y 座標，防止物理引擎讓玩家浮起或下沉。
/// fixedY 對應場景地面的 Y 高度（預設 -31f）。
/// 使用 LateUpdate 而非 Update，確保在物理模擬（FixedUpdate）和
/// 其他腳本的 Update 都執行完後才修正高度，避免被物理引擎覆蓋。
/// </summary>
public class PlayerHeightFixer : MonoBehaviour
{
    // 場景地面的 Y 座標，與 PlayerObstacleCollision 及 ObstacleHitDetector 中的硬編碼值一致
    public float fixedY = -31f;

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.y - fixedY) > 0.01f)
        {
            pos.y = fixedY;
            transform.position = pos;
        }
    }
}
