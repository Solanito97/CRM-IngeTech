using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ProductoInventarioViewModel
    {
        public int ID_PRODUCTO { get; set; }

        [Display(Name = "Código")]
        public string CODIGO { get; set; }

        [Display(Name = "Nombre")]
        public string NOMBRE { get; set; }

        [Display(Name = "Marca")]
        public string MARCA { get; set; }

        [Display(Name = "Precio")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal PRECIO { get; set; }

        [Display(Name = "Cantidad Total")]
        public int CANTIDAD_TOTAL { get; set; }

        [Display(Name = "Valor Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal VALOR_TOTAL { get; set; }
    }
}

