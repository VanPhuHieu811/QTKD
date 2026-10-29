# Ghi chú thiết kế QTKD (Phase 2, tuần 05-07)

Nguồn: `Tuan_02_QTKD_Req.pdf` (Phase 1: US03, AC01-AC05, mục 5.1 Trạng thái hợp đồng và CTR-01..07) và `TH_Phase2_QTKD.pdf`.

## 1. Phạm vi Phase 2

- **Làm:** Customer, Contract, PriceList/PriceItem và thao tác gửi hợp đồng trình duyệt (US03).
- **Chưa làm:** quy trình Approval đầy đủ, Payment, giao diện web, ký điện tử, phụ lục, đăng nhập/phân quyền.
- **Quy tắc áp dụng:** CTR-01, CTR-02, CTR-03. CTR-04..07 chỉ ghi nhận ở mục 3.

## 2. Trạng thái hợp đồng

Sơ đồ nguồn (mục 5.1) có 9 trạng thái. Trong code dùng enum tiếng Anh, lưu dạng chuỗi trong DB (`HasConversion<string>()`).

| Sơ đồ nguồn | Trong code | Ghi chú |
|---|---|---|
| Draft (Soạn thảo / Bản nháp) | `Draft` | Được sửa (CTR-01); trạng thái ban đầu |
| Submitted (Đã gửi duyệt) và Under Review (Đang rà soát) | `PendingApproval` | Kết quả của Submit. Gộp hai trạng thái vì Phase 2 chưa có Approval đầy đủ (xem G6) |
| Revision Requested (Yêu cầu chỉnh sửa) | `RevisionRequested` | Được sửa (CTR-01); khai báo trong enum |
| Approved (Đã phê duyệt) | `Approved` | Chỉ khai báo. Không được nhảy thẳng từ Draft (CTR-03) |
| Rejected (Từ chối) | `Rejected` | Chỉ khai báo |
| Active (Có hiệu lực) | `Active` | Chỉ khai báo |
| Expired (Hết hiệu lực) | `Expired` | Chỉ khai báo |
| Cancelled (Hủy) | `Cancelled` | Chỉ khai báo |

**Chuyển trạng thái được cài đặt trong Phase 2:**

| Từ | Đến | Cách thực hiện |
|---|---|---|
| (mới tạo) | `Draft` | `POST /api/contracts` |
| `Draft` | `PendingApproval` | `POST /api/contracts/{id}/submit` khi đạt CTR-02 |

Mọi chuyển trạng thái khác (ví dụ `Draft` → `Approved`) phải bị từ chối bởi một phương thức kiểm tra chuyển trạng thái dùng chung (phục vụ CTR-03 và TC07).

## 3. Quy tắc nghiệp vụ

| Mã | Nội dung (nguồn mục 5.1) | Trong Phase 2 |
|---|---|---|
| CTR-01 | Chỉ được chỉnh sửa hợp đồng ở Draft hoặc Revision Requested | **Cài đặt.** Sửa ở trạng thái khác trả 409, dữ liệu không đổi |
| CTR-02 | Chỉ Submit khi có khách hàng hợp lệ, thời gian hiệu lực hợp lệ và có ít nhất một tệp/hạng mục nội dung bắt buộc | **Cài đặt.** Thiếu một điều kiện trả 400 kèm danh sách lỗi, hợp đồng không đổi (giả định cụ thể ở mục 5) |
| CTR-03 | Không chuyển trực tiếp từ Draft sang Approved; phải qua quy trình phê duyệt | **Cài đặt.** Submit chỉ đưa hợp đồng sang `PendingApproval` |
| CTR-04 | Hợp đồng Rejected không tự động sửa và submit lại; cần bản chỉnh sửa hoặc chuyển về Revision | Ngoài phạm vi. Chưa có luồng từ chối |
| CTR-05 | Hợp đồng Approved chỉ chuyển Active khi đến ngày hiệu lực | Ngoài phạm vi |
| CTR-06 | Hợp đồng Active không được xóa; nếu không dùng nữa phải chuyển Cancelled hoặc Expired | Ngoài phạm vi. Không làm endpoint xóa Contract |
| CTR-07 | Hợp đồng Active/Approved thay đổi điều khoản quan trọng phải tạo phụ lục | Ngoài phạm vi (phụ lục chưa thuộc Phase 2) |

## 4. Mô hình dữ liệu

| Entity | Trường | Ràng buộc / ghi chú |
|---|---|---|
| **Customer** | Id, Name, Status | Name bắt buộc. Status: `Active` / `Inactive` |
| **Contract** | Id, ContractNo, CustomerId, StartDate, EndDate, Status, ServiceGroup, Content | ContractNo unique. CustomerId là FK tới Customer. ServiceGroup và Content phục vụ CTR-02 (giả định G1) |
| **ContractAttachment** | Id, ContractId, FileName | FK tới Contract. Chỉ lưu tên tệp, chưa upload thật (G2) |
| **PriceList** | Id, Name, EffectiveDate | |
| **PriceItem** | PriceListId, ServiceCode, UnitPrice | Khóa chính ghép (PriceListId, ServiceCode). UnitPrice > 0 |
| **ApprovalRequest** | Id, ContractId, SubmittedAt, Status | Mô phỏng Approval (G6). Unique index trên ContractId với điều kiện Status = Pending (G7) |
| **ContractHistory** | Id, ContractId, FromStatus, ToStatus, ChangedAt | Ghi lịch sử xử lý theo đặc tả UC-CON-03 |

