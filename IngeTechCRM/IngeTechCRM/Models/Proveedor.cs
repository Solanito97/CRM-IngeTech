using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Proveedor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_PROVEEDOR { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Nombre")]
        public string NOMBRE { get; set; }

        [StringLength(100)]
        [Display(Name = "Contacto")]
        public string CONTACTO { get; set; }

        [StringLength(20)]
        [Display(Name = "Teléfono")]
        public string TELEFONO { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string CORREO_ELECTRONICO { get; set; }

        [StringLength(255)]
        [Display(Name = "Dirección Completa")]
        public string DIRECCION_COMPLETO { get; set; }

        // Relaciones inversas
        public virtual ICollection<Producto> Productos { get; set; }
    }
}
