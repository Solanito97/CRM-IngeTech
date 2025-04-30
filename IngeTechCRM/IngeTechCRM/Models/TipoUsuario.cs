using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class TipoUsuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_TIPO_USUARIO { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Descripción")]
        public string DESCRIPCION { get; set; }

        // Relaciones inversas
        public virtual ICollection<Usuario> Usuarios { get; set; }
        public virtual ICollection<SegmentoComunicado> SegmentosComunicado { get; set; }
    }
}
