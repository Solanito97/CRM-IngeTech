using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class SegmentoComunicado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_SEGMENTO { get; set; }

        [Required]
        [Display(Name = "Comunicado")]
        public int ID_COMUNICADO { get; set; }

        [Display(Name = "Provincia")]
        public int? ID_PROVINCIA { get; set; }

        [Display(Name = "Tipo Usuario")]
        public int? ID_TIPO_USUARIO { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_COMUNICADO")]
        public virtual Comunicado Comunicado { get; set; }

        [ForeignKey("ID_PROVINCIA")]
        public virtual Provincia Provincia { get; set; }

        [ForeignKey("ID_TIPO_USUARIO")]
        public virtual TipoUsuario TipoUsuario { get; set; }
    }

}
