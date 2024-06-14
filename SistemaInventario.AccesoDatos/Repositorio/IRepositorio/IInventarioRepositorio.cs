using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaInventario.Modelos;

namespace SistemaInventario.AccesoDatos.Repositorio.IRepositorio
{
    public interface IInventarioRepositorio : IRepositorio<Inventario>
    {
        void Actualizar(Inventario inventario);

        IEnumerable<SelectListItem> ObtenerTodosDropdownList(string obj);
        

    }
}
