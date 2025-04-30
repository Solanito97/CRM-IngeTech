using Microsoft.AspNetCore.Mvc;
using IngeTechCRM.Models;

namespace IngeTechCRM.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly IngeTechDbContext _context;

        public CartCountViewComponent(IngeTechDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            int itemsEnCarrito = 0;

            if (usuarioId.HasValue)
            {
                itemsEnCarrito = _context.Carritos
                    .Where(c => c.ID_USUARIO == usuarioId && c.ACTIVO)
                    .SelectMany(c => c.Items)
                    .Sum(i => i.CANTIDAD);
            }

            return View(itemsEnCarrito); // Usa la vista Default.cshtml por defecto
        }
    }
}