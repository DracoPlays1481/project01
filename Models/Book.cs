using System;
using System.Collections.Generic;

namespace EWDProject.Models;

public partial class Book
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string Genre { get; set; } = null!;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();
}
