namespace Pkm.Domain.SharedKernel;

public static class DomainGuard
{
    public static void AgainstEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
            throw new DomainException($"{fieldName} is invalid.");
    }

    public static void AgainstNonPositive(int value, string fieldName)
    {
        if (value <= 0)
            throw new DomainException($"{fieldName} must be greater than zero.");
    }

    public static void AgainstNegative(long value, string fieldName)
    {
        if (value < 0)
            throw new DomainException($"{fieldName} is invalid.");
    }

    public static void AgainstNonPositive(long value, string fieldName)
    {
        if (value <= 0)
            throw new DomainException($"{fieldName} must be greater than zero.");
    }
}
