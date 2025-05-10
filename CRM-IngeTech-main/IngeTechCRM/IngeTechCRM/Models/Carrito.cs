using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Carrito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_CARRITO { get; set; }

        [Required]
        [Display(Name = "Usuario")]
        public int ID_USUARIO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Creación")]
        public DateTime FECHA_CREACION { get; set; } = DateTime.Now;

        [Display(Name = "Activo")]
        public bool ACTIVO { get; set; } = true;

        // Propiedades de navegación
        [ForeignKey("ID_USUARIO")]
        public virtual Usuario Usuario { get; set; }

        // Relaciones inversas
        public virtual ICollection<ItemCarrito> Items { get; set; }
    }
}
