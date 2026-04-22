using Pkm.Domain.Common;

namespace Pkm.Domain.Blocks;
/// <summary>
/// BlockTypeCode: Đại diện cho loại block trong hệ thống, có thể là các loại đã biết như 
/// paragraph, heading, todo, quote, code, image, toggle, divider, bulleted_list, numbered_list hoặc các loại tùy chỉnh khác.
/// </summary>
public sealed class BlockTypeCode : IEquatable<BlockTypeCode>
{
    public string Value { get; }

    private BlockTypeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("BlockTypeCode không hợp lệ.");

        Value = value.Trim().ToLowerInvariant();
    }

    public static readonly BlockTypeCode Paragraph = new("paragraph");
    public static readonly BlockTypeCode Heading1 = new("heading_1");
    public static readonly BlockTypeCode Heading2 = new("heading_2");
    public static readonly BlockTypeCode Heading3 = new("heading_3");
    public static readonly BlockTypeCode Todo = new("todo");
    public static readonly BlockTypeCode Quote = new("quote");
    public static readonly BlockTypeCode Code = new("code");
    public static readonly BlockTypeCode Image = new("image");
    public static readonly BlockTypeCode Toggle = new("toggle");
    public static readonly BlockTypeCode Divider = new("divider");
    public static readonly BlockTypeCode BulletedList = new("bulleted_list");
    public static readonly BlockTypeCode NumberedList = new("numbered_list");

    private static readonly IReadOnlyDictionary<string, BlockTypeCode> KnownTypes =
        new Dictionary<string, BlockTypeCode>
        {
            [Paragraph.Value] = Paragraph,
            [Heading1.Value] = Heading1,
            [Heading2.Value] = Heading2,
            [Heading3.Value] = Heading3,
            [Todo.Value] = Todo,
            [Quote.Value] = Quote,
            [Code.Value] = Code,
            [Image.Value] = Image,
            [Toggle.Value] = Toggle,
            [Divider.Value] = Divider,
            [BulletedList.Value] = BulletedList,
            [NumberedList.Value] = NumberedList
        };

    public static BlockTypeCode From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("BlockTypeCode không hợp lệ.");

        var normalized = value.Trim().ToLowerInvariant();

        return KnownTypes.TryGetValue(normalized, out var known)
            ? known
            : new BlockTypeCode(normalized);
    }

    public bool RequiresProps() => this == Image;

    public bool IsTextLike() =>
        this == Paragraph
        || this == Heading1
        || this == Heading2
        || this == Heading3
        || this == Quote
        || this == Code
        || this == Todo;

    public bool Equals(BlockTypeCode? other)
        => other is not null && Value == other.Value;

    public override bool Equals(object? obj)
        => obj is BlockTypeCode other && Equals(other);

    public override int GetHashCode()
        => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(BlockTypeCode? left, BlockTypeCode? right)
        => EqualityComparer<BlockTypeCode>.Default.Equals(left, right);

    public static bool operator !=(BlockTypeCode? left, BlockTypeCode? right)
        => !(left == right);
}