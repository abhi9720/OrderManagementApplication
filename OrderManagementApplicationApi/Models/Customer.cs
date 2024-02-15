using System.ComponentModel.DataAnnotations;

namespace OrderManagementApplicationApi.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Email { get; set; }

        public ICollection<Order> Orders { get; set; }
    }

}
