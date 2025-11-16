namespace Ordering.Domain.ValueObjects;
public record ProductId
{
    public Guid Value { get; }
    private ProductId(Guid value) => Value = value;
}