using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class DetallePedido
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_DETALLE { get; set; }

        [Required]
        [Display(Name = "Pedido")]
        public int ID_PEDIDO { get; set; }

        [Required]
        [Display(Name = "Producto")]
        public int ID_PRODUCTO { get; set; }

        [Required]
        [Display(Name = "Cantidad")]
        public int CANTIDAD { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Precio Unitario")]
        public decimal PRECIO_UNITARIO { get; set; }

        [Required]
        [Display(Name = "Almacén Origen")]
        public int ID_ALMACEN_ORIGEN { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_PEDIDO")]
        public virtual Pedido Pedido { get; set; }

        [ForeignKey("ID_PRODUCTO")]
        public virtual Producto Producto { get; set; }

        [ForeignKey("ID_ALMACEN_ORIGEN")]
        public virtual Almacen AlmacenOrigen { get; set; }
    }
}
