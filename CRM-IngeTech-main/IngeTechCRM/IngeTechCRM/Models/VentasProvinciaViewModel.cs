using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class VentasProvinciaViewModel
    {
        public int ID_PROVINCIA { get; set; }

        [Display(Name = "Provincia")]
        public string PROVINCIA { get; set; }

        [Display(Name = "Total Pedidos")]
        public int TOTAL_PEDIDOS { get; set; }

        [Display(Name = "Monto Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal MONTO_TOTAL { get; set; }
    }
}
