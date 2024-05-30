﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.AccesoDatos.Data;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos.Especificaciones;

namespace SistemaInventario.AccesoDatos.Repositorio
{
    public class Repositorio<T> : IRepositorio<T> where T : class
    {

        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repositorio(ApplicationDbContext db)
        {
            _db = db;
            this.dbSet = _db.Set<T>();
        }

        public async Task Agregar(T entidad)
        {
            await dbSet.AddAsync(entidad);  //Insertar en tabla
        }

        public async Task<T> Obtener(int id)
        {
            return await dbSet.FindAsync(id); //Busca filtrando por la tabla
        }

        public async Task<IEnumerable<T>> ObtenerTodos(Expression<Func<T, bool>> Filtro = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string incluirPopiedades = null, bool isTracking = true)
        {
            IQueryable<T> query = dbSet;
            if(Filtro != null)
            {
                query = query.Where(Filtro);
            }
            if(incluirPopiedades != null)
            {
                foreach (var incluirProp in incluirPopiedades.Split(new char[] { ','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(incluirProp); //ejemplo ""Categoria.Marca"
                }
            }
            if(orderBy != null)
            {
                query = orderBy(query);
            }
            if (!isTracking)
            {
                query = query.AsNoTracking();
            }
            return await query.ToListAsync();
        }

        public PagedList<T> ObtenerTodosPaginado(Parametros parametros, Expression<Func<T, bool>> Filtro = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string incluirPopiedades = null, bool isTracking = true)
        {
            IQueryable<T> query = dbSet;
            if (Filtro != null)
            {
                query = query.Where(Filtro);
            }
            if (incluirPopiedades != null)
            {
                foreach (var incluirProp in incluirPopiedades.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(incluirProp); //ejemplo ""Categoria.Marca"
                }
            }
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            if (!isTracking)
            {
                query = query.AsNoTracking();
            }
            return PagedList<T>.ToPagedList(query, parametros.PageNumber, parametros.PageSize);
        }

        public async Task<T> ObtenerPrimero(Expression<Func<T, bool>> Filtro = null, string incluirPopiedades = null, bool isTracking = true)
        {
            IQueryable<T> query = dbSet;
            if (Filtro != null)
            {
                query = query.Where(Filtro);
            }
            if (incluirPopiedades != null)
            {
                foreach (var incluirProp in incluirPopiedades.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(incluirProp); //ejemplo ""Categoria.Marca"
                }
            }            
            if (!isTracking)
            {
                query = query.AsNoTracking();
            }
            return await query.FirstOrDefaultAsync();
        }

        public void Remover(T entidad)
        {
            dbSet.Remove(entidad);
        }

        public void RemoverRango(IEnumerable<T> entidad)
        {
            dbSet.RemoveRange(entidad);
        }

        
    }
}
