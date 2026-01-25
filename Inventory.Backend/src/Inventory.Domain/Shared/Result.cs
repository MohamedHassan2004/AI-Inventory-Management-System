namespace Inventory.Domain.Shared;

public class Result
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public string Code { get; }

    protected Result(bool isSuccess, string message, string code)
    {
        IsSuccess = isSuccess;
        Message = message;
        Code = code;
    }

    public static Result Success(string message = "") => new(true, message, string.Empty);
    public static Result Failure(string code, string message) => new(false, message, code);

    public static Result<TValue> Success<TValue>(TValue value, string message = "") => new(value, true, message, string.Empty);
    public static Result<TValue> Failure<TValue>(string code, string message) => new(default!, false, message, code);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, string message, string code)
        : base(isSuccess, message, code)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");
}
