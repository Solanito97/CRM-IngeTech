namespace IngeTechCRM.Models
{
    public class ItemCarritoViewModel
    {
        public int ID_ITEM { get; set; }
        public int ID_PRODCUTO { get; set; }
        public string CODIGO_PRODUCTO { get; set; }
        public string NOMBRE_PRODUCTO { get; set; }
        public string CATEGORIA_PRODUCTO { get; set; }
        public string MARCA_PRODUCTO { get; set; }
        public string IMAGEN_PRODUCTO { get; set; }
        public int CANTIDAD { get; set; }
        public decimal PRECIO_UNITARIO { get; set; }
        public decimal SUBTOTAL { get; set; }
    }
}
