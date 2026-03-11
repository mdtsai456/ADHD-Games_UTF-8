using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// ⚠️ 開發測試用腳本，不屬於正式遊戲流程。
/// 功能：按 Q 鍵將玩家瞬間傳送至指定位置（teleportPoint），並計數傳送次數。
/// 正式 Build 前應停用或移除此腳本，避免影響遊戲體驗。
/// </summary>
public class test : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI numText;
    int num = 0;
    [Header("Target")]
    [SerializeField] private GameObject targetToShow;

    [Header("Options")]
    [SerializeField] private int startSeconds = 10;
    private int t;

    [Header("Teleport")]
    public Transform teleportPoint;   // 手動在 Inspector 指定的傳送目標位置

    private void Start()
    {
        if (targetToShow != null)
            targetToShow.SetActive(false);
    }

    private void Update()
    {
        // Q 鍵觸發傳送（測試用）
        if (Input.GetKeyDown(KeyCode.Q))
        {
            num++;
            numText.text = num.ToString();
            ShowTarget();
            t = startSeconds;

            TeleportToPoint();
        }
    }

    private void TeleportToPoint()
    {
        if (teleportPoint == null)
        {
            Debug.LogWarning("[test] teleportPoint 未指定！");
            return;
        }

        // 找玩家
        var player = FindObjectOfType<TPlayerInput>();
        if (player == null)
        {
            Debug.LogWarning("[test] 找不到 TPlayerInput (玩家)");
            return;
        }

        // 停止玩家移動（避免傳送後繼續執行上一個 MoveRoute 協程）
        player.ForceStopMovement();

        // 瞬間傳送至指定位置
        player.transform.position = teleportPoint.position;
        player.transform.rotation = teleportPoint.rotation;

        Debug.Log("[test] 玩家已傳送至 teleportPoint: " + teleportPoint.name);
    }

    public void ShowTarget()
    {
        if (targetToShow != null)
            targetToShow.SetActive(true);
    }
}
