# Proposal: Long-term Memory System for Antigravity Agent

## 1. Problem Statement
Agent hiện tại bị "mất trí nhớ" sau mỗi Session. Cần một cơ chế lưu trữ bền vững (Persistent) để Agent nhận diện được người chơi và các sự kiện quan trọng trong quá khứ.

## 2. Proposed Solution
Thiết lập hệ thống bộ nhớ kép (Dual-Memory System) chạy Local:
- **Semantic Memory:** Lưu trữ tri thức, sở thích, sự kiện khái quát (Vector-based).
- **Episodic Memory:** Lưu trữ dòng thời gian các sự kiện cụ thể (Timeline-based).

## 3. Technology Stack
- **Engine:** Unity 6 (LTS).
- **Storage:** SQLite (với phần mở rộng Vector nếu có thể, hoặc C# Linq cho Small-scale Vector Search).
- **Framework:** Antigravity (Sử dụng `MemoryBead` làm đơn vị lưu trữ cơ bản).

## 4. Success Criteria
- [ ] Agent có thể lưu một thông tin mới vào DB.
- [ ] Agent có thể truy xuất thông tin liên quan dựa trên từ khóa (Semantic Search).
- [ ] Dữ liệu được bảo toàn sau khi tắt/mở lại Unity.