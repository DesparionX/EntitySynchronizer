using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.EntitySynchronizer
{
    public enum Operation { Add, Update, Delete };

    /// <summary>
    /// Interface that provides the ability to easily synchronize entities using dto collection.
    /// Currently there are three operations:
    /// <list type="number">
    /// <item><description>Add</description></item>
    /// <item><description>Update</description></item>
    /// <item><description>Delete</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="ТContext">Ensures the providen type is <see cref="DbContext"/></typeparam>
    
    public interface IEntitySynchronizer<ТContext> where ТContext : DbContext
    {
        DbContext Context { get; }
        IMapper Mapper { get; }


        /// <summary>
        /// This is the main method used to synchronize entities in db based on given operation.
        /// </summary>
        /// <typeparam name="TEntity">Ensures that first collection is entity class.</typeparam>
        /// <typeparam name="TDTO">Ensures that second collection is the DTO class of that entity type.</typeparam>
        /// <typeparam name="TId">Specifies the ID type of the entities and DTOs, also enforces it to be value type.</typeparam>
        /// <param name="entityList">Entities collection from db. (Recommendation: retrieve the list right before calling the method.)</param>
        /// <param name="dtoList">DTOs collection. (Same type as the entities collection.)</param>
        /// <param name="operation">The operation that will be executed. (Add, Update, Delete)</param>
        /// <returns><see cref="SynchronizeResult"/> Containing operation status and message.</returns>
        Task<SynchronizeResult> SynchronizeCollection<TEntity, TDTO, TId>(ICollection<TEntity> entityList, ICollection<TDTO> dtoList, Operation operation)
            where TId : IEquatable<TId>
            where TEntity : class,IEntity<TId> 
            where TDTO : class, IDTO<TId, TEntity>;

    }
}
