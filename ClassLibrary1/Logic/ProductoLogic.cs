
using Data.Models;
using Entities.Items;
using Entities.RequestModels;
using Logic.ILogic;
using Microsoft.EntityFrameworkCore;
using Enumeradores;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Entities.ResponseModels;

namespace Logic.Logic
{
    public class ProductoLogic : IProductoLogic
    {
        private readonly Maria_MCContext _context;

        public ProductoLogic(Maria_MCContext context)
        {
            _context = context;
        }

        public async Task<object> CargaDeProductosporArchivo(CargarProductosRequest productoRequest) {

            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Agregar los productos del Archivo Excel
                    foreach (var prod in productoRequest.Productos)
                    {
                        Producto producto = new Producto
                        {
                            IdEstado = prod.IdEstado,
                            //Descripcion = prod.Descripcion,
                            Stock = 1,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            IdUsuario = productoRequest.IdUsuario,
                            IdTipoProducto = prod.IdTipoProducto,
                            IdTipoTalle = prod.IdTipoTalle,
                            IdTipoMarca = prod.IdTipoMarca,
                            PrecioDeCompra = prod.PrecioDeCompra,
                            PrecioDeVenta = prod.PrecioDeVenta,
                        };

                        _context.Productos.Add(producto);
                        await _context.SaveChangesAsync();

                        // 2. Editar el estado del producto en la tabla ProductoEstado
                        ProductoEstado? estadoAnterior = await _context.ProductoEstado
                            .Where(pe => pe.IdProducto == producto.IdProducto && pe.FechaFin == null)
                            .FirstOrDefaultAsync();

                        if (estadoAnterior != null)
                        {
                            estadoAnterior.FechaFin = DateTime.Now;
                            _context.Entry(estadoAnterior).State = EntityState.Modified;
                        }

                        ProductoEstado nuevoEstado = new ProductoEstado
                        {
                            IdProducto = producto.IdProducto,
                            IdEstado = producto.IdEstado,
                            FechaInicio = DateTime.Now,
                            IdUsuario = productoRequest.IdUsuario
                        };

                        _context.ProductoEstado.Add(nuevoEstado);
                    }

                    await _context.SaveChangesAsync();

                    // Commit de la transacción
                    dbContextTransaction.Commit();
                    return new { result = "ok", message = "El archivo se proceso correctamente" };
                }
                catch (Exception e)
                {
                    // En caso de error, hacer rollback de la transacción
                    dbContextTransaction.Rollback();
                    // Manejar el error o lanzarlo nuevamente si es necesario
                    return new { result = "error", message = "Ocurrio un error al cargar los datos del archivo" };
                    
                }
            }

        }

        public async Task<object> GetProductoToSell(string codigo)
        {
            try {
                Int64 id = 0;
                if (!Int64.TryParse(codigo, out var result))
                {
                    return new { result = "error", message = "Producto inexistente" };
                }

                id = result;

                var producto = await _context.Productos
                    .Include(p => p.IdEstadoNavigation)
                    .Include(p => p.IdTipoMarcaNavigation)
                    .Include(p => p.IdTipoTalleNavigation)
                    .Include(p => p.IdTipoProductoNavigation)
                    .FirstOrDefaultAsync(p => p.IdProducto == id);

                if (producto == null)
                {
                    return new { result = "error", message = "Producto inexistente" };
                }

                if (producto.IdEstado != (int)TipoEstadoEnum.Para_la_venta)
                {
                    return new { result = "error", message = "El Producto no se puede cargar para la venta. Estado ('" + producto.IdEstadoNavigation.Descripcion + "') \n para ello debe cambiar el estado del producto a -> '" + TipoEstadoEnum.Para_la_venta.ToString() + "'." };
                }

                var item = new
                {
                    id = producto.IdProducto,
                    //code = producto.CodigoDeBarra,
                    //description = producto.Descripcion,
                    idEstado = producto.IdEstado,
                    estado = producto.IdEstadoNavigation?.Descripcion,
                    idCategoria = producto.IdTipoProductoNavigation?.Id, // el TipoProducto es la categoria de Producto
                    categoria = producto.IdTipoProductoNavigation?.Descripcion, // el TipoProducto es la categoria de Producto
                    idMarca = producto.IdTipoMarca,
                    marca = producto.IdTipoMarcaNavigation?.Descripcion,
                    idTalle = producto.IdTipoTalle,
                    talle = producto.IdTipoTalleNavigation?.Descripcion,
                    price = producto.PrecioDeVenta
                };

                return new { result = "ok", item };

            } catch(Exception e) {

                return new { result = "error", message = "Fallo en GetProductoToSell -> " + e.Message };

            }
           
        } 
        public async Task<object> GetProducto(string codigo)
        {
            try {

                Int64 id = 0;
                if (!Int64.TryParse(codigo, out var result))
                {
                    return new { result = "error", message = "Producto inexistente" };
                }

                id = result;

                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == id);

                if (producto == null)
                {
                    return new { result = "error", message = "Producto inexistente" };
                }

                var item = new
                {
                    id = producto.IdProducto,
                    //code = producto.CodigoDeBarra,
                    //descripcion = producto.Descripcion,
                    idEstado = producto.IdEstado,
                    //estado = producto.IdEstadoNavigation?.Descripcion,
                    idTipoProducto = producto.IdTipoProducto, // el TipoProducto es la categoria de Producto
                                                              //categoria = producto.IdTipoProductoNavigation?.Descripcion, // el TipoProducto es la categoria de Producto
                    idTipoMarca = producto.IdTipoMarca,
                    //marca = producto.IdTipoMarcaNavigation?.Descripcion,
                    idTipoTalle = producto.IdTipoTalle,
                    //talle = producto.IdTipoTalleNavigation?.Descripcion,
                    precioDeVenta = producto.PrecioDeVenta,
                    precioDeCompra = producto.PrecioDeCompra
                };

                return new { result = "ok", item };
            }
            catch (Exception e)
            {

                return new { result = "error", message = "Fallo en GetProducto -> " + e.Message };

            }
        }

        public async Task<object> GuardarProducto(UpdateProductosRequest productoRequest)
        {
            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var productoAModificar = await _context.Productos.FindAsync(productoRequest.producto.Id);
                    if (productoAModificar == null)
                    {
                        return new { result = "error", message = "Producto inexistente" };
                    }

                    //productoAModificar.Descripcion = productoRequest.producto.Descripcion;
                    productoAModificar.IdEstado = productoRequest.producto.IdEstado;
                    productoAModificar.IdTipoMarca = productoRequest.producto.IdTipoMarca;
                    productoAModificar.IdTipoProducto = productoRequest.producto.IdTipoProducto;
                    productoAModificar.IdTipoTalle = productoRequest.producto.IdTipoTalle;
                    productoAModificar.PrecioDeVenta = productoRequest.producto.PrecioDeVenta;
                    productoAModificar.PrecioDeCompra = productoRequest.producto.PrecioDeCompra;
                    productoAModificar.IdUsuario = productoRequest.IdUsuario;
                    productoAModificar.ModifiedDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // 2. Editar el estado del producto en la tabla ProductoEstado
                    ProductoEstado? estadoAnterior = await _context.ProductoEstado
                        .Where(pe => pe.IdProducto == productoAModificar.IdProducto && pe.FechaFin == null)
                        .FirstOrDefaultAsync();
                    if(estadoAnterior != null && estadoAnterior.IdEstado != productoRequest.producto.IdEstado) {
                        if (estadoAnterior != null)
                        {
                            estadoAnterior.FechaFin = DateTime.Now;
                            _context.Entry(estadoAnterior).State = EntityState.Modified;
                        }

                        ProductoEstado nuevoEstado = new ProductoEstado
                        {
                            IdProducto = productoAModificar.IdProducto,
                            IdEstado = productoAModificar.IdEstado,
                            FechaInicio = DateTime.Now,
                            IdUsuario = productoRequest.IdUsuario
                        };

                        _context.ProductoEstado.Add(nuevoEstado);
                    }

                    // Commit de la transacción
                    dbContextTransaction.Commit();

                    return new { result = "ok", message = "El Producto con ID: " + productoAModificar.IdProducto.ToString() + " se actualizo correctamente." };
                }
                catch (Exception e)
                {
                    // En caso de error, hacer rollback de la transacción
                    dbContextTransaction.Rollback();
                    // Manejar el error o lanzarlo nuevamente si es necesario
                    return new { result = "error", message = "Ocurrio un error al cargar los datos del archivo" };

                }
            }
        }

        public async Task<object> GetAllProductosToPrint()
        {
            try {
                var ListaProductos = await _context.Productos
                    .Include(p => p.IdEstadoNavigation)
                    .Include(p => p.IdTipoMarcaNavigation)
                    .Include(p => p.IdTipoTalleNavigation)
                    .Include(p => p.IdTipoProductoNavigation)
                    .Where(p => p.IdEstado != (int)TipoEstadoEnum.Vendido)
                    .ToListAsync();
                //.FirstOrDefaultAsync(p => p.IdProducto == id);

                if (ListaProductos.Count == 0)
                {
                    return new { result = "error", message = "No Hay Productos para Imprimir." };
                }
                var listToPrint = new List<ProductoPrintResponse>();
                foreach (var producto in ListaProductos)
                {
                    var item = new ProductoPrintResponse
                    {
                        IdProducto = producto.IdProducto,
                        //Descripcion = producto.Descripcion,
                        TipoProductoDescripcion = producto.IdTipoProductoNavigation.Descripcion, // el TipoProducto es la categoria de Producto
                        TipoMarcaDescripicion = producto.IdTipoMarcaNavigation.Descripcion,
                        TipoTalleDescripcion = producto.IdTipoTalleNavigation.Descripcion.Split('-')[0].Trim(),
                        PrecioDeVenta = producto.PrecioDeVenta
                    };
                    listToPrint.Add(item);
                }

                return new { result = "ok", listToPrint };
            }
            catch (Exception e)
            {

                return new { result = "error", message = "Fallo en GetAllProductosToPrint -> " + e.Message };

            }
        }
    }
}
