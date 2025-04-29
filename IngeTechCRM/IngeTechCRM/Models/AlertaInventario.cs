using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class AlertaInventario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_ALERTA { get; set; }

        [Required]
        [Display(Name = "Inventario")]
        public int ID_INVENTARIO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Alerta")]
        public DateTime FECHA_ALERTA { get; set; } = DateTime.Now;

        [Display(Name = "Procesada")]
        public bool PROCESADA { get; set; } = false;

        // Propiedades de navegación
        [ForeignKey("ID_INVENTARIO")]
        public virtual Inventario Inventario { get; set; }
    }
}
