using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido")]
        [Display(Name = "Correo Electrónico")]
        public string CORREO_ELECTRONICO { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string CONTRASENA { get; set; }

        [Display(Name = "Recordarme")]
        public bool RECORDARME { get; set; }
    }
}
