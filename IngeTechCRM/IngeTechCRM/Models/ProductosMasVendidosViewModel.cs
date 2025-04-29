using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ProductosMasVendidosViewModel
    {
        public int ID_PRODCUTO { get; set; }

        [Display(Name = "Código")]
        public string CODIGO { get; set; }

        [Display(Name = "Producto")]
        public string PRODUCTO { get; set; }

        [Display(Name = "Categoría")]
        public string CATEGORIA { get; set; }

        [Display(Name = "Unidades Vendidas")]
        public int UNIDADES_VENDIDAS { get; set; }

        [Display(Name = "Monto Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal MONTO_TOTAL { get; set; }
    }
}
