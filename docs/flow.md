## 勇闖迷宮 遊戲流程

### 詳細流程

```
玩家站在路口，看線索
         │
         ▼
   按下左鍵或右鍵
         │
         ▼
  loop.PrepareMove(dir)
  ─────────────────────────────────
  ① 算出答對或答錯（存入 pendingIsCorrect）
  ② 備用路口搬到選的方向出口
     standby.AlignEntryTo(exit)
  ③ 備用路口裝好下一題或同一題
     targetIdx = 答對 ? idx+1 : idx
     SetupChunk(standby, plans[targetIdx], projectedTier)
  ④ 備用路口顯示出來
     standby.SetActive(true)
         │
         ▼
  MoveRoute 開始（Coroutine）
  ─────────────────────────────────
  Phase 1: 直走 firstLegDistance
  Phase 2: 轉身 yaw 90°
  Phase 3: 再走 secondLegDistance
         │
         ├──────────────────────────────────────────┐
         │                                          │
  【途中撞到障礙物】                           【順利走完】
         │                                          │
         ▼                                          ▼
  ObstacleHitDetector 觸發                  loop.CommitMove()
  ──────────────────────────────           ──────────────────────────────
  ① 停止角色移動                            ① firstAisle 隱藏
     input.ForceStopMovement()             ② 答對 → idx +1
  ② CancelPending()                           答錯 → tgame.Wrong() 扣分
     └─ 備用路口隱藏                        ③ previousJunction 更新
     └─ idx 不動                            ④ active ↔ standby 正式交換
     └─ active 不變
  ③ 傳送回路口起點
     transform.position = returnPoint
  ④ tgame.Wrong() 扣分（撞牆懲罰）
         │                                          │
         ▼                                          ▼
  玩家回到原路口                            玩家抵達下一個路口
  同一題再答一次                       ┌────────────┴────────────┐
                                    【答對】                  【答錯】
                                   全新下一題              同一題再來一次
```

### 關鍵檔案

| 檔案 | 職責 | 主要方法 |
|------|------|----------|
| `TGameJunctionLoop.cs` | 路口切換迴圈管理 | `PrepareMove()`, `CommitMove()`, `CancelPending()` |
| `TPlayerInput.cs` | 輸入偵測與移動動畫 | `MoveRoute()`, `ForceStopMovement()`, `OnMovePerformed()` |
| `ObstacleHitDetector.cs` | 障礙物碰撞處理 | `OnTriggerEnter()` |
| `TGame.cs` | 主遊戲控制器、計分 | `BuildPlans()`, `Wrong()`, `Correct()` |
| `TJunction.cs` | 單一路口節點設定 | `SetupHintsFromChildren()`, `SetupObstacle()`, `AlignEntryTo()` |

### Tier 難度遞增（連續答對越多，提示越難）

玩家每答對一題，`consecutiveCorrect` 就 +1。
系統根據連對次數查表，決定目前在第幾個難度等級（tier）。

```
連對次數    0~4   5~9   10~14   15~19   20+
tier          0     1      2       3      4
```

- **答對**：連對 +1 → 重新算 tier → 下一題用新 tier 挑提示
- **答錯**：tier 直接降一級，連對次數退回該級的門檻值
  - 例：原本 tier 2（連對 10），答錯 → tier 1，連對重設為 5

tier 會傳進 `SetupChunk()`，再餵給 `SetupHintsFromChildren()` 決定要顯示第幾組好/壞提示。
簡單說：**一直答對 → 提示越來越少或越難辨認；答錯就降回容易一點的提示。**

### 障礙物計時系統（擋在路上的閘門/尖刺）

每個路口在 `BuildPlans()` 時有 70% 機率會排一個障礙物。
障礙物類型：`GateUp`、`GateDown`、`Spike`、`Dog`。

障礙物掛著 `IObstacle` 介面，啟動後會跑一個循環：

```
  隱藏（hiddenSeconds）     看不到障礙，可以安心走
        │
        ▼
  倒數（showSeconds）       障礙出現並顯示倒數計時器
        │
        ▼
  開放（openSeconds）       障礙暫時消失，這段時間可以通過
        │
        └──→ 回到隱藏，繼續循環
```

- 每題的 `hiddenSeconds`、`showSeconds`、`openSeconds` 都是隨機產生的
- `TJunction.SetupObstacle()` 負責把障礙放出來，呼叫 `IObstacle.Configure()` 設定時間，再 `StartCycle()` 開跑
- 玩家撞到障礙 → 由 `ObstacleHitDetector` 處理（見上方流程圖）
