namespace Lendora.Application.Loans;

public sealed record ServiceResult<T>(
    bool Succeeded,
    T? Data,
    IReadOnlyCollection<string> Errors)
{
    public static ServiceResult<T> Success(T data) => new(true, data, []);

    public static ServiceResult<T> Failure(params string[] errors) => new(false, default, errors);
}
