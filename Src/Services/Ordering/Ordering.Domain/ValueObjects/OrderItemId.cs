namespace Ordering.Domain.ValueObjects;
public record OrderItemId
{
    public Guid Value { get; }
    private OrderItemId(Guid value) => Value = value;
   
}