using Ordering.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ordering.Application.Contracts.Persistence
{
    /// <summary>
    /// Defines the IAsyncRepository type.
    /// </summary>
    public interface IAsyncRepository<T> where T : EntityBase
    {
        /// <summary>
        /// Describes the GetAllAsync contract member.
        /// </summary>
        Task<IReadOnlyList<T>> GetAllAsync();
        /// <summary>
        /// Describes the GetAsync contract member.
        /// </summary>
        Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
        /// <summary>
        /// Describes the GetAsync contract member with sorting and an include path.
        /// </summary>
        Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate = null,
                                        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
                                        string includeString = null,
                                        bool disableTracking = true);
        /// <summary>
        /// Describes the GetAsync contract member with sorting and multiple includes.
        /// </summary>
        Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate = null,
                                       Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
                                       List<Expression<Func<T, object>>> includes = null,
                                       bool disableTracking = true);
        /// <summary>
        /// Describes the GetByIdAsync contract member.
        /// </summary>
        Task<T> GetByIdAsync(int id);
        /// <summary>
        /// Describes the AddAsync contract member.
        /// </summary>
        Task<T> AddAsync(T entity);
        /// <summary>
        /// Describes the UpdateAsync contract member.
        /// </summary>
        Task UpdateAsync(T entity);
        /// <summary>
        /// Describes the DeleteAsync contract member.
        /// </summary>
        Task DeleteAsync(T entity);
    }
}
