using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Categoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_CATEGORIA { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string NOMBRE { get; set; }

        [Display(Name = "Descripción")]
        public string DESCRIPCION { get; set; }

        // Relaciones inversas
        public virtual ICollection<Producto> Productos { get; set; }
    }
}
