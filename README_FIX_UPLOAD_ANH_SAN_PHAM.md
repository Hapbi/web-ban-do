# Sửa lỗi tải ảnh từ Dashboard

- Form thêm/sửa sản phẩm giờ có thể nhập URL ảnh hoặc tải file ảnh từ máy tính.
- File được lưu trong `wwwroot/uploads/products/`; database chỉ lưu đường dẫn ảnh (không lưu dữ liệu nhị phân).
- Sau khi bấm Lưu, ảnh đã tải lên sẽ hiển thị ở trang chủ, danh sách sản phẩm và trang chi tiết.
- Có thể chọn ảnh mới để thay ảnh hiện tại hoặc tick "Xóa ảnh hiện tại".
- Giới hạn: JPG/JPEG/PNG/WEBP/GIF, tối đa 5 MB.
- Nếu chỉ sửa tên/giá/mô tả mà không chọn ảnh mới và không tick xóa, ảnh cũ được giữ nguyên.
- File upload cũ do ứng dụng tạo sẽ được xóa khỏi thư mục khi thay/xóa ảnh.
