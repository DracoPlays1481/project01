using System;
using System.Collections.Generic;

namespace EWDProject.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    public string OrderStatus { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();
}
