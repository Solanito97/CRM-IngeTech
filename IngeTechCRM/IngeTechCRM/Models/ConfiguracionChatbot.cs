using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ConfiguracionChatbot
    {
        [Key]
        [Column("ID_CONFIGURACION")]
        public int ID_CONFIGURACION { get; set; }

        [Column("TIPO")]
        [Required]
        [StringLength(50)]
        public string TIPO { get; set; }

        [Column("PALABRA_CLAVE")]
        [StringLength(100)]
        public string PALABRA_CLAVE { get; set; }

        [Column("RESPUESTA")]
        [Required]
        public string RESPUESTA { get; set; }

        [Column("ACTIVO")]
        public bool ACTIVO { get; set; } = true;
    }
}
