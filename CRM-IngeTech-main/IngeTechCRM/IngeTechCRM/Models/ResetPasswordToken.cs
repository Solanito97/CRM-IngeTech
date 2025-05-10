using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IngeTechCRM.Models
{
    public class ResetPasswordToken
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string TOKEN { get; set; }

        [Required]
        public int ID_USUARIO { get; set; }

        [Required]
        public DateTime FECHA_EXPIRACION { get; set; }

        public bool UTILIZADO { get; set; }

        // Relación con Usuario
        [ForeignKey("ID_USUARIO")]
        public virtual Usuario Usuario { get; set; }
    }
}