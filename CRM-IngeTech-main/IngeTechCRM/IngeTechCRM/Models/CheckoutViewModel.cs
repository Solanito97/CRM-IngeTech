using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "La dirección de envío es obligatoria")]
        [StringLength(255, ErrorMessage = "La dirección debe tener entre {2} y {1} caracteres", MinimumLength = 10)]
        [Display(Name = "Dirección de Envío")]
        public string DIRECCION_ENVIO { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria")]
        [Display(Name = "Provincia")]
        public int ID_PROVINCIA { get; set; }

        [StringLength(500)]
        [Display(Name = "Notas")]
        public string NOTAS { get; set; }

        public List<ItemCarritoViewModel> ItemsCarrito { get; set; }
        public decimal TOTAL { get; set; }
    }
}
