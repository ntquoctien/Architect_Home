using UnityEngine;
using UnityEngine.UI;

public class BlueprintModeController : MonoBehaviour
{
    [Header("1. Hệ thống Grid")]
    public GameObject gridVisualsObject;      // Kéo GameObject 'GridVisuals' vào đây
    public MonoBehaviour blueprintInteraction; // Kéo script 'BlueprintInteraction' vào đây

    [Header("2. UI Button")]
    public ProButtonAnim blueprintButtonAnim; // Kéo cái nút Blueprint UI vào đây

    private bool isEditMode = false;

    void Start()
    {
        // Mặc định khi vào game thì tắt chế độ Blueprint đi
        SetBlueprintMode(false);
    }

    // Hàm này sẽ gắn vào sự kiện OnClick của nút
    public void ToggleBlueprintMode()
    {
        isEditMode = !isEditMode;
        SetBlueprintMode(isEditMode);
    }

    void SetBlueprintMode(bool isActive)
    {
        // 1. Bật/Tắt hiển thị lưới
        if (gridVisualsObject != null) 
            gridVisualsObject.SetActive(isActive);

        // 2. Bật/Tắt tính năng tương tác (Con trỏ chuột ảo)
        if (blueprintInteraction != null) 
            blueprintInteraction.enabled = isActive;

        // 3. Đổi trạng thái nút bấm (Sáng đèn/Tắt đèn)
        if (blueprintButtonAnim != null)
            blueprintButtonAnim.SetActiveState(isActive);

        // Debug để kiểm tra
        Debug.Log("Chế độ Blueprint: " + (isActive ? "BẬT" : "TẮT"));
    }
}