using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Almacen
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_ALMACEN { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string NOMBRE { get; set; }

        [Required]
        [Display(Name = "Provincia")]
        public int ID_PROVINCIA { get; set; }

        [StringLength(255)]
        [Display(Name = "Dirección Completa")]
        public string DIRECCION_COMPLETA { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_PROVINCIA")]
        public virtual Provincia Provincia { get; set; }

        // Relaciones inversas
        public virtual ICollection<Inventario> Inventarios { get; set; }
        public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; }
        public virtual ICollection<DetallePedido> DetallesPedido { get; set; }
    }
}
