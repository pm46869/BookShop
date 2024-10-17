using Microsoft.AspNetCore.Identity;

namespace BookShop.Models;

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string UserId { get; set; }
    public virtual IdentityUser User { get; set; }
    public string PaymentMethod { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Waiting;
    public string Address { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; }
}

public enum OrderStatus
{
    Waiting,
    Sent,
    Delivered
}
