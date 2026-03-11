using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家方向輸入處理器，驅動三段式移動（直走→轉身→再直走）。
///
/// 移動流程（MoveRoute 協程）：
///   1. 直走 firstLegDistance（路口入口到分叉點）
///   2. 原地轉身（播放轉身動畫，同時旋轉）
///   3. 再直走 secondLegDistance（進入分叉後段，確認已通過路口）
///   4. CommitMove()（正式提交答題結果）
///
/// isMoving 布林鎖確保移動協程進行中時，玩家連按不會觸發多個平行 MoveRoute。
/// </summary>
public class TPlayerInput : GameInputSystemBase
{
    [Header("Junction 控制")]
    public TGameJunctionLoop loop;

    [Header("逐幀移動參數")]
    public float moveSpeed = 8f;
    public float rotateSpeed = 360f;

    // 玩家按鍵後先向前直走的距離（路口入口到分叉點的距離，以 Unity 世界單位計）
    public float firstLegDistance = 150f;
    // 轉彎後繼續向前走的距離（分叉後進入下一路口緩衝區的距離）
    public float secondLegDistance = 50f;

    [Header("動畫控制器")]
    public Animator animator;
    public string idleState = "Idle";
    public string runState = "Run";
    public string turnLeftTrigger = "TurnLeft";
    public string turnRightTrigger = "TurnRight";

    // isMoving：防止玩家在移動協程進行中連按方向鍵觸發多個平行 MoveRoute 的互鎖旗標
    private bool isMoving = false;
    private InputAction moveAction;

    protected new void OnEnable()
    {
        base.OnEnable();
        moveAction = controls.FindAction("GameControl/Movement");
        moveAction.performed += OnMovePerformed;
        moveAction.Enable();
    }

    protected new void OnDisable()
    {
        base.OnDisable();
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.Disable();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // isMoving 鎖：移動中忽略所有新輸入，防止協程重疊
        if (isMoving) return;

        //var loop = FindObjectOfType<TGameJunctionLoop>();
        //if (loop && loop.CurrentActive)
        //{
        //    var obstacle = loop.CurrentActive.GetComponentInChildren<IObstacle>();
        //    if (obstacle != null && !obstacle.IsPassable)
        //    {
        //        Debug.Log("【阻擋中】不能通行！");
        //        FindObjectOfType<TGame>()?.Wrong();
        //        return; // 不讓玩家動
        //    }
        //}

        float x = context.ReadValue<Vector2>().x;
        bool isLeft;

        if (x < -0.5f)      isLeft = true;
        else if (x > 0.5f)  isLeft = false;
        else return;

        // 在移動協程「開始前」就呼叫 PrepareMove，讓 standby 路口可以在玩家走路途中
        // 同時在背景建置完成（非同步建置窗口），避免玩家抵達時路口還沒準備好
        if (loop != null)
        {
            var dir = isLeft ? BranchDir.Left : BranchDir.Right;
            loop.PrepareMove(dir);
        }

        StartCoroutine(MoveRoute(isLeft ? -90f : +90f, isLeft));
    }

    /// <summary>
    /// 三段式移動協程：直走 → 轉身 → 再直走 → 提交答案。
    /// </summary>
    private IEnumerator MoveRoute(float yawDegrees, bool isLeft)
    {
        isMoving = true;

        // 播放「跑步」動畫
        if (animator)
            animator.CrossFade(runState, 0.15f);

        // ── 第一段：直走 firstLegDistance（進入路口到分叉點）──
        Vector3 p0 = transform.position;
        Vector3 f0 = FlatForward(transform);
        Vector3 p1 = p0 + f0 * firstLegDistance;

        yield return MoveTo(p1);

        // ── 第二段：播放轉身動畫並同時旋轉（不產生位移）──
        if (animator)
        {
            animator.SetTrigger(isLeft ? turnLeftTrigger : turnRightTrigger);
        }

        Quaternion r0 = transform.rotation;
        Quaternion r1 = Quaternion.AngleAxis(yawDegrees, Vector3.up) * r0;

        yield return RotateTo(r1);

        // 轉彎後清除舊路口的提示物件（玩家已選定方向，舊提示不再需要）
        loop?.CurrentActive?.ClearHints();

        // ── 第三段：轉身後再往前跑 secondLegDistance（進入分叉後段）──
        Vector3 f1 = FlatForward(r1);
        Vector3 p2 = p1 + f1 * secondLegDistance;

        yield return MoveTo(p2);

        transform.SetPositionAndRotation(p2, r1);

        // 先解除移動鎖定，確保即使 CommitMove 內部拋例外也不會造成永久卡住
        isMoving = false;

        // 停下來 → 播放 idle
        if (animator)
            animator.CrossFade(idleState, 0.15f);

        // 正式提交答案（推進 idx 或扣分）
        loop?.CommitMove();
    }

    private IEnumerator MoveTo(Vector3 targetPos)
    {
        while ((transform.position - targetPos).sqrMagnitude > 0.001f)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            transform.position = next;
            yield return null;
        }
        transform.position = targetPos;
    }

    private IEnumerator RotateTo(Quaternion targetRot)
    {
        while (Quaternion.Angle(transform.rotation, targetRot) > 0.05f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    /// <summary>
    /// 取 Transform 的水平前向方向（Y 軸歸零）。
    /// Y 軸歸零的原因：在有坡度的地形上，forward 會帶有斜面分量，
    /// 歸零後確保玩家沿水平面移動，避免爬坡或下坡時方向偏移。
    /// </summary>
    private static Vector3 FlatForward(Transform t)
    {
        Vector3 f = t.forward;
        f.y = 0f;
        return f.normalized;
    }

    /// <summary>
    /// 取旋轉四元數的水平前向方向（Y 軸歸零），理由同上。
    /// </summary>
    private static Vector3 FlatForward(Quaternion r)
    {
        Vector3 f = (r * Vector3.forward);
        f.y = 0f;
        return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
    }

    /// <summary>
    /// 輕量版停止：只停協程並解除 isMoving 鎖，不重置動畫。
    /// 適用於需要快速停止但不希望打斷動畫狀態的場景。
    /// </summary>
    public void ForceStopMoving()
    {
        StopAllCoroutines();
        isMoving = false;
    }

    /// <summary>
    /// 完整版停止：停協程 + 解除鎖 + 補充停止邏輯 + 重置動畫到 Idle。
    /// 由 ObstacleHitDetector（撞牆）和 test.cs（測試傳送）呼叫。
    /// </summary>
    public void ForceStopMovement()
    {
        Debug.Log("[TPlayerInput] ForceStopMovement()");

        // 停止所有協程（包括 MoveRoute）
        StopAllCoroutines();
        isMoving = false;

        // 停止位置偏移、移動過程
        SmoothSnapStop();

        // 回到 Idle 動畫
        ResetToIdle();
    }

    public void ResetToIdle()
    {
        if (animator != null)
        {
            animator.ResetTrigger("TurnLeft");
            animator.ResetTrigger("TurnRight");
            animator.Play("Idle", 0, 0f);
            Debug.Log("[TPlayerInput] Animator set to Idle");
        }
    }

    // 可選：停止補償協程（如有 snap coroutine 可在此停止）
    private void SmoothSnapStop()
    {
        //StopCoroutine("SnapCoroutine");
    }
}
