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

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder los 50 caracteres")]
        [Display(Name = "Nombre de Usuario")]
        public string NOMBRE_USUARIO { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [StringLength(100, ErrorMessage = "El correo electrónico no puede exceder los 100 caracteres")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido")]
        [Display(Name = "Correo Electrónico")]
        public string CORREO_ELECTRONICO { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 128 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string CONTRASENA { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\u00f1\u00d1\s]+$",
        ErrorMessage = "El nombre completo solo puede contener letras, espacios y tildes")]
        [StringLength(100, ErrorMessage = "El nombre completo no puede exceder los 100 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NOMBRE_COMPLETO { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres")]
        [Display(Name = "Teléfono")]
        [RegularExpression(@"^[\d\-\+\(\)\s]+$", ErrorMessage = "El teléfono solo puede contener números, espacios, guiones, paréntesis y el signo +")]
        public string TELEFONO { get; set; }

        [Required(ErrorMessage = "La dirección completa es obligatoria")]
        [StringLength(200, ErrorMessage = "La dirección no puede exceder los 200 caracteres")]
        [Display(Name = "Dirección Completa")]
        public string DIRECCION_COMPLETA { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        [Range(typeof(DateTime), "1900-01-01", "2010-12-31", ErrorMessage = "La fecha de nacimiento debe estar entre 1900 y 2010")]
        public DateTime? FECHA_NACIMIENTO { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha de Registro")]
        public DateTime FECHA_REGISTRO { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Último Acceso")]
        public DateTime ULTIMO_ACCESO { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La provincia es obligatoria")]
        [Display(Name = "Provincia")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una provincia válida")]
        public int ID_PROVINCIA { get; set; }

        [Required(ErrorMessage = "El tipo de usuario es obligatorio")]
        [Display(Name = "Tipo de Usuario")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de usuario válido")]
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