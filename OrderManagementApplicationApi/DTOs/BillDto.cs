namespace OrderManagementApplicationApi.DTOs
{
    public class BillDto
    {
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        public string CustomerName { get; set; }
        public decimal OrderTotal { get; set; }
    }

}
