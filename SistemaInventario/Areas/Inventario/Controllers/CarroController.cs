using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.ViewModels;
using SistemaInventario.Utilidades;
using Stripe.BillingPortal;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Security.Claims;

namespace SistemaInventario.Areas.Inventario.Controllers
{
    [Area("Inventario")]
    public class CarroController : Controller
    {

        private readonly IUnidadTrabajo _unidadTrabajo;
        private string _webUrl;

        [BindProperty]
        public CarroComprasVM carroComprasVM { get; set; }

        public CarroController(IUnidadTrabajo unidadTrabajo, IConfiguration configuration)
        {
            _unidadTrabajo = unidadTrabajo;
            _webUrl = configuration.GetValue<string>("DomainUrls:Web_Url");
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            carroComprasVM = new CarroComprasVM();
            carroComprasVM.Orden = new Modelos.Orden();
            carroComprasVM.CarroCompraLista = await _unidadTrabajo.CarroCompra.ObtenerTodos(
                                                u => u.UsuarioAppId == claim.Value,
                                                incluirPopiedades:"Producto");
            carroComprasVM.Orden.TotalOrden = 0;
            carroComprasVM.Orden.UsuarioAppId = claim.Value;

            foreach (var lista in carroComprasVM.CarroCompraLista)
            {
                lista.Precio = lista.Producto.Precio; //Siemre muestra el precio actual del producto
                carroComprasVM.Orden.TotalOrden += (lista.Precio * lista.Cantidad);
            }

            return View(carroComprasVM);
        }

        public async Task<IActionResult> mas(int carroId)
        {
            var carroCompras = await _unidadTrabajo.CarroCompra.ObtenerPrimero(c => c.Id == carroId);
            carroCompras.Cantidad += 1;
            await _unidadTrabajo.Guardar();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> menos(int carroId)
        {
            var carroCompras = await _unidadTrabajo.CarroCompra.ObtenerPrimero(c => c.Id == carroId);
            if(carroCompras.Cantidad == 1)
            {
                //Se remueve el registro del carro de compras y se actializa la sesion
                var carroLista = await _unidadTrabajo.CarroCompra.ObtenerTodos(
                                            c => c.UsuarioAppId == carroCompras.UsuarioAppId);
                var numeroProducto = carroLista.Count();
                _unidadTrabajo.CarroCompra.Remover(carroCompras);
                await _unidadTrabajo.Guardar();
                HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProducto -1 );
            }
            else
            {
                carroCompras.Cantidad -= 1;
                await _unidadTrabajo.Guardar();
            }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> remover(int carroId)
        {
            //Remueve el registro del carro de compras y actualiza la sesion
            var carroCompras = await _unidadTrabajo.CarroCompra.ObtenerPrimero(c => c.Id == carroId);
            var carroLista = await _unidadTrabajo.CarroCompra.ObtenerTodos(
                                           c => c.UsuarioAppId == carroCompras.UsuarioAppId);
            var numeroProducto = carroLista.Count();
            _unidadTrabajo.CarroCompra.Remover(carroCompras);
            await _unidadTrabajo.Guardar();
            HttpContext.Session.SetInt32(DS.ssCarroCompras, numeroProducto - 1);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Proceder()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            carroComprasVM = new CarroComprasVM()
            {
                Orden = new Modelos.Orden(),
                CarroCompraLista = await _unidadTrabajo.CarroCompra.ObtenerTodos(c => c.UsuarioAppId == claim.Value,
                                                                                    incluirPopiedades: "Producto"),
                Compañia = await _unidadTrabajo.Compañia.ObtenerPrimero()
            };

            carroComprasVM.Orden.TotalOrden = 0;
            carroComprasVM.Orden.UsuarioApp = await _unidadTrabajo.UsuarioAplicacion.ObtenerPrimero(u => u.Id == claim.Value);

            foreach (var lista in carroComprasVM.CarroCompraLista)
            {
                lista.Precio = lista.Producto.Precio;
                carroComprasVM.Orden.TotalOrden += (lista.Precio * lista.Cantidad);
            }
            carroComprasVM.Orden.NombresCliente = carroComprasVM.Orden.UsuarioApp.Nombres + " " +
                                                  carroComprasVM.Orden.UsuarioApp.Apellideos;
            carroComprasVM.Orden.Telefono = carroComprasVM.Orden.UsuarioApp.PhoneNumber;
            carroComprasVM.Orden.Direccion = carroComprasVM.Orden.UsuarioApp.Direccion;
            carroComprasVM.Orden.Pais = carroComprasVM.Orden.UsuarioApp.Pais;
            carroComprasVM.Orden.Ciudad = carroComprasVM.Orden.UsuarioApp.Ciudad;

            //Controlar stock
            foreach (var lista in carroComprasVM.CarroCompraLista)
            {
                //Capturar el stock de cada producto
                var producto = await _unidadTrabajo.BodegaProducto.ObtenerPrimero(b => b.ProductoId == lista.ProductoId &&
                                                                              b.BodegaId == carroComprasVM.Compañia.BodegaVentaId);
                if(lista.Cantidad > producto.Cantidad)
                {
                    TempData[DS.Error] = "La cantidad del producto " + lista.Producto.Descripcion +
                        "excede al stock actual (" + producto.Cantidad + ")";
                    return RedirectToAction("Index");
                }
            }
            return View(carroComprasVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Proceder(CarroComprasVM carroCompraVM)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            carroCompraVM.CarroCompraLista = await _unidadTrabajo.CarroCompra.ObtenerTodos(c => c.UsuarioAppId == claim.Value,
                                                                                    incluirPopiedades: "Producto");
            carroCompraVM.Compañia = await _unidadTrabajo.Compañia.ObtenerPrimero();
            carroCompraVM.Orden.TotalOrden = 0;
            carroCompraVM.Orden.UsuarioAppId = claim.Value;
            carroCompraVM.Orden.FechaOrden = DateTime.Now;

            foreach (var lista in carroCompraVM.CarroCompraLista)
            {
                lista.Precio = lista.Producto.Precio;
                carroCompraVM.Orden.TotalOrden += (lista.Precio * lista.Cantidad);
            }
            //Controlar Stock
            foreach (var lista in carroCompraVM.CarroCompraLista)
            {
                //Capturar el stock de cada producto
                var producto = await _unidadTrabajo.BodegaProducto.ObtenerPrimero(b => b.ProductoId == lista.ProductoId &&
                                                                              b.BodegaId == carroCompraVM.Compañia.BodegaVentaId);
                if (lista.Cantidad > producto.Cantidad)
                {
                    TempData[DS.Error] = "La cantidad del producto " + lista.Producto.Descripcion +
                        "excede al stock actual (" + producto.Cantidad + ")";
                    return RedirectToAction("Index");
                }
            }
            carroCompraVM.Orden.EstadoOrden = DS.EstadoPendiente;
            carroCompraVM.Orden.EstadoPago = DS.PagoEstadoPendiente;
            await _unidadTrabajo.Orden.Agregar(carroCompraVM.Orden);
            await _unidadTrabajo.Guardar();
            //Gravar el detalle de la orden
            foreach (var lista in carroCompraVM.CarroCompraLista)
            {
                OrdenDetalle ordenDetalle = new OrdenDetalle()
                {
                    ProductoId = lista.ProductoId,
                    OrdenId = carroCompraVM.Orden.Id,
                    Precio = lista.Precio,
                    Cantidad = lista.Cantidad
                };
                await _unidadTrabajo.OrdenDetalle.Agregar(ordenDetalle);
                await _unidadTrabajo.Guardar();
            }
            //Stripe
            var usuario = await _unidadTrabajo.UsuarioAplicacion.ObtenerPrimero(u => u.Id == claim.Value);
            var options = new Stripe.Checkout.SessionCreateOptions
            {                
                SuccessUrl = _webUrl + $"inventario/carro/OrdenConfirmacion?id={carroCompraVM.Orden.Id}",
                CancelUrl = _webUrl + $"inventario/carro/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                CustomerEmail = usuario.Email
            };
            foreach (var lista in carroCompraVM.CarroCompraLista)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions()
                    {
                        UnitAmount = (long)(lista.Precio * 100), //$20 => 200
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = lista.Producto.Descripcion
                        }
                    },
                    Quantity = lista.Cantidad
                };
                options.LineItems.Add(sessionLineItem);

            }
            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = service.Create(options);
            _unidadTrabajo.Orden.ActualizarPagoStripeId(carroCompraVM.Orden.Id, session.Id, session.PaymentIntentId);
            await _unidadTrabajo.Guardar();
            Response.Headers.Add("Location", session.Url); //Redirecciona a Stripe
            return new StatusCodeResult(303);            
        }

