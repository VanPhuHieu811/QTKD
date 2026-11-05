namespace QTKD.Api.Exceptions;

// Lỗi nghiệp vụ có mã HTTP và mã lỗi rõ ràng. Middleware sẽ đổi chúng thành response JSON thống nhất.
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public IReadOnlyList<string> Errors { get; }

    protected AppException(int statusCode, string code, string message, IEnumerable<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Errors = errors?.ToList() ?? new List<string>();
    }
}

// 404
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(404, "NOT_FOUND", message) { }
}

// 400: dữ liệu không hợp lệ hoặc không đạt điều kiện (ví dụ CTR-02)
public class ValidationFailedException : AppException
{
    public ValidationFailedException(string code, string message, IEnumerable<string> errors)
        : base(400, code, message, errors) { }
}

// 409: sai trạng thái, trùng dữ liệu (ví dụ CTR-01, CTR-03, gửi lặp)
public class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(409, code, message) { }
}
