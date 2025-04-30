using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class InventarioCategoriaViewModel
    {
        public int ID_CATEGORIA { get; set; }

        [Display(Name = "Categoría")]
        public string NOMBRE_CATEGORIA { get; set; }

        [Display(Name = "Total Productos")]
        public int TOTAL_PRODUCTOS { get; set; }

        [Display(Name = "Unidades Totales")]
        public int UNIDADES_TOTALES { get; set; }

        [Display(Name = "Valor Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal VALOR_TOTAL { get; set; }
    }
}
