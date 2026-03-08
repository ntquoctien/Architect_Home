# 🏠 Hướng Dẫn Setup Custom Room Editor

## 📋 Tổng Quan Hệ Thống

Hệ thống Custom Room cho phép chuyển đổi giữa:
- **Room_Static**: Phòng tĩnh với kích thước cố định (hệ thống cũ)
- **DynamicRoomSystem**: Phòng động có thể chỉnh sửa runtime (hệ thống mới)

---

## 🔧 BƯỚC 1: Tạo GameObject DynamicRoomSystem

1. **Click chuột phải** trong Hierarchy → **Create Empty**
2. **Đổi tên** thành `DynamicRoomSystem`
3. **Đặt Position** = `(0, 0, 0)` (cùng vị trí với Room_Static)
4. **Tắt GameObject** này (uncheck checkbox ở Inspector) - sẽ được bật khi cần

### Thêm Scripts vào DynamicRoomSystem:
Chọn `DynamicRoomSystem` và thêm 3 component:

| Component | Chức năng |
|-----------|-----------|
| `RoomEditorController` | Điều khiển chính, xử lý input |
| `RoomMeshGenerator` | Tự động thêm (RequireComponent) |
| `RoomMeasurementView` | Tự động thêm (RequireComponent) |

### Cấu hình RoomEditorController:
```
┌─ RoomEditorController ─────────────────────────────────┐
│                                                        │
│ ▼ Initial Room Settings                                │
│   Initial Width: 5        ← Giá trị mặc định           │
│   Initial Length: 5       ← Giá trị mặc định           │
│                                                        │
│ ▼ Node Settings                                        │
│   Node Handle Prefab: (None) ← Để trống = tự tạo sphere│
│   Node Handle Size: 0.3                                │
│   Node Normal Color: 🟢 Green                          │
│   Node Hover Color: 🟡 Yellow                          │
│   Node Selected Color: 🔵 Cyan                         │
│                                                        │
│ ▼ Edge Interaction                                     │
│   Edge Click Threshold: 0.5                            │
│                                                        │
│ ▼ Integration                                          │
│   Room Theme Controller: [Kéo từ Room_Static vào]      │
│   Camera Controller: [Kéo Main Camera vào]             │
└────────────────────────────────────────────────────────┘
```

### Cấu hình RoomMeshGenerator:
```
┌─ RoomMeshGenerator ────────────────────────────────────┐
│                                                        │
│ ▼ Mesh Settings                                        │
│   Wall Height: 3          ← Phải khớp với Room_Static  │
│   Wall Thickness: 0.1     ← Phải khớp với Room_Static  │
│   Extrude Walls Inward: ☑ true                         │
│   Generate Wall Caps: ☑ true                           │
│                                                        │
│ ▼ UV Settings                                          │
│   UV Scale: 1                                          │
│                                                        │
│ ▼ Mesh Objects                                         │
│   Floor Object: (None)    ← Để trống = tự tạo         │
│   Walls Object: (None)    ← Để trống = tự tạo         │
└────────────────────────────────────────────────────────┘
```

### Cấu hình RoomMeasurementView:
```
┌─ RoomMeasurementView ──────────────────────────────────┐
│                                                        │
│ ▼ Measurement Settings                                 │
│   Show Measurements: ☑ true                            │
│   Measurement Height: 0.1                              │
│   Measurement Unit: "m"                                │
│   Decimal Places: 1                                    │
│                                                        │
│ ▼ Text Style                                           │
│   Text Size: 0.5                                       │
│   Text Color: ⬜ White                                 │
│   Background Color: ⬛ Black (Alpha 0.7)               │
└────────────────────────────────────────────────────────┘
```

---

## 🔧 BƯỚC 2: Tạo GameModeManager

1. **Click chuột phải** trong Hierarchy → **Create Empty**
2. **Đổi tên** thành `GameModeManager`
3. **Thêm Script**: `RoomModeSwitcher`

### Cấu hình RoomModeSwitcher:
```
┌─ RoomModeSwitcher ─────────────────────────────────────┐
│                                                        │
│ ▼ Old Static Room System                               │
│   Old Room: [Kéo Room_Static vào]                      │
│             ↳ Component RoomController                 │
│   Old Room Object: [Kéo Room_Static vào]               │
│             ↳ GameObject gốc                           │
│                                                        │
│ ▼ New Dynamic Room System                              │
│   New Room Editor: [Kéo DynamicRoomSystem vào]         │
│             ↳ Component RoomEditorController           │
│   New Room Object: [Kéo DynamicRoomSystem vào]         │
│             ↳ GameObject gốc                           │
│                                                        │
│ ▼ Optional UI References                               │
│   Btn Custom Room: [Kéo Btn_CustomRoom vào]            │
│             ↳ Tự động gán OnClick listener             │
│                                                        │
│ ▼ Debug                                                │
│   Log Transitions: ☑ true                              │
└────────────────────────────────────────────────────────┘
```

---

## 🔧 BƯỚC 3: Cập nhật CameraController trên Main Camera

Đảm bảo `CameraController` có các reference đúng:

```
┌─ CameraController (Main Camera) ───────────────────────┐
│                                                        │
│ ▼ Room Center (Target)                                 │
│   Target: [Kéo Room_Static/Pivot vào]                  │
│   Floor Renderer: [Kéo Room_Static/Floor vào]          │
│   Look Height: 1.2                                     │
│                                                        │
│ ... (các setting khác giữ nguyên)                      │
└────────────────────────────────────────────────────────┘
```

