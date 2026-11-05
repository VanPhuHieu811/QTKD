# Tài liệu API (tuần 06)

Mọi lỗi nghiệp vụ trả về cùng một định dạng:

```json
{
  "status": 400,
  "code": "CTR-02",
  "message": "Hợp đồng chưa đủ điều kiện gửi trình duyệt.",
  "errors": ["..."]
}
```

| HTTP | Ý nghĩa | Mã `code` thường gặp |
|---|---|---|
| 400 | Dữ liệu không hợp lệ hoặc không đạt điều kiện | `VALIDATION_FAILED`, `CTR-02` |
| 404 | Không tìm thấy đối tượng | `NOT_FOUND` |
| 409 | Sai trạng thái, trùng dữ liệu | `CTR-01`, `CTR-03`, `ALREADY_SUBMITTED`, `INVALID_STATE`, `INVALID_TRANSITION`, `CONTRACT_NO_EXISTS`, `SERVICE_CODE_EXISTS` |
| 500 | Lỗi hệ thống | `INTERNAL_ERROR` |

## 1. Danh sách API

| Nhóm | Method và đường dẫn | Mô tả | Thành công |
|---|---|---|---|
| Customer | `GET /api/customers` | Danh sách khách hàng | 200 |
| Customer | `GET /api/customers/{id}` | Chi tiết khách hàng | 200 |
| Customer | `POST /api/customers` | Tạo khách hàng | 201 |
| Customer | `PUT /api/customers/{id}` | Cập nhật khách hàng | 200 |
| Contract | `GET /api/contracts` | Danh sách hợp đồng | 200 |
| Contract | `GET /api/contracts/{id}` | Chi tiết hợp đồng | 200 |
| Contract | `POST /api/contracts` | Tạo hợp đồng (trạng thái `Draft`) | 201 |
| Contract | `PUT /api/contracts/{id}` | Cập nhật hợp đồng (CTR-01) | 200 |
| Contract | `POST /api/contracts/{id}/attachments` | Thêm tệp đính kèm (CTR-01) | 200 |
| Contract | `POST /api/contracts/{id}/submit` | Gửi trình duyệt (US03, CTR-02, CTR-03) | 200 |
| Pricing | `GET /api/pricelists` | Danh sách bảng giá kèm các mục giá | 200 |
| Pricing | `GET /api/pricelists/{id}` | Chi tiết bảng giá | 200 |
| Pricing | `POST /api/pricelists` | Tạo bảng giá | 201 |
| Pricing | `GET /api/pricelists/{id}/items` | Xem các mục giá | 200 |
| Pricing | `POST /api/pricelists/{id}/items` | Thêm mục giá | 201 |

## 2. Request và response mẫu

### Tạo khách hàng

`POST /api/customers`
```json
{ "name": "Công ty Bình Minh" }
```
201:
```json
{ "id": 3, "name": "Công ty Bình Minh", "status": "Active" }
```

Thiếu tên (`{}`) trả 400:
```json
{
  "status": 400,
  "code": "VALIDATION_FAILED",
  "message": "Dữ liệu khách hàng không hợp lệ.",
  "errors": ["Name là bắt buộc."]
}
```

### Tạo hợp đồng

`POST /api/contracts`
```json
{
  "contractNo": "HD-2026-019",
  "customerId": 3,
  "startDate": "2026-10-01",
  "endDate": "2027-09-30",
  "serviceGroup": "Dịch vụ cảng"
}
```
201:
```json
{
  "id": 2,
  "contractNo": "HD-2026-019",
  "customerId": 3,
  "customerName": "Công ty Bình Minh",
  "startDate": "2026-10-01",
  "endDate": "2027-09-30",
  "status": "Draft",
  "serviceGroup": "Dịch vụ cảng",
  "content": null,
  "attachments": []
}
```

### Cập nhật hợp đồng (bổ sung nội dung)

`PUT /api/contracts/2`
```json
{
  "customerId": 3,
  "startDate": "2026-10-01",
  "endDate": "2027-09-30",
  "serviceGroup": "Dịch vụ cảng",
  "content": "Điều khoản cung cấp dịch vụ xếp dỡ container"
}
```
200: trả về hợp đồng đã cập nhật. Nếu hợp đồng đang `PendingApproval`, trả 409:
```json
{
  "status": 409,
  "code": "CTR-01",
  "message": "Không thể chỉnh sửa hợp đồng ở trạng thái PendingApproval. Chỉ được sửa khi hợp đồng ở trạng thái Draft hoặc RevisionRequested.",
  "errors": []
}
```

### Thêm tệp đính kèm

`POST /api/contracts/2/attachments`
```json
{ "fileName": "hop-dong-HD-2026-019.pdf" }
```

### Gửi trình duyệt (US03)

`POST /api/contracts/2/submit` (không cần body)