Quan hệ: Customer 1-n Contract; Contract 1-n ContractAttachment; Contract 1-n ApprovalRequest; Contract 1-n ContractHistory; PriceList 1-n PriceItem.

**Dữ liệu mẫu (tuần 05):** 2 Customer (có Công ty Minh Phát), 1 Contract `HD-2026-018` ở `Draft` (khách hàng Công ty Minh Phát, hiệu lực 01/10/2026 - 30/09/2027, nhóm dịch vụ "Dịch vụ cảng"), 1 PriceList có 2 PriceItem.

## 5. Giả định và phần cần xác nhận

Đặc tả nguồn còn các câu hỏi mở (Q1, Q2). Các mục dưới đây là giả định cho bản demo, **không phải yêu cầu chính thức**.

| # | Vấn đề | Giả định | Cần xác nhận |
|---|---|---|---|
| G1 | "Nội dung bắt buộc" (Q2) | `ServiceGroup` và `Content` không rỗng | Có |
| G2 | "Tệp bắt buộc" (Q2) | Có ít nhất 1 `ContractAttachment`. CTR-02 gốc chỉ yêu cầu "ít nhất một tệp/hạng mục", bản demo yêu cầu cả G1 và G2 cho chặt chẽ | Có |
| G3 | "Thời gian hiệu lực hợp lệ" | `StartDate` và `EndDate` có giá trị và `StartDate < EndDate` | Có |
| G4 | "Khách hàng hợp lệ" | Customer tồn tại và `Status = Active` | Có |
| G5 | Phân quyền (AC01, 403; Q1) | Chưa làm đăng nhập/phân quyền; mọi request được coi là của Nhân viên Kinh doanh | Có (Q1) |
| G6 | Mô phỏng Approval | Submit thành công thì tạo 1 bản ghi `ApprovalRequest` (Pending), đổi trạng thái hợp đồng sang `PendingApproval`, ghi `ContractHistory`. Không có bước rà soát/phê duyệt thật | Không |
| G7 | Gửi lặp (AC05) | Hợp đồng đã `PendingApproval` thì trả 409 và không tạo thêm `ApprovalRequest`. Unique index lọc theo Status = Pending chặn trùng ở mức DB | Không |
| G8 | Gộp Submitted và Under Review | Dùng chung `PendingApproval` | Có |
| G9 | Submit và lưu | Đổi trạng thái, tạo `ApprovalRequest` và ghi `ContractHistory` trong một transaction. Lỗi ở bước nào thì rollback, hợp đồng không đổi (A5 của UC-CON-03) | Không |

**Giới hạn của phiên bản hiện tại:** chưa có người duyệt, chưa có chuyển trạng thái sau `PendingApproval`, chưa kiểm soát đồng thời bằng token (chỉ dựa vào kiểm tra trạng thái và unique index).

## 6. API Submit (US03)

`POST /api/contracts/{id}/submit`

| Kết quả | HTTP | Nội dung trả về |
|---|---|---|
| Thành công | 200 | `{ contractId, status: "PendingApproval", approvalId }` |
| Không đạt CTR-02 | 400 | Mã lỗi và danh sách điều kiện không đạt |
| Hợp đồng không tồn tại | 404 | Thông báo lỗi |
| Sai trạng thái hoặc gửi lặp | 409 | Mã lỗi, trạng thái hiện tại |

Thứ tự kiểm tra trong service: tìm Contract (404) → kiểm tra trạng thái là `Draft` (409) → kiểm tra CTR-02 (400) → transaction: tạo `ApprovalRequest`, đổi trạng thái, ghi lịch sử.

## 7. Thay đổi so với Phase 1

| Nội dung | Phase 1 | Phase 2 | Lý do |
|---|---|---|---|
| Chống gửi trùng | Header `Idempotency-Key` | Kiểm tra trạng thái + unique index | Phase 2 chưa có Approval đầy đủ; cách này đủ chặn gửi trùng và đơn giản hơn |
| Phân quyền (AC01, 403) | Có kiểm tra quyền, Bearer token | Chưa cài đặt | Ngoài phạm vi Phase 2; Q1 chưa được xác nhận |
| Approval Service | Dịch vụ riêng tạo approval instance | Bảng `ApprovalRequest` đơn giản trong cùng API | Chưa yêu cầu quy trình Approval đầy đủ |
| Audit/Event | Thành phần Audit / Event riêng | Bảng `ContractHistory` | Đủ để "ghi nhận lịch sử xử lý" |
| Trạng thái Submitted / Under Review | Hai trạng thái | Một trạng thái `PendingApproval` | Chưa có bước rà soát (G8) |

## 8. Cấu trúc solution

```
QTKD/
├── src/QTKD.Api/
│   ├── Controllers/   (API: nhận request, trả response)
│   ├── Services/      (nghiệp vụ: CTR-01..03, Submit)
│   ├── Data/          (QtkdDbContext, migration, seed)
│   └── Entities/      (Customer, Contract, ...)
├── tests/             (xUnit, tuần 07)
├── docs/              (design-notes.md, tài liệu API, minh chứng)
└── README.md
```