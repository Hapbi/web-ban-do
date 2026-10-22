# Sửa liên kết chi tiết, ticker và chọn ảnh tạm

## Đã sửa
- Link "Xem thông tin chi tiết" dùng Url.Action đến Home/Detail với ID sản phẩm.
- Thanh chữ chạy không còn dừng khi rê chuột vào.
- Form thêm/sửa sản phẩm có mục chọn ảnh từ máy tính để xem trước, thay ảnh xem trước và xóa ảnh chọn.

## Lưu ý về ảnh
Bộ chọn file mới chỉ dùng `URL.createObjectURL` phía trình duyệt: không tải file lên server, không lưu vào database và ảnh tạm sẽ mất khi rời trang. Nó dùng để xem thử ảnh tại form. Muốn ảnh được lưu và hiện với mọi người sau khi lưu sản phẩm, phải dùng trường URL ảnh hoặc bổ sung chức năng upload server riêng.
