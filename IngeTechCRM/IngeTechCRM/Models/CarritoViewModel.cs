namespace IngeTechCRM.Models
{
    public class CarritoViewModel
    {
        public int ID_CARRITO { get; set; }
        public List<ItemCarritoViewModel> Items { get; set; }
        public decimal TOTAL { get; set; }
    }
}
