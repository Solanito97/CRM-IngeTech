using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Inventario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_INVENTARIO { get; set; }

        [Required(ErrorMessage = "El producto es obligatorio.")]
        [Display(Name = "Producto")]
        public int ID_PRODUCTO { get; set; }

        [Required(ErrorMessage = "El almacén es obligatorio.")]
        [Display(Name = "Almacén")]
        public int ID_ALMACEN { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0.")]
        [Display(Name = "Cantidad")]
        public int CANTIDAD { get; set; }

        [Required(ErrorMessage = "La cantidad mínima es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad mínima debe ser mayor a 0.")]
        [Display(Name = "Cantidad Mínima")]
        public int CANTIDAD_MINIMA { get; set; } = 5;

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Actualización")]
        public DateTime FECHA_ACTUALIZACION { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("ID_PRODUCTO")]
        public virtual Producto Producto { get; set; }

        [ForeignKey("ID_ALMACEN")]
        public virtual Almacen Almacen { get; set; }

        // Relaciones inversas
        public virtual ICollection<AlertaInventario> Alertas { get; set; }
    }
}
