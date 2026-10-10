namespace ECommerce.Core.Models;

/// <summary>Simple operation outcome with an optional error message for UI display.</summary>
public class Result
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public static Result Ok() => new() { Success = true };
    public static Result Fail(string error) => new() { Success = false, Error = error };
}

public class Result<T> : Result
{
    public T? Value { get; init; }
    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
    public static new Result<T> Fail(string error) => new() { Success = false, Error = error };
}

public class CouponValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public Entities.Coupon? Coupon { get; init; }
    public decimal DiscountAmount { get; init; }
}
