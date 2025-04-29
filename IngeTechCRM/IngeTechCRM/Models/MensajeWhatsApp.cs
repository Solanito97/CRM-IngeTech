using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class MensajeWhatsApp
    {
        [Key]
        [Column("ID_MENSAJE")]
        public int ID_MENSAJE { get; set; }

        [Column("ID_CONVERSACION")]
        public int ID_CONVERSACION { get; set; }

        [Column("TEXTO")]
        [Required]
        public string TEXTO { get; set; }

        [Column("FECHA_HORA")]
        public DateTime FECHA_HORA { get; set; }

        [Column("ES_ENTRANTE")]
        public bool ES_ENTRANTE { get; set; }

        [Column("ID_MENSAJE_EXTERNO")]
        [StringLength(100)]
        public string ID_MENSAJE_EXTERNO { get; set; }

        [ForeignKey("ID_CONVERSACION")]
        public virtual ConversacionWhatsApp Conversacion { get; set; }
    }
}
