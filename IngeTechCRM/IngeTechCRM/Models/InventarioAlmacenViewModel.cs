using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class InventarioAlmacenViewModel
    {
        public int ID_ALMACEN { get; set; }

        [Display(Name = "Almacén")]
        public string NOMBRE_ALMACEN { get; set; }

        [Display(Name = "Provincia")]
        public string PROVINCIA { get; set; }

        [Display(Name = "Total Productos")]
        public int TOTAL_PRODUCTOS { get; set; }

        [Display(Name = "Valor Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal VALOR_TOTAL { get; set; }

        [Display(Name = "Productos con Stock Bajo")]
        public int PRODUCTOS_STOCK_BAJO { get; set; }
    }
}
