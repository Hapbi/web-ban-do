// Xác nhận trước khi xóa
document.querySelectorAll('form[data-confirm]').forEach(f => f.addEventListener('submit', e => {
    if (!confirm(f.dataset.confirm)) e.preventDefault();
}));

// Tự gửi form khi đổi lựa chọn (bộ lọc, trạng thái đơn, vai trò)
document.querySelectorAll('[data-autosubmit]').forEach(el => el.addEventListener('change', () => el.form.submit()));

// Nút sao chép nội dung chuyển khoản
document.querySelectorAll('[data-copy]').forEach(b => b.addEventListener('click', () => {
    navigator.clipboard?.writeText(document.querySelector(b.dataset.copy).textContent.trim());
    b.textContent = 'Đã chép ✓';
}));

// Xem trước ảnh QR khi chọn file
const qi = document.getElementById('qrInput');
if (qi) qi.addEventListener('change', () => {
    const f = qi.files[0];
    if (!f) return;
    const p = document.getElementById('qrPreview');
    p.src = URL.createObjectURL(f);
    p.style.display = 'block';
});

// Tự ẩn thông báo sau vài giây
setTimeout(() => document.querySelectorAll('.alert').forEach(a => a.style.display = 'none'), 4000);

// Ô nhập tiền: tự thêm dấu chấm theo hàng nghìn (1.000 / 10.000.000), gửi lên server dạng số thuần
const fmtMoney = v => v.replace(/\D/g, '').replace(/\B(?=(\d{3})+(?!\d))/g, '.');
document.querySelectorAll('input[data-money]').forEach(i => {
    i.value = fmtMoney(i.value);
    i.addEventListener('input', () => {
        const digitsBefore = i.value.slice(0, i.selectionStart).replace(/\D/g, '').length;
        i.value = fmtMoney(i.value);
        let n = 0, pos = 0;
        while (pos < i.value.length && n < digitsBefore) { if (/\d/.test(i.value[pos])) n++; pos++; }
        i.setSelectionRange(pos, pos);
    });
    if (i.form) i.form.addEventListener('submit', () => { i.value = i.value.replace(/\./g, ''); });
});


// Trang thanh toán: đổi ghi chú theo phương thức (QR / COD)
document.querySelectorAll('input[name=method]').forEach(r => r.addEventListener('change', () => {
    document.querySelectorAll('[data-note]').forEach(n => n.hidden = n.dataset.note !== r.value);
}));
