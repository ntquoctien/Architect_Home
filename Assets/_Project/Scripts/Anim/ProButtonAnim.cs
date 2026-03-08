using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening; // Yêu cầu phải cài DOTween

public class ProButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("1. References (Kéo thả vào đây)")]
    public RectTransform iconTransform; // Kéo cái Icon_Visual (Child) vào
    public Image frameImage;           // Kéo cái Button (Parent/Hexagon) vào

    [Header("2. Settings Animation")]
    public float hoverScale = 1.1f;    // Độ to khi hover của khung
    public float iconPopScale = 1.25f; // Độ to khi hover của icon (nên to hơn khung)
    public float clickScale = 0.9f;    // Độ nhỏ khi nhấn xuống
    public float duration = 0.2f;      // Tốc độ (càng nhỏ càng nhanh)

    [Header("3. Colors (Màu sắc)")]
    public Color normalColor = new Color32(53, 53, 53, 255);   // Màu xám đen mặc định
    public Color hoverColor = new Color32(80, 80, 80, 255);    // Màu khi rê chuột (sáng hơn chút)
    public Color activeColor = new Color32(255, 153, 0, 255);  // Màu cam khi ĐANG CHỌN

    private bool isSelected = false; // Biến kiểm tra xem nút này có đang được chọn không
    private Vector3 originalIconScale;
    private Vector3 originalFrameScale;

    void Start()
    {
        // Lưu lại kích thước ban đầu để sau này reset
        if (iconTransform != null) originalIconScale = iconTransform.localScale;
        originalFrameScale = transform.localScale;

        // Đảm bảo màu bắt đầu đúng
        ResetToNormal();
    }

    // --- XỬ LÝ RÊ CHUỘT (HOVER) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelected) return; // Nếu đang chọn rồi thì không làm gì cả

        // 1. Khung: Phóng to nhẹ + Đổi màu sáng hơn
        transform.DOScale(originalFrameScale * hoverScale, duration).SetEase(Ease.OutBack);
        if (frameImage) frameImage.DOColor(hoverColor, duration);

        // 2. Icon: Phóng to mạnh hơn + Lắc nhẹ (Tạo điểm nhấn)
        if (iconTransform)
        {
            iconTransform.DOScale(originalIconScale * iconPopScale, duration).SetEase(Ease.OutBack);
            // Lắc icon một chút (Shake Rotation): Sức mạnh 15 độ, rung 10 lần
            iconTransform.DOShakeRotation(duration, 15f, 10, 90f); 
        }
    }

    // --- XỬ LÝ RỜI CHUỘT (EXIT) ---
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected) return;
        ResetToNormal();
    }

    // --- XỬ LÝ NHẤN XUỐNG (DOWN) ---
    public void OnPointerDown(PointerEventData eventData)
    {
        // Hiệu ứng nhún xuống (Squash)
        transform.DOScale(originalFrameScale * clickScale, 0.1f).SetEase(Ease.InSine);
    }

    // --- XỬ LÝ NHẢ CHUỘT (UP) ---
    public void OnPointerUp(PointerEventData eventData)
    {
        // Khi nhả ra thì quay lại kích thước hover (nếu vẫn còn hover) hoặc bình thường
        if (!isSelected)
        {
             transform.DOScale(originalFrameScale * hoverScale, 0.1f).SetEase(Ease.OutBack);
        }
    }
    
    // --- XỬ LÝ CLICK (LOGIC CHỌN) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ví dụ: Logic Toggle (Bấm vào thì sáng, bấm cái nữa thì tắt)
        // Bạn có thể sửa logic này tuỳ game (ví dụ bấm nút này thì tắt nút kia)
        // ToggleSelection(!isSelected); 
    }

    // Hàm gọi từ bên ngoài để set trạng thái (Ví dụ Manager gọi)
    public void SetActiveState(bool active)
    {
        isSelected = active;
        if (active)
        {
            // Trạng thái Active: To hơn xíu, màu cam rực rỡ
            transform.DOScale(originalFrameScale * 1.05f, duration);
            if (frameImage) frameImage.DOColor(activeColor, duration);
            if (iconTransform) iconTransform.DOScale(originalIconScale, duration);
        }
        else
        {
            ResetToNormal();
        }
    }

    void ResetToNormal()
    {
        transform.DOScale(originalFrameScale, duration);
        if (frameImage) frameImage.DOColor(normalColor, duration);
        if (iconTransform)
        {
            iconTransform.DOScale(originalIconScale, duration);
            iconTransform.rotation = Quaternion.identity; // Reset góc xoay
        }
    }
}