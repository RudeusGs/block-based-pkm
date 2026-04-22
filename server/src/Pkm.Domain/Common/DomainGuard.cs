namespace Pkm.Domain.Common;

public static class DomainGuard
{
    public static void AgainstEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
            throw new DomainException($"{fieldName} không hợp lệ.");
    }

    public static void AgainstNonPositive(int value, string fieldName)
    {
        if (value <= 0)
            throw new DomainException($"{fieldName} phải lớn hơn 0.");
    }

    public static void AgainstNegative(long value, string fieldName)
    {
        if (value < 0)
            throw new DomainException($"{fieldName} không hợp lệ.");
    }

    public static void AgainstNonPositive(long value, string fieldName)
    {
        if (value <= 0)
            throw new DomainException($"{fieldName} phải lớn hơn 0.");
    }
}