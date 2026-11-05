using QTKD.Api.Exceptions;

namespace QTKD.Api.Middleware;

public record ErrorResponse(int Status, string Code, string Message, IReadOnlyList<string> Errors);

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse(ex.StatusCode, ex.Code, ex.Message, ex.Errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi khi xử lý {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse(500, "INTERNAL_ERROR", "Đã xảy ra lỗi hệ thống.", Array.Empty<string>()));
        }
    }
}
