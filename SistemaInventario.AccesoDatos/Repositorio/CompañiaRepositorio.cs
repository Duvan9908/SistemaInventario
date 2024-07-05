using SistemaInventario.AccesoDatos.Data;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaInventario.AccesoDatos.Repositorio
{
    public class CompañiaRepositorio : Repositorio<Compañia>, ICompañiaRepositorio
    {

        private readonly ApplicationDbContext _db;

        public CompañiaRepositorio(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Actualizar(Compañia compañia)
        {
            var compañiaBD = _db.Compañias.FirstOrDefault(b => b.Id == compañia.Id);
            if (compañiaBD != null)
            {
                compañiaBD.Nombre = compañia.Nombre;
                compañiaBD.Descripcion = compañia.Descripcion;
                compañiaBD.Pais = compañia.Pais;
                compañiaBD.Ciudad = compañia.Ciudad;
                compañiaBD.Direccion = compañia.Direccion;
                compañiaBD.Telefono = compañia.Telefono;
                compañiaBD.BodegaVentaId = compañia.BodegaVentaId;
                compañiaBD.ActualizadoPorId = compañia.ActualizadoPorId;
                compañiaBD.FechaActualizacion = compañia.FechaActualizacion;
                _db.SaveChanges();
            }
        }
    }
}
