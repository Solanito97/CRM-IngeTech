using System.ComponentModel.DataAnnotations;

namespace IngeTechCRM.Models
{
    public class ComunicadoEfectividadViewModel
    {
        public int ID_COMUNICADO { get; set; }

        [Display(Name = "Título")]
        public string TITULO { get; set; }

        [Display(Name = "Fecha Creación")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime FECHA_CREACION { get; set; }

        [Display(Name = "Fecha Envío")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime FECHA_ENVIO { get; set; }

        [Display(Name = "Total Envíos")]
        public int TOTAL_ENVIOS { get; set; }
    }
}
