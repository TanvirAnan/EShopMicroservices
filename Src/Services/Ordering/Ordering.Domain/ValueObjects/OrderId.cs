
namespace Ordering.Domain.ValueObjects;
public record OrderId
{
    public Guid Value { get; }
    private OrderId(Guid value) => Value = value;
  
}
