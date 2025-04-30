using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IngeTechCRM.Models
{
    // Modelo para CRM_USUARIO
    public class Usuario
    {
        [Key]
        public int IDENTIFICACION { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Nombre de Usuario")]
        public string NOMBRE_USUARIO { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string CORREO_ELECTRONICO { get; set; }

        [Required]
        [StringLength(128)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string CONTRASENA { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre Completo")]
        public string NOMBRE_COMPLETO { get; set; }

        [StringLength(20)]
        [Display(Name = "Teléfono")]
        public string TELEFONO { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Dirección Completa")]
        public string DIRECCION_COMPLETA { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime? FECHA_NACIMIENTO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha de Registro")]
        public DateTime FECHA_REGISTRO { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Último Acceso")]
        public DateTime ULTIMO_ACCESO { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Provincia")]
        public int ID_PROVINCIA { get; set; }

        [Required]
        [Display(Name = "Tipo de Usuario")]
        public int ID_TIPO_USUARIO { get; set; }

        // Propiedades de navegación
        [ForeignKey("ID_PROVINCIA")]
        public virtual Provincia Provincia { get; set; }

        [ForeignKey("ID_TIPO_USUARIO")]
        public virtual TipoUsuario TipoUsuario { get; set; }

        // Relaciones inversas (si son necesarias)
        public virtual ICollection<Producto> ProductosCreados { get; set; }
        public virtual ICollection<Comunicado> ComunicadosCreados { get; set; }
        public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; }
        public virtual ICollection<Carrito> Carritos { get; set; }
        public virtual ICollection<Pedido> Pedidos { get; set; }
        public virtual ICollection<EnvioComunicado> ComunicadosRecibidos { get; set; }
    }
}