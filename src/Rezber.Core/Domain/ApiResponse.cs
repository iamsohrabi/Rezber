using System.Net;
using System.Text.Json.Serialization;

namespace Rezber.Core.Domain;

/// <summary>
/// Represents the base response model for all API operations.
/// </summary>
public abstract class BaseResponse
{
    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

    /// <summary>
    /// Gets or sets the user-friendly message describing the response outcome.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the unique correlation ID for request tracing.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the response.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the version of the API.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApiVersion { get; set; }
}

/// <summary>
/// Represents a successful API response containing data of type <typeparamref name="TData"/>.
/// </summary>
/// <typeparam name="TData">The type of data contained in the response.</typeparam>
public sealed class ApiResponse<TData> : BaseResponse
{
    public TData? Data { get; set; }

    /// <summary>
    /// Gets or sets the count of items in the data collection (for paginated responses).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number (for paginated responses).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size (for paginated responses).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PageSize { get; set; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<TData> Success(TData data, string message = "Operation completed successfully")
    {
        return new ApiResponse<TData>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    /// <summary>
    /// Creates a successful paginated response.
    /// </summary>
    public static ApiResponse<TData> SuccessPaginated(TData data, int totalCount, int page, int pageSize, string message = "Operation completed successfully")
    {
        return new ApiResponse<TData>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            StatusCode = (int)HttpStatusCode.OK,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Creates a created response (201) for resource creation.
    /// </summary>
    public static ApiResponse<TData> Created(TData data, string message = "Resource created successfully")
    {
        return new ApiResponse<TData>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            StatusCode = (int)HttpStatusCode.Created
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static ApiResponse<TData> Error(string message, int statusCode = (int)HttpStatusCode.BadRequest)
    {
        return new ApiResponse<TData>
        {
            IsSuccess = false,
            Message = message,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Creates a not found response.
    /// </summary>
    public static ApiResponse<TData> NotFound(string message = "Resource not found")
    {
        return new ApiResponse<TData>
        {
            IsSuccess = false,
            Message = message,
            StatusCode = (int)HttpStatusCode.NotFound
        };
    }

    /// <summary>
    /// Creates an unauthorized response.
    /// </summary>
    public static ApiResponse<TData> Unauthorized(string message = "Authentication required")
    {
        return new ApiResponse<TData>
        {
            IsSuccess = false,
            Message = message,
            StatusCode = (int)HttpStatusCode.Unauthorized
        };
    }

    /// <summary>
    /// Creates a forbidden response.
    /// </summary>
    public static ApiResponse<TData> Forbidden(string message = "Access denied")
    {
        return new ApiResponse<TData>
        {
            IsSuccess = false,
            Message = message,
            StatusCode = (int)HttpStatusCode.Forbidden
        };
    }

    /// <summary>
    /// Implicitly converts a data object to a success ApiResponse.
    /// </summary>
    public static implicit operator ApiResponse<TData>(TData data)
    {
        return Success(data);
    }
}

/// <summary>
/// Represents an error API response containing validation errors.
/// </summary>
public sealed class ErrorResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the collection of error messages.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation errors (field-specific).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }

    /// <summary>
    /// Gets or sets the exception details (only in development environment).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Creates an error response with a single error.
    /// </summary>
    public static ErrorResponse Create(string error, string message = "Operation failed", int statusCode = (int)HttpStatusCode.BadRequest)
    {
        return new ErrorResponse
        {
            IsSuccess = false,
            Errors = new List<string> { error },
            Message = message,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Creates an error response with multiple errors.
    /// </summary>
    public static ErrorResponse CreateMany(List<string> errors, string message = "Operation failed", int statusCode = (int)HttpStatusCode.BadRequest)
    {
        return new ErrorResponse
        {
            IsSuccess = false,
            Errors = errors,
            Message = message,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Creates a validation error response.
    /// </summary>
    public static ErrorResponse Validation(Dictionary<string, List<string>> validationErrors, string message = "Validation failed")
    {
        return new ErrorResponse
        {
            IsSuccess = false,
            ValidationErrors = validationErrors,
            Message = message,
            StatusCode = (int)HttpStatusCode.BadRequest
        };
    }
}

/// <summary>
/// Provides factory methods for creating standardized API responses.
/// </summary>
public static class ResponseFactory
{
    /// <summary>
    /// Creates a successful API response with the specified data.
    /// </summary>
    public static ApiResponse<TData> Success<TData>(TData data, string message = "Operation completed successfully")
    {
        return ApiResponse<TData>.Success(data, message);
    }

    /// <summary>
    /// Creates a successful paginated response.
    /// </summary>
    public static ApiResponse<TData> SuccessPaginated<TData>(TData data, int totalCount, int page, int pageSize, string message = "Operation completed successfully")
    {
        return ApiResponse<TData>.SuccessPaginated(data, totalCount, page, pageSize, message);
    }

    /// <summary>
    /// Creates a created response (201) for resource creation.
    /// </summary>
    public static ApiResponse<TData> Created<TData>(TData data, string message = "Resource created successfully")
    {
        return ApiResponse<TData>.Created(data, message);
    }

    /// <summary>
    /// Creates a response with no content (204).
    /// </summary>
    public static ApiResponse<object> NoContent(string message = "Operation completed successfully")
    {
        return new ApiResponse<object>
        {
            IsSuccess = true,
            Data = null,
            Message = message,
            StatusCode = (int)HttpStatusCode.NoContent
        };
    }

    /// <summary>
    /// Creates an error response with a single error message.
    /// </summary>
    public static ErrorResponse Error(string error, string message = "Operation failed", int statusCode = (int)HttpStatusCode.BadRequest)
    {
        return ErrorResponse.Create(error, message, statusCode);
    }

    /// <summary>
    /// Creates an error response with multiple error messages.
    /// </summary>
    public static ErrorResponse ErrorMany(List<string> errors, string message = "Operation failed", int statusCode = (int)HttpStatusCode.BadRequest)
    {
        return ErrorResponse.CreateMany(errors, message, statusCode);
    }

    /// <summary>
    /// Creates a not found error response.
    /// </summary>
    public static ErrorResponse NotFound(string message = "Resource not found")
    {
        return ErrorResponse.Create(message, "Resource not found", (int)HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Creates a validation error response.
    /// </summary>
    public static ErrorResponse Validation(Dictionary<string, List<string>> validationErrors, string message = "Validation failed")
    {
        return ErrorResponse.Validation(validationErrors, message);
    }

    /// <summary>
    /// Creates an unauthorized error response.
    /// </summary>
    public static ErrorResponse Unauthorized(string message = "Authentication required")
    {
        return ErrorResponse.Create(message, "Authentication required", (int)HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Creates a forbidden error response.
    /// </summary>
    public static ErrorResponse Forbidden(string message = "Access denied")
    {
        return ErrorResponse.Create(message, "Access denied", (int)HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Creates an internal server error response.
    /// </summary>
    public static ErrorResponse InternalError(string message = "An internal error occurred", string? exceptionDetails = null)
    {
        return new ErrorResponse
        {
            IsSuccess = false,
            Errors = new List<string> { message },
            Message = "Internal server error",
            StatusCode = (int)HttpStatusCode.InternalServerError,
            ExceptionDetails = exceptionDetails
        };
    }
}
