namespace OrderManagementApplicationApi.DTOs
{
    public class PlaceOrderDto
    {
        public int CustomerId { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
    }

}

