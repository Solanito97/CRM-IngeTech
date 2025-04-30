using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class EnvioComunicado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_ENVIO { get; set; }

        [Required]
        [Display(Name = "Comunicado")]
        public int ID_COMUNICADO { get; set; }

        [Required]
        [Display(Name = "Usuario Destinatario")]
        public int ID_USUARIO_DESTINATARIO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Envío")]
        public DateTime FECHA_ENVIO { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("ID_COMUNICADO")]
        public virtual Comunicado Comunicado { get; set; }

        [ForeignKey("ID_USUARIO_DESTINATARIO")]
        public virtual Usuario UsuarioDestinatario { get; set; }
    }
}
