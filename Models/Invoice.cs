using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cocktail.back.Models
{
    public class Invoice
    {
        [Key]
        public int IdInvoice { get; set; }

        [Required]
        public int IdOrder { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [ForeignKey("IdOrder")]
        public Order? Order { get; set; }
    }
}