> **Lưu ý**: Khi chuyển sang Edit Mode, `RoomEditorController` sẽ tự động 
> cập nhật `floorRenderer` của CameraController sang Floor mới.

---

## 🔧 BƯỚC 4: Cấu hình Room_Static (Hệ thống cũ)

Đảm bảo `RoomController` và `RoomThemeController` đã được cấu hình:

```
┌─ RoomController (Room_Static) ─────────────────────────┐
│                                                        │
│ ▼ Dimensions (meters)                                  │
│   Width: 8         ← Kích thước phòng hiện tại         │
│   Length: 10                                           │
│   Height: 3                                            │
│                                                        │
│ ▼ References                                           │
│   Floor: [Room_Static/Floor]                           │
│   Walls Root: [Room_Static/Walls]                      │
│   Wall01-04: [Các Wall tương ứng]                      │
└────────────────────────────────────────────────────────┘
```

---

## 🔧 BƯỚC 5: Gán Button UI (Nếu chưa gán ở Bước 2)

### Cách 1: Kéo thả vào RoomModeSwitcher (Khuyến nghị)
- Đã làm ở Bước 2, field `Btn Custom Room`

### Cách 2: Gán thủ công OnClick
1. Chọn `Btn_CustomRoom` trong Hierarchy
2. Tìm component **Button** trong Inspector
3. Ở mục **On Click ()**:
   - Click **+** để thêm event
   - Kéo `GameModeManager` vào ô Object
   - Chọn **RoomModeSwitcher → SwitchToEditMode()**

---

## 📐 Hierarchy Cuối Cùng

```
Scene
├── 📹 Main Camera
│       └── Script: CameraController
│           • Target: Room_Static/Pivot
│           • Floor Renderer: Room_Static/Floor
│
├── 💡 Directional Light
│
├── ⚙️ GameModeManager          ← MỚI TẠO
│       └── Script: RoomModeSwitcher
│           • Old Room: Room_Static (RoomController)
│           • Old Room Object: Room_Static
│           • New Room Editor: DynamicRoomSystem (RoomEditorController)
│           • New Room Object: DynamicRoomSystem
│           • Btn Custom Room: Btn_CustomRoom
│
├── 🏠 Room_Static              ← Active mặc định ✅
│   │   └── Scripts: RoomController, RoomThemeController
│   ├── Pivot
│   ├── Floor
│   └── Walls
│       ├── Wall_0, Wall_1, Wall_2, Wall_3
│
├── ✏️ DynamicRoomSystem        ← Inactive mặc định ❌
│       └── Scripts: RoomEditorController, RoomMeshGenerator, RoomMeasurementView
│           • Room Theme Controller: Room_Static (RoomThemeController)
│           • Camera Controller: Main Camera (CameraController)
│
├── 🖱️ EventSystem
│
└── 🖼️ FeatureCanvas
    └── Bottom_Toolbar
        └── Btn_CustomRoom      ← Gán vào RoomModeSwitcher
```

---

## ▶️ Cách Hoạt Động Khi Chạy Game

### Trạng thái ban đầu:
- `Room_Static`: **Active** → Hiển thị phòng tĩnh
- `DynamicRoomSystem`: **Inactive** → Ẩn

### Khi nhấn Btn_CustomRoom:
1. `RoomModeSwitcher.SwitchToEditMode()` được gọi
2. Lấy `width` và `length` từ `RoomController` (VD: 8m x 10m)
3. Bật `DynamicRoomSystem`
4. Gọi `RoomEditorController.InitializeFromExisting(8, 10)`
5. Tắt `Room_Static`
6. → Phòng động xuất hiện với **cùng kích thước** phòng cũ!

### Trong Edit Mode:
- **Kéo các Node (hình cầu)** ở góc để thay đổi hình dạng
- **Click vào cạnh tường** để tách tường (thêm node mới)
- **Click phải vào Node** để xóa (tối thiểu 3 node)
- Đo lường hiển thị realtime trên mỗi tường

---

## ⚠️ Lưu Ý Quan Trọng

1. **Đồng bộ Wall Height**: 
   - `RoomController.height` = `RoomMeshGenerator.wallHeight` = **3**

2. **Đồng bộ Wall Thickness**:
   - `RoomController.wallThickness` = `RoomMeshGenerator.wallThickness` = **0.1**

3. **Materials/Theme**:
   - `RoomEditorController` sẽ tự động gọi `RoomThemeController.ApplyTheme()`
   - Đảm bảo đã assign materials trong `RoomThemeController`

4. **Camera Bounds**:
   - Tự động cập nhật khi geometry thay đổi qua `CameraController.RebuildBoundsFromFloor()`

---

## 🧪 Test Checklist

- [ ] Play game → Thấy phòng tĩnh (Room_Static)
- [ ] Click Btn_CustomRoom → Phòng động xuất hiện cùng kích thước
- [ ] Console log: `[RoomModeSwitcher] Transitioning to Edit Mode...`
- [ ] Thấy các Node hình cầu ở 4 góc
- [ ] Kéo được Node để thay đổi hình dạng
- [ ] Số đo (VD: "8.0m") hiển thị trên mỗi cạnh
- [ ] Vật liệu/màu giống phòng cũ (từ RoomThemeController)
