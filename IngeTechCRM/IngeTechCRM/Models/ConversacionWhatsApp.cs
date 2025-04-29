using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ConversacionWhatsApp
    {
        [Key]
        [Column("ID_CONVERSACION")]
        public int ID_CONVERSACION { get; set; }

        [Column("NUMERO_TELEFONO")]
        [Required]
        [StringLength(15)]
        public string NUMERO_TELEFONO { get; set; }

        [Column("NOMBRE_CONTACTO")]
        [StringLength(100)]
        public string NOMBRE_CONTACTO { get; set; }

        [Column("ID_USUARIO")]
        public int? ID_USUARIO { get; set; }

        [Column("FECHA_CREACION")]
        public DateTime FECHA_CREACION { get; set; }

        [Column("ULTIMA_ACTUALIZACION")]
        public DateTime ULTIMA_ACTUALIZACION { get; set; }

        [Column("ESTADO")]
        [StringLength(20)]
        public string ESTADO { get; set; } = "Activo";

        [Column("NOTAS")]
        [StringLength(500)]
        public string NOTAS { get; set; }

        public virtual ICollection<MensajeWhatsApp> Mensajes { get; set; }

        [ForeignKey("ID_USUARIO")]
        public virtual Usuario Usuario { get; set; }
    }
}
