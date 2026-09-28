using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Repository
{
    public class BaseRepository<T> : IRepository<T> where T : class
    {

        public readonly AlahiaPosContext _Context;
        public DbSet<T> _Dbset;
        public BaseRepository(AlahiaPosContext Context)
        {
            this._Context = Context;
            _Dbset=this._Context.Set<T>();

        }

        public void DeleteEntity(T entity)
        {
            this._Context.Entry(entity).State = EntityState.Deleted;
            this._Context.SaveChanges();
        }
        public void Delete(int id)
        {
            var Entity = _Dbset.Find(id);
            if(Entity!=null)
            {
                _Dbset.Remove(Entity);
                _Context.SaveChanges();
            }
            
        }
        public IEnumerable<T> GetAllByExpresionInclude(
    Expression<Func<T, bool>> expression,
    params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _Dbset;

            if (includes != null)
            {
                foreach (var include in includes)
                    query = query.Include(include);
            }

            return query.Where(expression).ToList();
        }
        public IEnumerable<T> GetAllByExpresionNoAsync(Expression<Func<T, bool>> expression)
        {
            return _Dbset.Where(expression).ToList(); 
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _Dbset.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(params string[] IncludeEntity)
        {
            
            foreach (var item in IncludeEntity)
            {
                  _Dbset.Include(item);
            }

            return await _Dbset.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllByExpresionAsync(Expression<Func<T, 
            bool>> expression)
        {
            return await _Dbset.Where(expression).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllByExpresionAsync(
     Expression<Func<T, bool>> expression,
     params string[] includeEntities)
        {
            IQueryable<T> query = _Dbset;

            foreach (var include in includeEntities)
            {
                query = query.Include(include);
            }

            return await query
                .Where(expression)
                .ToListAsync();
        }

        public IEnumerable<T> GetAllByExpresionNoAsync(
           Expression<Func<T, bool>> expression,
            params string[] includeEntities)
        {
            IQueryable<T> query = _Dbset;

            foreach (var include in includeEntities)
            {
                query = query.Include(include);
            }

            return query.Where(expression).ToList();
        }

        public async Task<bool> GetAny(Expression<Func<T, bool>> expression)
        {
            return await _Dbset.AnyAsync(expression);
        }
        public bool GetAnyNotAsync(Expression<Func<T, bool>> expression)
        {
            return  _Dbset.Any(expression);
        }

        public async Task<T> GetByExpresionAsync(Expression<Func<T, bool>> expression)
        {
            return await _Dbset.FirstOrDefaultAsync(expression);
        }
        public T GetByExpresion(Expression<Func<T, bool>> expression)
        {
            return  _Dbset.FirstOrDefault(expression);
        }

        public async Task<T> GetByExpresionAsync(Expression<Func<T, bool>> expression,
            params string[] IncludeEntity)
        {
            foreach (var item in IncludeEntity)
            {
                _Dbset.Include(item);
            }

            return await _Dbset.FirstOrDefaultAsync(expression);
        }

        public async Task<T> GetByIdAsync(int id)
        {
           
            return await _Dbset.FindAsync(id);
        }
        public T GetById(int id)
        {

            return  _Dbset.Find(id);
        }
        

        public async Task Save(T entity)
        {
           
              await _Dbset.AddAsync(entity);
             await _Context.SaveChangesAsync();
        }
        public void SaveNoAsync(T entity)
        {

             _Dbset.Add(entity);
             _Context.SaveChanges();
        }
        public async Task Save(IEnumerable<T> entity)
        {
            try
            {
                await _Dbset.AddRangeAsync(entity);
                _Context.SaveChanges();
            }
            catch(Exception ex)
            {

            }
           
        }

        public void Update(int Id,T entity)
        {
            _Context.ChangeTracker.Clear();
            _Dbset.Update(entity);
            _Context.SaveChanges();
        }
        public void Dispose()
        {
            _Context.Dispose();
        }
    }
}
