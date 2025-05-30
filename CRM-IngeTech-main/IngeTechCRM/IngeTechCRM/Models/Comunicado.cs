using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Comunicado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_COMUNICADO { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150, ErrorMessage = "El título no puede exceder los 150 caracteres.")]
        [Display(Name = "Título")]
        public string TITULO { get; set; }

        [Required]
        [Display(Name = "Mensaje")]
        public string MENSAJE { get; set; }

        [Required(ErrorMessage = "La fecha de envío programado es obligatoria.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Creación")]
        public DateTime FECHA_CREACION { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Envío Programado")]
        public DateTime? FECHA_ENVIO_PROGRAMADO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Envío Real")]
        public DateTime? FECHA_ENVIO_REAL { get; set; }

        [Required]
        [Display(Name = "Usuario Creador")]
        public int ID_USUARIO_CREADOR { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_USUARIO_CREADOR")]
        public virtual Usuario UsuarioCreador { get; set; }

        // Relaciones inversas
        public virtual ICollection<SegmentoComunicado> Segmentos { get; set; }
        public virtual ICollection<EnvioComunicado> Envios { get; set; }
    }
}
