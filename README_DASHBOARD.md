# Dashboard mới

- Giao diện dashboard đã được tích hợp vào `Views/Manage/Dashboard.cshtml`.
- CSS dashboard nằm trong `wwwroot/css/style.css`.
- Bản SVG tham chiếu thiết kế nằm ở `wwwroot/images/design/dashboard-reference.svg`.

Mở project bằng Visual Studio, chạy ứng dụng và truy cập `/Manage/Dashboard` bằng tài khoản quản trị.

Lưu ý: các con số trong file SVG chỉ là số liệu minh họa. Giao diện chạy thật lấy dữ liệu từ model của ứng dụng.


## Khắc phục CSS không cập nhật

Dashboard có stylesheet riêng `wwwroot/css/dashboard.css`; `_Layout.cshtml` tự thêm stylesheet này tại `/Manage/Dashboard` và dùng `asp-append-version` để tránh cache CSS cũ.
