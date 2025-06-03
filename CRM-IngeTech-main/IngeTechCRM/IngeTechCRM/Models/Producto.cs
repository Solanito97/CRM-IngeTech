using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_PRODUCTO { get; set; }

        [Required(ErrorMessage = "El campo Código es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo Código no puede tener más de 50 caracteres.")]
        [Display(Name = "Código")]
        public string CODIGO { get; set; }

        [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "El campo Nombre no puede tener más de 150 caracteres.")]
        [Display(Name = "Nombre")]
        public string NOMBRE { get; set; }

        [Required(ErrorMessage = "El campo Descripción es obligatorio.")]
        [Display(Name = "Descripción")]
        public string DESCRIPCION { get; set; }

        [Required(ErrorMessage = "El campo Precio es obligatorio.")]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Precio")]
        public decimal PRECIO { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una categoría.")]
        [Display(Name = "Categoría")]
        public int ID_CATEGORIA { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una marca.")]
        [Display(Name = "Marca")]
        public int ID_MARCA { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un proveedor.")]
        [Display(Name = "Proveedor")]
        public int ID_PROVEEDOR { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Creación")]
        public DateTime FECHA_CREACION { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Actualización")]
        public DateTime? FECHA_ACTUALIZACION { get; set; }

        [Required(ErrorMessage = "El campo Usuario Creador es obligatorio.")]
        [Display(Name = "Usuario Creador")]
        public int ID_USUARIO_CREADOR { get; set; }

        [Display(Name = "Activo")]
        public bool ACTIVO { get; set; } = true;

        // Propiedades de navegación
        [ForeignKey("ID_CATEGORIA")]
        public virtual Categoria Categoria { get; set; }

        [ForeignKey("ID_MARCA")]
        public virtual Marca Marca { get; set; }

        [ForeignKey("ID_PROVEEDOR")]
        public virtual Proveedor Proveedor { get; set; }

        [ForeignKey("ID_USUARIO_CREADOR")]
        public virtual Usuario UsuarioCreador { get; set; }

        // Relaciones inversas
        public virtual ICollection<ImagenProducto> Imagenes { get; set; }
        public virtual ICollection<Inventario> Inventarios { get; set; }
        public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; }
        public virtual ICollection<ItemCarrito> ItemsCarrito { get; set; }
        public virtual ICollection<DetallePedido> DetallesPedido { get; set; }
    }
}
