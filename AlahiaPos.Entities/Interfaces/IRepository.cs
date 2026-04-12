using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        public void DeleteEntity(T entity);
        public IEnumerable<T> GetAllByExpresionInclude(
   Expression<Func<T, bool>> expression,
   params Expression<Func<T, object>>[] includes);
        T GetById(int id);
        void SaveNoAsync(T entity);
        public IEnumerable<T> GetAllByExpresionNoAsync(Expression<Func<T,
            bool>> expression,
            params string[] IncludeEntity);
        T GetByExpresion(Expression<Func<T, bool>> expression);
        Task<T> GetByExpresionAsync(Expression<Func<T,bool>> expression);
        Task<T> GetByExpresionAsync(Expression<Func<T, bool>> expression, params string[] IncludeEntity);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(params string[] IncludeEntity);
        Task<IEnumerable<T>> GetAllByExpresionAsync(Expression<Func<T, bool>> expression);
        IEnumerable<T> GetAllByExpresionNoAsync(Expression<Func<T, bool>> expression);
        Task<IEnumerable<T>> GetAllByExpresionAsync(Expression<Func<T, bool>> expression, params string[] IncludeEntity);
        Task<bool> GetAny(Expression<Func<T, bool>> expression);
        bool GetAnyNotAsync(Expression<Func<T, bool>> expression);
       
        Task Save(T entity);
        Task Save(IEnumerable<T> entity);
        void Update(int Id,T entity);
        void Delete(int id);

       

    }
}
