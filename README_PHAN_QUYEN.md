# Phân quyền chức năng theo từng tài khoản

Admin vào **Quản trị → Tài khoản & phân quyền** (`/Manage/Users`), bấm **🔐 Phân quyền** ở dòng của tài khoản Staff
(`/Manage/Permissions/{id}`), tick/bỏ tick các chức năng rồi lưu.

## Quy tắc
- **Admin**: luôn toàn quyền, không chỉnh được.
- **Staff**: chỉ truy cập các chức năng được tick. Menu ẩn mục không có quyền; truy cập thẳng URL sẽ ra trang "Không có quyền".
- **Customer**: không có quyền quản trị.
- Quản lý tài khoản & phân quyền chỉ Admin (không thể cấp cho Staff, tránh tự nâng quyền).
- Thay đổi có hiệu lực ngay ở request kế tiếp của tài khoản đó (không cần đăng nhập lại).
- Mọi thay đổi được ghi vào Nhật ký hệ thống ("Phân quyền tài khoản").

## Danh sách quyền
| Khóa | Chức năng |
|---|---|
| revenue.view / stock.view | Xem doanh thu / tồn kho trên Dashboard |
| product.manage / price.set / product.delete | Quản lý sản phẩm / đổi giá / xóa vĩnh viễn |
| taxonomy.manage | Danh mục · Thương hiệu · Tag |
| order.view / order.status | Xem & in đơn / đổi trạng thái, hủy đơn |
| post.manage / contact.manage / banner.manage | Bài viết / liên hệ & phản ánh / banner |
| report.view / stats.view | Báo cáo vận hành / thống kê & CSV |
| settings.manage / log.view | Cấu hình website & QR / nhật ký hệ thống |

## Lưu trữ & tương thích
- Dùng lại cột `Users.Permissions` (đã có sẵn, trước đây không dùng) → **không cần đổi database**.
- `""` (rỗng) = quyền mặc định của Staff như trước (sản phẩm, đơn hàng, bài viết, liên hệ, báo cáo, doanh thu, tồn kho),
  nên tài khoản Staff cũ giữ nguyên hành vi. `"-"` = tắt hết. Nút "Về quyền mặc định" đặt lại chuỗi rỗng.
- Đổi vai trò một tài khoản sẽ đưa quyền của nó về mặc định.

## File thay đổi
`Models/Models.cs` (Perm), `Helpers/Ext.cs` (Can), `Helpers/RequirePermAttribute.cs` (mới),
`Controllers/AccountController.cs` (claims), `Controllers/ManageController.cs`, `Controllers/Manage.Admin.cs`,
`Program.cs` (làm mới phiên khi quyền đổi), `Views/Manage/Permissions.cshtml` (mới), `Users.cshtml`, `Products.cshtml`, `ProductForm.cshtml`, `Views/Shared/_Layout.cshtml`.
