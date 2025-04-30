using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class MovimientoInventario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_MOVIMIENTO { get; set; }

        [Required]
        [Display(Name = "Producto")]
        public int ID_PRODUCTO { get; set; }

        [Required]
        [Display(Name = "Almacén")]
        public int ID_ALMACEN { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Tipo Movimiento")]
        public string TIPO_MOVIMIENTO { get; set; } // ENTRADA, SALIDA, TRANSFERENCIA, AJUSTE

        [Required]
        [Display(Name = "Cantidad")]
        public int CANTIDAD { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Movimiento")]
        public DateTime FECHA_MOVIMIENTO { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Usuario")]
        public int ID_USUARIO { get; set; }

        [StringLength(255)]
        [Display(Name = "Observación")]
        public string OBSERVACION { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_PRODUCTO")]
        public virtual Producto Producto { get; set; }

        [ForeignKey("ID_ALMACEN")]
        public virtual Almacen Almacen { get; set; }

        [ForeignKey("ID_USUARIO")]
        public virtual Usuario Usuario { get; set; }
    }
}
