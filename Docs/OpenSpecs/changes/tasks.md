# Tasks: Memory Implementation Roadmap

- [ ] **Task 1: Infrastructure Setup**
    - Cài đặt SQLite Unity Plugin (hoặc chuẩn bị Wrapper).
    - Tạo thư mục `Assets/Scripts/Memory/`.
- [ ] **Task 2: Data Access Layer**
    - Viết `DatabaseHandler.cs` để tạo Table và CRUD cơ bản.
- [ ] **Task 3: Semantic Logic**
    - Triển khai hàm tính `CosineSimilarity`.
    - Tạo Mock Vector Provider (để test trước khi nhúng Model Embedding thật).
- [ ] **Task 4: Antigravity Integration**
    - Tạo `AntigravityMemoryBridge.cs`.
    - Cấu hình file `ANTIGRAVITY.md` để Agent biết cách sử dụng bộ nhớ.
- [ ] **Task 5: Verification**
    - Chạy Test Case: Lưu "Tôi thích màu đỏ" -> Hỏi "Màu yêu thích của tôi là gì?" -> Verify kết quả.