        public async Task<IActionResult> OrdenConfirmacion(int id)
        {
            var orden = await _unidadTrabajo.Orden.ObtenerPrimero(o => o.Id == id, incluirPopiedades: "UsuarioApp");
            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = service.Get(orden.SessionId);
            var carroCompra = await _unidadTrabajo.CarroCompra.ObtenerTodos(u => u.UsuarioAppId == orden.UsuarioAppId);
            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unidadTrabajo.Orden.ActualizarPagoStripeId(id, session.Id, session.PaymentIntentId);
                _unidadTrabajo.Orden.ActualizarEstado(id, DS.EstadoAprobado, DS.PagoEstadoAprobado);
                await _unidadTrabajo.Guardar();

                //Disminuir el Stock de la bodega de venta
                var compañia = await _unidadTrabajo.Compañia.ObtenerPrimero();
                foreach (var lista in carroCompra)
                {
                    var bodegaProducto = new BodegaProducto();
                    bodegaProducto = await _unidadTrabajo.BodegaProducto.ObtenerPrimero(b => b.ProductoId == lista.ProductoId
                                                                                      && b.BodegaId == compañia.BodegaVentaId);
                    await _unidadTrabajo.KardexInventario.RegistrarKaedex(bodegaProducto.Id, "Salida",
                                                                          "Venta - Orden#" + id,
                                                                           bodegaProducto.Cantidad,
                                                                           lista.Cantidad,
                                                                           orden.UsuarioAppId);
                    bodegaProducto.Cantidad -= lista.Cantidad;
                    await _unidadTrabajo.Guardar();
                }

            }
            //Borramos el carro de compra y la sesion del carro de compra
            
            List<CarroCompra> carroCompraLista = carroCompra.ToList();
            _unidadTrabajo.CarroCompra.RemoverRango(carroCompraLista);
            await _unidadTrabajo.Guardar();
            HttpContext.Session.SetInt32(DS.ssCarroCompras, 0);
            return View(id);
        }
    }


}
