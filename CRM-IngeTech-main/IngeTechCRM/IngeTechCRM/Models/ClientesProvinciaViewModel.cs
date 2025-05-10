using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ClientesProvinciaViewModel
    {
        public int ID_PROVINCIA { get; set; }

        [Display(Name = "Provincia")]
        public string PROVINCIA { get; set; }

        [Display(Name = "Total Clientes")]
        public int TOTAL_CLIENTES { get; set; }

        [Display(Name = "Clientes Activos")]
        public int CLIENTES_ACTIVOS { get; set; }

        [Display(Name = "% Activos")]
        [DisplayFormat(DataFormatString = "{0:F2}%")]
        public double PORCENTAJE_ACTIVOS { get; set; }
    }
}
