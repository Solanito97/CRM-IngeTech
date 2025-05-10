using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ItemCarrito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_ITEM { get; set; }

        [Required]
        [Display(Name = "Carrito")]
        public int ID_CARRITO { get; set; }

        [Required]
        [Display(Name = "Producto")]
        public int ID_PRODUCTO { get; set; }

        [Required]
        [Display(Name = "Cantidad")]
        public int CANTIDAD { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Precio Unitario")]
        public decimal PRECIO_UNITARIO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Agregado")]
        public DateTime FECHA_AGREGADO { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("ID_CARRITO")]
        public virtual Carrito Carrito { get; set; }

        [ForeignKey("ID_PRODUCTO")]
        public virtual Producto Producto { get; set; }
    }

}