Thành công, 200:
```json
{ "contractId": 2, "status": "PendingApproval", "approvalId": 1 }
```

Thiếu điều kiện CTR-02, 400 (hợp đồng và database không đổi):
```json
{
  "status": 400,
  "code": "CTR-02",
  "message": "Hợp đồng chưa đủ điều kiện gửi trình duyệt.",
  "errors": [
    "Thiếu nội dung bắt buộc (ServiceGroup và Content).",
    "Thiếu tệp đính kèm bắt buộc (cần ít nhất 1 tệp)."
  ]
}
```

Gửi lần hai, 409 (không tạo thêm `ApprovalRequest`):
```json
{
  "status": 409,
  "code": "ALREADY_SUBMITTED",
  "message": "Hợp đồng đã được gửi trình duyệt (PendingApproval), không tạo thêm yêu cầu mới.",
  "errors": []
}
```

Hợp đồng không ở `Draft` (ví dụ `Approved`), 409:
```json
{
  "status": 409,
  "code": "INVALID_STATE",
  "message": "Chỉ gửi trình duyệt được hợp đồng ở trạng thái Draft (hiện tại: Approved).",
  "errors": []
}
```

### Bảng giá

`POST /api/pricelists`
```json
{ "name": "Bảng giá dịch vụ kho 2026", "effectiveDate": "2026-11-01" }
```

`POST /api/pricelists/2/items`
```json
{ "serviceCode": "KHO-01", "unitPrice": 250000 }
```
201:
```json
{ "serviceCode": "KHO-01", "unitPrice": 250000 }
```
Đơn giá bằng hoặc nhỏ hơn 0 trả 400 với lỗi `UnitPrice phải lớn hơn 0.`

## 3. Giới hạn của phiên bản hiện tại

- Chưa có đăng nhập và phân quyền (AC01, giả định G5).
- Approval được mô phỏng bằng một bản ghi `ApprovalRequest` có trạng thái `Pending`. Chưa có API duyệt, từ chối hay yêu cầu chỉnh sửa (giả định G6).
- Gửi lặp được chặn bằng kiểm tra trạng thái và unique index trên `ApprovalRequest` (G7). Chưa dùng `Idempotency-Key`.
- Hợp đồng được lưu ở `Draft` kể cả khi ngày hiệu lực chưa hợp lệ hoặc chưa có nội dung. Các điều kiện này chỉ được kiểm tra khi gửi (CTR-02).

## 4. Bảng đối chiếu US03, AC và CTR với mã xử lý

| Yêu cầu | Nội dung | Đoạn xử lý | Cách kiểm tra |
|---|---|---|---|
| AC01 | Không có quyền thì từ chối | Chưa cài đặt (giả định G5) | Ghi rõ trong báo cáo |
| AC02 | Sai trạng thái thì từ chối | `ContractService.SubmitAsync` (hai nhánh kiểm tra `Status`) | Submit hợp đồng `Approved` hoặc `PendingApproval`: 409 |
| AC03 | Không đạt CTR-02 thì không tạo quy trình và trả danh sách điều kiện chưa đạt | `ContractRules.GetSubmitErrors`, ném `ValidationFailedException("CTR-02")` trong `SubmitAsync` | Submit hợp đồng thiếu nội dung/tệp: 400, DB không đổi |
| AC04 | Đủ điều kiện thì tạo yêu cầu phê duyệt và chuyển `PendingApproval` | `ContractService.SubmitAsync` (tạo `ApprovalRequest`, `ContractHistory`, đổi `Status`) | Submit hợp đồng hợp lệ: 200, bảng `ApprovalRequests` có 1 dòng |
| AC05 | Gửi lặp hoặc đồng thời không tạo nhiều phiên | Nhánh `ALREADY_SUBMITTED` trong `SubmitAsync` và unique index lọc trên `ApprovalRequests` (`QtkdDbContext`) | Submit lần hai: 409, không có dòng mới |
| CTR-01 | Chỉ sửa khi `Draft` hoặc `RevisionRequested` | `ContractRules.EnsureEditable`, gọi trong `UpdateAsync` và `AddAttachmentAsync` | PUT hợp đồng `PendingApproval`: 409 `CTR-01` |
| CTR-02 | Chỉ gửi khi khách hàng, thời gian hiệu lực, nội dung/tệp hợp lệ | `ContractRules.GetSubmitErrors` | Thiếu một điều kiện: 400, trạng thái vẫn `Draft` |
| CTR-03 | Không chuyển thẳng `Draft` sang `Approved` | `ContractRules.EnsureCanTransition` (gọi trong `SubmitAsync`, chỉ cho `Draft` sang `PendingApproval`) | Unit test tuần 07 (TC07); Submit thành công chỉ ra `PendingApproval` |

Ghi chú: khi nộp, bổ sung số dòng của từng đoạn mã trong repository vào cột "Đoạn xử lý".
