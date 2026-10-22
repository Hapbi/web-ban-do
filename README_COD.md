# Thanh toán khi nhận hàng (COD)

- Khách chọn **Chuyển khoản QR** hoặc **Thanh toán khi nhận hàng (COD)** ở bước Thanh toán.
- Đơn COD vào thẳng trạng thái **Chờ xác nhận** (không cần quét QR). Nhân viên xác nhận -> **Đang giao** -> **Hoàn thành** (đã thu tiền).
- Đơn COD không dùng trạng thái "Chờ thanh toán" / "Đã thanh toán".
- Doanh thu: đơn chuyển khoản tính khi "Đã thanh toán / Đang giao / Hoàn thành"; đơn COD chỉ tính khi **Hoàn thành**.
- Database cũ tự thêm cột `Orders.PaymentMethod` (mặc định QR), không mất dữ liệu.
- Hủy đơn vẫn hoàn lại kho.
