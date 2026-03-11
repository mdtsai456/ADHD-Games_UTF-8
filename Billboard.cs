using UnityEngine;

/// <summary>
/// 讓物件的正面始終朝向主相機（廣告牌效果）。
/// 使用 LateUpdate 而非 Update，確保在相機完成移動後才更新旋轉，
/// 避免因相機和物件在同一幀更新導致一幀的方向閃爍。
/// </summary>
public class Billboard : MonoBehaviour
{
    public Camera mainCamera;

    void Start()
    {
        // 先嘗試找 MainCamera
        mainCamera = Camera.main;

        // 如果沒找到，就手動抓場景中的第一個 Camera
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
            Debug.LogWarning("MainCamera not found, fallback to first Camera in scene.");
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("沒找到camera");
            return; 
        }

        // LookAt 讓 transform.forward 指向相機（第一步，用來取得方向向量）
        transform.LookAt(mainCamera.transform);

        // LookRotation 以「物件→相機」方向為 forward 重新計算旋轉，
        // 確保物件正面（而非背面）朝向相機，同時避免 LookAt 可能造成的翻轉問題
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }
}
