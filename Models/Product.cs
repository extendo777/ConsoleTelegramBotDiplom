using System;
using System.Collections.Generic;

namespace ConsoleTelegramBot.Models
{
    public partial class Product
    {
        public Product()
        {
            Carts = new HashSet<Cart>();
            OrderProducts = new HashSet<OrderProduct>();
        }

        public int Id { get; set; }
        public string? Title { get; set; }
        public double? Price { get; set; }
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public byte[]? Image { get; set; }

        public virtual Brand? Brand { get; set; }
        public virtual Category? Category { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<OrderProduct> OrderProducts { get; set; }
    }
}
