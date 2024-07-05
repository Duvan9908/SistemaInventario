﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaInventario.Modelos;

namespace SistemaInventario.AccesoDatos.Repositorio.IRepositorio
{
    public interface ICompañiaRepositorio : IRepositorio<Compañia>
    {
        void Actualizar(Compañia compañia);

    }
}
