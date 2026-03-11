using UnityEngine;
using TMPro;

/// <summary>
/// 具備五階段狀態機的抽象障礙物基類。
/// 狀態循環順序：VisibleBlocked → HiddenBlocked → Opening → Open → Closing → VisibleBlocked（重複）
/// 子類別（GateObstacle / SpikeObstacle / DogObstacle）可覆寫動畫細節，但不需實作狀態機本身。
/// </summary>
public abstract class TimedObstacle : MonoBehaviour, IObstacle
{
    [Header("Time Settings")]
    // 倒數計時 UI「顯示」期間的秒數（對應 ObstacleConfig.showSeconds，命名刻意不同，注意對應關係）
    public float visibleSeconds = 3f;
    // 倒數計時 UI「隱藏」期間的秒數（障礙仍擋住通道，但玩家看不到剩餘時間）
    public float hiddenSeconds = 5f;
    // 障礙物完全開啟、可通行的秒數
    public float openSeconds = 2f;

    [Header("UI")]
    public TextMeshProUGUI countdownText;

    [Header("Movement")]
    public Transform closedPosition;
    public Transform openPosition;
    public float moveSpeed = 3f;

    // 單一倒數計時器：在 VisibleBlocked/HiddenBlocked 階段從 (visibleSeconds + hiddenSeconds) 倒數至 0；
    // 在 Open 階段重設為 openSeconds 繼續倒數。
    private float timer;

    private enum State
    {
        // 顯示倒數計時 UI，障礙物擋住通道（玩家可看到剩餘時間，預測開門時機）
        VisibleBlocked,
        // 隱藏倒數計時 UI，障礙物仍擋住通道（玩家看不到剩餘時間，製造不確定感）
        HiddenBlocked,
        // 障礙物正移動至開放位置，尚不可通行（IsPassable = false）
        Opening,
        // 障礙物完全打開，可自由通行（IsPassable = true），timer 倒數 openSeconds
        Open,
        // 障礙物正移動回封閉位置，不可通行
        Closing,
        // 障礙物停用（路口被替換後由 StopCycle() 呼叫，物件靜止）
        Stopped
    }

    private State state = State.VisibleBlocked;
    private bool passable = false;

    public bool IsPassable => passable;

    // ===== 初始化 =====
    public void StartCycle()
    {
        passable = false;

        // timer 從「顯示秒 + 隱藏秒」的總和開始倒數，而非兩段獨立計時器。
        // 當 timer > hiddenSeconds 時為 VisibleBlocked（顯示倒數）；
        // 當 timer <= hiddenSeconds 時為 HiddenBlocked（隱藏倒數）；
        // 當 timer <= 0 時觸發 Opening。
        timer = visibleSeconds + hiddenSeconds;
        state = State.VisibleBlocked;

        UpdateCountdown(forceShow: true);

        Debug.Log($"[{name}] StartCycle → VisibleBlocked, timer={timer}");
    }

    public void StopCycle()
    {
        state = State.Stopped;
    }

    private void Update()
    {
        if (state == State.Stopped)
            return;

        switch (state)
        {
            case State.VisibleBlocked: HandleVisible(); break;
            case State.HiddenBlocked: HandleHidden(); break;
            case State.Opening: HandleOpening(); break;
            case State.Open: HandleOpen(); break;
            case State.Closing: HandleClosing(); break;
        }
    }

    // ===== A. 顯示倒數（timer 介於 visibleSeconds+hiddenSeconds 到 hiddenSeconds 之間）=====
    private void HandleVisible()
    {
        passable = false;

        UpdateCountdown();

        timer -= Time.deltaTime;

        // timer 降至 hiddenSeconds 以下時，代表「顯示倒數階段」已耗盡，轉入隱藏階段。
        // 此閾值本身等於隱藏秒數，不需額外變數。
        if (timer <= hiddenSeconds)
        {
            state = State.HiddenBlocked;
            UpdateCountdown(); // 此呼叫會讓 UI 自動隱藏（因為 timer <= hiddenSeconds）
            Debug.Log($"[{name}] Visible → Hidden, timer={timer}");
        }
    }

    // ===== B. 隱藏倒數（timer 從 hiddenSeconds 倒數至 0）=====
    private void HandleHidden()
    {
        passable = false;

        // UI 已在 HandleVisible 轉態時被 UpdateCountdown() 隱藏，這裡不需再次操作 UI。
        // 只需等待 timer 倒數至 0 後觸發開門動作。
        UpdateCountdown();

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            state = State.Opening;
            Debug.Log($"[{name}] Hidden → Opening");
        }
    }

    // ===== C. 門打開中（從 closedPosition 移動到 openPosition）=====
    private void HandleOpening()
    {
        passable = false;

        transform.position = Vector3.MoveTowards(
            transform.position,
            openPosition.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, openPosition.position) < 0.02f)
        {
            state = State.Open;
            passable = true;
            // 重設 timer 為 openSeconds，開始計算開放時間
            timer = openSeconds;

            Debug.Log($"[{name}] Opening → Open, timer={timer}");
        }
    }

    // ===== D. 開啟（可通行，倒數 openSeconds 後關閉）=====
    private void HandleOpen()
    {
        passable = true;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            passable = false;
            state = State.Closing;

            Debug.Log($"[{name}] Open → Closing");
        }
    }

    // ===== E. 關閉中（從 openPosition 移動回 closedPosition）=====
    private void HandleClosing()
    {
        passable = false;

        transform.position = Vector3.MoveTowards(
            transform.position,
            closedPosition.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, closedPosition.position) < 0.02f)
        {
            // 重設 timer 為總和，重新開始一個完整的顯示+隱藏週期
            timer = visibleSeconds + hiddenSeconds;

            state = State.VisibleBlocked;

            UpdateCountdown(forceShow: true);

            Debug.Log($"[{name}] Closing → VisibleBlocked restart, timer={timer}");
        }
    }

    // ===== UI 更新邏輯 =====
    private void UpdateCountdown(bool forceShow = false)
    {
        if (countdownText == null)
            return;

        float m = visibleSeconds + hiddenSeconds;

        if (forceShow)
        {
            countdownText.gameObject.SetActive(true);
            // 使用天花板取整（CeilToInt），讓倒數從最大整數開始而非小數，視覺上更直覺
            countdownText.text = Mathf.CeilToInt(timer).ToString();
            return;
        }

        // timer > hiddenSeconds 等同於「仍在 VisibleBlocked 階段」，才顯示倒數 UI
        if (timer > hiddenSeconds)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = Mathf.CeilToInt(timer).ToString();
        }
        else
        {
            // timer <= hiddenSeconds：進入隱藏階段，關閉倒數 UI
            countdownText.gameObject.SetActive(false);
        }
    }

    // ===== 外部設定 =====
    /// <summary>
    /// 套用外部傳入的時間參數。
    /// ⚠️ 命名對應注意：cfg.showSeconds（ObstacleConfig 的欄位名）→ visibleSeconds（本類別的欄位名）
    /// 兩者指相同概念，但因為歷史原因命名不一致。若修改欄位名請同步更新 ObstacleConfig。
    /// </summary>
    public void Configure(ObstacleConfig cfg)
    {
        hiddenSeconds  = cfg.hiddenSeconds;
        visibleSeconds = cfg.showSeconds;   // ← showSeconds 對應 visibleSeconds，命名不同但語意相同
        openSeconds    = cfg.openSeconds;
    }
}
