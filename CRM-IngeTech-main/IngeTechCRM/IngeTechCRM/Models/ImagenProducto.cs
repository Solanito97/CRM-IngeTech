using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ImagenProducto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_IMAGEN { get; set; }

        [Required]
        [Display(Name = "Producto")]
        public int ID_PRODUCTO { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Ruta Imagen")]
        public string RUTA_IMAGEN { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_PRODUCTO")]
        public virtual Producto Producto { get; set; }
    }
}
