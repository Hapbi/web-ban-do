# Sprint 3 — Chữ chạy, ảnh sản phẩm, cấu hình chi tiết và đánh giá

## Đã thêm
- Dải thông báo chữ chạy phía trên nội dung website.
- Thay các ảnh sản phẩm đang dùng `picsum.photos` bằng ảnh theo nhóm sản phẩm từ Unsplash CDN; có ảnh SVG dự phòng nếu đường dẫn ảnh lỗi.
- Trang chi tiết sản phẩm có khu vực cấu hình kỹ thuật, đánh giá sao, bình luận và danh sách đánh giá.
- Form quản trị thêm/sửa sản phẩm có trường “Cấu hình chi tiết”; mỗi dòng được hiển thị thành một thông số riêng.
- Bảng `ProductReviews` và cột `Products.DetailedConfiguration` được tạo/nâng cấp tự động khi khởi động; không yêu cầu xóa `shop.db`.

## Chạy
1. Giải nén ZIP vào thư mục mới.
2. Mở `CuaHangGiaDung.csproj` bằng Visual Studio.
3. Chạy project bằng `Ctrl+F5`.
4. Mở một sản phẩm và cuộn xuống “Cấu hình chi tiết” / “Đánh giá & bình luận”.
5. Đăng nhập Admin, vào Quản lý sản phẩm → Sửa để cập nhật cấu hình chi tiết.

## Ghi chú
- Các thông số tự điền ban đầu là thông tin mẫu/tham khảo; quản trị viên cần kiểm tra và sửa theo đúng model thực tế trước khi đưa website vào sử dụng.
- Ảnh sản phẩm được tải từ Internet. Nếu một nguồn ảnh bị chặn hoặc URL không còn tồn tại, trang sẽ hiển thị ảnh dự phòng.
- Chưa chạy build trong môi trường này do không có .NET SDK cài sẵn.
