using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class MovimientoInventario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_MOVIMIENTO { get; set; }

        [Required(ErrorMessage = "El Producto es obligatorio.")]
        [Display(Name = "Producto")]
        public int ID_PRODUCTO { get; set; }

        [Required(ErrorMessage = "El Almacén es obligatorio.")]
        [Display(Name = "Almacén")]
        public int ID_ALMACEN { get; set; }

        [Required(ErrorMessage = "El Tipo de Movimiento es obligatorio.")]
        [StringLength(20)]
        [Display(Name = "Tipo Movimiento")]
        public string TIPO_MOVIMIENTO { get; set; } // ENTRADA, SALIDA, TRANSFERENCIA, AJUSTE

        [Required(ErrorMessage = "La Cantidad es obligatoria.")]
        [Display(Name = "Cantidad")]
        public int CANTIDAD { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Movimiento")]
        public DateTime FECHA_MOVIMIENTO { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Usuario")]
        public int ID_USUARIO { get; set; }

        [Required(ErrorMessage = "La Observación es obligatoria.")]
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
