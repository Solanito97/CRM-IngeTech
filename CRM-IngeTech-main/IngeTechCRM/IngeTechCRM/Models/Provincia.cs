using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Provincia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_PROVINCIA { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Nombre")]
        public string NOMBRE { get; set; }

        // Relaciones inversas
        public virtual ICollection<Usuario> Usuarios { get; set; }
        public virtual ICollection<Almacen> Almacenes { get; set; }
        public virtual ICollection<Pedido> Pedidos { get; set; }
        public virtual ICollection<SegmentoComunicado> SegmentosComunicado { get; set; }
    }
}
