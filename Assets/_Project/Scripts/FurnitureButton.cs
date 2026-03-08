using UnityEngine;
using UnityEngine.UI;

public class FurnitureButton : MonoBehaviour
{
    [Header("Data món đồ này")]
    public FurnitureData data;          // Kéo asset FurnitureData vào đây

    [Header("Tham chiếu hệ thống đặt đồ")]
    public FurniturePlacer placer;      // Kéo object có FurniturePlacer vào (GameController) :contentReference[oaicite:2]{index=2}

    [Header("UI hiển thị")]
    public Image iconImage;             // Image để hiện icon (bên trong nút)

    private void Start()
    {
        // Gán icon từ data
        if (data != null && iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.preserveAspect = true;   // GIỮ TỈ LỆ, TỰ FIT VÀO NÚT
        }

        // Đăng ký click
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClickButton);
    }

    private void OnClickButton()
    {
        if (placer == null || data == null)
        {
            Debug.LogWarning("FurnitureButton: Chưa gán placer hoặc data!");
            return;
        }

        // Bắt đầu chế độ đặt đồ
        placer.StartPlacement(data);
    }
}
