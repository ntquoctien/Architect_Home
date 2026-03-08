# Design: Memory Architecture & Schema

## 1. Data Models
### `MemoryEntry` (Table: `memories`)
- `id`: GUID (Primary Key)
- `content`: TEXT (Nội dung thô)
- `embedding`: BLOB (Mảng float 384/768 chiều - Tùy Model)
- `type`: TEXT (Semantic | Episodic)
- `importance`: FLOAT (0.0 - 1.0)
- `timestamp`: DATETIME

## 2. Component Architecture
- **MemoryManager.cs**: Singleton điều phối việc ghi/đọc.
- **VectorProvider.cs**: Wrapper gọi Inference (Local hoặc API) để lấy Embedding.
- **AntigravityBridge.cs**: Inject dữ liệu vào Prompt của Agent dưới dạng `Context`.

## 3. Workflow Logic
1. **Ghi (Store):** Agent thực hiện hành động -> `MemoryManager` tạo `MemoryEntry` -> Lưu SQLite.
2. **Đọc (Retrieve):** User hỏi -> `VectorProvider` tạo Embedding câu hỏi -> Query SQLite tìm `Top K` kết quả có Cosine Similarity cao nhất -> Trả về Context cho Agent.

## 4. Unity 6 Specifics
- Sử dụng `Unity.Collections` để tối ưu hóa mảng Embedding.
- Lưu DB tại `Application.persistentDataPath`.