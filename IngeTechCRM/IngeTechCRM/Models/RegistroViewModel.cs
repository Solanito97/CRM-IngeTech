using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "La identificación es obligatoria")]
        [Display(Name = "Identificación")]
        public int IDENTIFICACION { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre de usuario debe tener entre {2} y {1} caracteres", MinimumLength = 3)]
        [Display(Name = "Nombre de Usuario")]
        public string NOMBRE_USUARIO { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido")]
        [Display(Name = "Correo Electrónico")]
        public string CORREO_ELECTRONICO { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(50, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string CONTRASENA { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string CONFIRMAR_CONTRASENA { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre completo debe tener entre {2} y {1} caracteres", MinimumLength = 3)]
        [Display(Name = "Nombre Completo")]
        public string NOMBRE_COMPLETO { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono debe tener entre {2} y {1} caracteres", MinimumLength = 8)]
        [Display(Name = "Teléfono")]
        public string TELEFONO { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(200, ErrorMessage = "La dirección debe tener entre {2} y {1} caracteres", MinimumLength = 10)]
        [Display(Name = "Dirección Completa")]
        public string DIRECCION_COMPLETA { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime? FECHA_NACIMIENTO { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria")]
        [Display(Name = "Provincia")]
        public int ID_PROVINCIA { get; set; }
    }
}
