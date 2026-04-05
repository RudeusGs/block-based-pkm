namespace server.Domain.Base
{
    /// <summary>
    /// DomainException: Ngoại lệ tùy chỉnh cho các lỗi liên quan đến logic nghiệp vụ trong domain.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException() { }
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}