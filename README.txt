CUA HANG GIA DUNG - ASP.NET Core MVC (.NET 8) + SQLite
Chay:  dotnet restore  roi  dotnet run   ->  mo http://localhost:5090
Tat web bang Ctrl+C truoc khi chay lai. Xoa shop.db de reset du lieu mau (shop.db cu van dung duoc, tu nang cap).

VAI TRO (Admin / Staff / Customer)
  Admin  admin@shop.vn / Admin@123   Staff  staff@shop.vn / Staff@123   Khach: tu dang ky
Admin, Staff dang nhap xong vao thang /Manage/Dashboard. Khach khong co dashboard.

SPRINT 2 - DEV 2 (Admin + Staff)
  US11 Staff: dang nhap quan tri + dashboard (doanh thu, ton kho)
  US12 Staff: san pham - xem / them / sua / an / khoi phuc   (gia san pham da co chi Admin doi; xoa chi Admin)
  US13 Staff: don hang - doi trang thai, huy (chon "Da huy", hoan kho), IN DON
  US14 Staff: bai viet (/Manage/Posts), xu ly lien he & phan anh (/Manage/Contacts). Khach gui tai /Home/Contact, doc bai tai /Home/Posts
  US15 Admin: danh muc / thuong hieu / tag (/Manage/Taxonomy) - gan vao san pham o form them/sua
  US16 Admin: tai khoan: tao Staff, doi vai tro, khoa/mo khoa, dat lai mat khau (/Manage/Users)
  US17 Admin: thong ke ton kho, ban chay, doanh thu theo ngay/danh muc + xuat CSV (/Manage/Stats)
  US18 Admin: banner trang chu (/Manage/Banners), cau hinh website/QR (/Manage/Settings), nhat ky he thong (/Manage/SystemLog)
  Staff: bao cao van hanh (/Manage/Report)
