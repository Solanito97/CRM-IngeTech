using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Pedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_PEDIDO { get; set; }

        [Required]
        [Display(Name = "Usuario")]
        public int ID_USUARIO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Pedido")]
        public DateTime FECHA_PEDIDO { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        [Display(Name = "Estado")]
        public string ESTADO { get; set; } = "PENDIENTE"; // PENDIENTE, PROCESANDO, ENVIADO, ENTREGADO, CANCELADO

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Total")]
        public decimal TOTAL { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Dirección Envío")]
        public string DIRECCION_ENVIO { get; set; }

        [Required]
        [Display(Name = "Provincia")]
        public int ID_PROVINCIA { get; set; }

        [StringLength(500)]
        [Display(Name = "Notas")]
        public string? NOTAS { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_USUARIO")]
        public virtual Usuario Usuario { get; set; }

        [ForeignKey("ID_PROVINCIA")]
        public virtual Provincia Provincia { get; set; }

        // Relaciones inversas
        public virtual ICollection<DetallePedido> Detalles { get; set; }
    }
}
