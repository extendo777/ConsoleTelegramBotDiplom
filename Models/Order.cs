using System;
using System.Collections.Generic;

namespace ConsoleTelegramBot.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderProducts = new HashSet<OrderProduct>();
        }

        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Status { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? OrderTime { get; set; }
        public double? DeliveryCost { get; set; }
        public string? DeliveryAddress { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<OrderProduct> OrderProducts { get; set; }
    }
}
