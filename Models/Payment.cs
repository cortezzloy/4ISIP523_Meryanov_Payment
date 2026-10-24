using System;

namespace MeryanovPayment.Models
{
    public class Payment
    {
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public int UserID { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public int Num { get; set; }
        public decimal Price { get; set; }

        public decimal Total => Num * Price;
    }

    public class PaymentView
    {
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int UserID { get; set; }
        public string UserFIO { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public int Num { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}
