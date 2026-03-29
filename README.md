# Block-based Personal Knowledge Management System (Block-based PKM)

## Giới thiệu
Block-based Personal Knowledge Management System (Block-based PKM) là một ứng dụng web hỗ trợ người dùng lưu trữ, tổ chức và quản lý tri thức cá nhân theo **mô hình dạng khối (block-based)**.  
Hệ thống cho phép xây dựng nội dung linh hoạt thông qua các block độc lập như văn bản, tiêu đề, danh sách công việc và đoạn mã nguồn, giúp người dùng dễ dàng chỉnh sửa, sắp xếp và mở rộng tri thức.

Dự án được thực hiện như một **đồ án sinh viên ngành Công nghệ Thông tin**, đồng thời mang tính ứng dụng thực tế cao, phù hợp với xu hướng quản lý tri thức hiện đại.

---

## Mục tiêu dự án
- Nghiên cứu và áp dụng mô hình **Block-based Knowledge Management**
- Xây dựng hệ thống quản lý tri thức cá nhân hiện đại
- Rèn luyện kỹ năng phát triển ứng dụng web theo kiến trúc client–server
- Vận dụng .net cho backend và Vue.js cho frontend
- Tạo nền tảng có khả năng mở rộng cho các hệ thống quản lý tri thức thông minh trong tương lai

---

## Mô hình Block-based Knowledge Management
Trong mô hình block-based, nội dung được chia nhỏ thành các **khối (block)** độc lập.  
Mỗi block đại diện cho một đơn vị thông tin riêng biệt và có thể:
- Tạo mới, chỉnh sửa hoặc xóa độc lập
- Sắp xếp lại thứ tự bằng kéo – thả
- Kết hợp nhiều loại block trong cùng một trang nội dung

Cách tiếp cận này giúp tăng tính linh hoạt, khả năng tái sử dụng và tổ chức tri thức hiệu quả hơn so với phương pháp ghi chép truyền thống.

---

## Các chức năng chính

### Quản lý người dùng
- Đăng ký và đăng nhập tài khoản
- Xác thực bằng JWT
- Phân quyền và bảo mật dữ liệu người dùng

### Quản lý Workspace
- Tạo và quản lý các workspace cá nhân
- Mỗi workspace đại diện cho một không gian tri thức hoặc lĩnh vực riêng

### Quản lý Page
- Tạo, chỉnh sửa, xóa trang ghi chú
- Hỗ trợ cấu trúc phân cấp (page con)

### Quản lý Block
- Hỗ trợ các loại block:
  - Text (văn bản)
  - Heading (tiêu đề)
  - Todo (danh sách công việc)
  - Code (đoạn mã)
- Chỉnh sửa nội dung từng block
- Thay đổi thứ tự block trong trang

### Tìm kiếm nội dung
- Tìm kiếm ghi chú theo từ khóa
- Lọc kết quả theo workspace hoặc page

---

## Công nghệ sử dụng

### Backend
- .NET Core Web API
- ASP.NET Core Identity + JWT Authentication
- Entity Framework Core (EF Core)
- PostgreSQL

### Frontend
- Vue.js 3
- Vue Router
- Pinia
- Axios
- TipTap Editor (block-based editor)

---

## Kiến trúc hệ thống
Hệ thống được xây dựng theo mô hình **Client – Server**:
- Frontend (Vue.js) đảm nhiệm giao diện và trải nghiệm người dùng
- Backend (Spring Boot) xử lý nghiệp vụ, xác thực và quản lý dữ liệu
- Giao tiếp thông qua RESTful API

---

## Kế hoạch phát triển
Dự án được triển khai theo các giai đoạn:
1. Khảo sát & thiết kế hệ thống
2. Phát triển backend
3. Phát triển frontend
4. Kiểm thử & tối ưu
5. Hoàn thiện báo cáo và thuyết trình

---

## Hướng phát triển trong tương lai
- Bổ sung liên kết giữa các block và page
- Áp dụng full-text search nâng cao
- Hỗ trợ chia sẻ workspace
- Tích hợp AI hỗ trợ gợi ý và phân tích tri thức

---

## Thông tin
- **Loại dự án**: Đồ án sinh viên
- **Lĩnh vực**: Quản lý tri thức cá nhân (Personal Knowledge Management)
- **Mô hình**: Block-based Knowledge Management
