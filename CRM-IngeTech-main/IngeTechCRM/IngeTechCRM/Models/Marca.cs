using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Marca
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_MARCA { get; set; }

        [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string NOMBRE { get; set; }

        [Required(ErrorMessage = "El campo Descripción es obligatorio.")]
        [Display(Name = "Descripción")]
        public string DESCRIPCION { get; set; }

        // Relaciones inversas
        public virtual ICollection<Producto> Productos { get; set; }
    }
}
