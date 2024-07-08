using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.Especificaciones;
using SistemaInventario.Modelos.ViewModel;
using SistemaInventario.Modelos.ViewModels;
using SistemaInventario.Utilidades;
using System.Diagnostics;
using System.Security.Claims;

namespace SistemaInventario.Areas.Inventario.Controllers
{
    [Area("Inventario")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnidadTrabajo _unidadTrabajo;
        [BindProperty]
        public CarroComprasVM carroComprasVM { get; set; }

        public HomeController(ILogger<HomeController> logger, IUnidadTrabajo unidadTrabajo)
        {
            _logger = logger;
            _unidadTrabajo = unidadTrabajo;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string busqueda = "", string busquedaActual = "")
        {

            //Controlar sesion
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if(claim != null)
            {
                var carroLista = await _unidadTrabajo.CarroCompra.ObtenerTodos(c => c.UsuarioAppId == claim.Value);
                var numeroProductos = carroLista.Count(); //Obtiene el numeor de productos en el carro de compras
                HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProductos);
            }


            if (!String.IsNullOrEmpty(busqueda))
            {
                pageNumber = 1;
            }
            else
            {
                busqueda = busquedaActual;
            }
            ViewData["BusquedaActual"] = busqueda;

            if(pageNumber < 1) { pageNumber = 1; }

            Parametros parametros = new Parametros()
            {
                PageNumber = pageNumber,
                PageSize = 4
                
            };

            var resultado = _unidadTrabajo.Producto.ObtenerTodosPaginado(parametros);

            if (!String.IsNullOrEmpty(busqueda))
            {
                resultado = _unidadTrabajo.Producto.ObtenerTodosPaginado(parametros, p => p.Descripcion.Contains(busqueda));
            }

            ViewData["TotalPaginas"] = resultado.MetaData.TotalPages;
            ViewData["TotalRegristros"] = resultado.MetaData.TotalCount;
            ViewData["PageSiza"] = resultado.MetaData.PageSize;
            ViewData["PageNumber"] = pageNumber;
            ViewData["Previo"] = "disabled"; //clase css para Desabilitar boton
            ViewData["Siguiente"] = "";

            if(pageNumber > 1) { ViewData["Previo"] = ""; }
            if(resultado.MetaData.TotalPages <= pageNumber) { ViewData["Siguiente"] = "disabled"; }

            return View(resultado);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            carroComprasVM = new CarroComprasVM();
            carroComprasVM.Compañia = await _unidadTrabajo.Compañia.ObtenerPrimero();
            carroComprasVM.Producto = await _unidadTrabajo.Producto.ObtenerPrimero(p => p.Id == id,
                                                    incluirPopiedades: "Marca,Categoria");
            var bodegaProducto = await _unidadTrabajo.BodegaProducto.ObtenerPrimero(b => b.ProductoId == id &&
                                                                        b.BodegaId == carroComprasVM.Compañia.BodegaVentaId);
            if(bodegaProducto == null)
            {
                carroComprasVM.Stock = 0;
            }
            else
            {
                carroComprasVM.Stock = bodegaProducto.Cantidad;
            }
            carroComprasVM.CarroCompra = new CarroCompra()
            {
                Producto = carroComprasVM.Producto,
                ProductoId = carroComprasVM.Producto.Id
            };
            return View(carroComprasVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Detalle(CarroComprasVM carroComprasVM)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            carroComprasVM.CarroCompra.UsuarioAppId = claim.Value;

            CarroCompra carroBD = await _unidadTrabajo.CarroCompra.ObtenerPrimero(c => c.UsuarioAppId == claim.Value &&
                                                                                    c.ProductoId == carroComprasVM.CarroCompra.ProductoId);
            if (carroBD == null)
            {
                await _unidadTrabajo.CarroCompra.Agregar(carroComprasVM.CarroCompra);
            }
            else
            {
                carroBD.Cantidad += carroComprasVM.CarroCompra.Cantidad;
                _unidadTrabajo.CarroCompra.Actualizar(carroBD);
            }
            await _unidadTrabajo.Guardar();
            TempData[DS.Exitosa] = "¡Producto agregado!";

            //Agregar valor a la sesion
            var carroLista = await _unidadTrabajo.CarroCompra.ObtenerTodos(c => c.UsuarioAppId == claim.Value);
            var numeroProductos = carroLista.Count(); //Obtiene el numeor de productos en el carro de compras
            HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProductos);

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
