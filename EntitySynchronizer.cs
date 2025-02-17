using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.EntitySynchronizer
{
    /// <summary>
    /// Implementation of <see cref="IEntitySynchronizer{ТContext}"/>
    /// Provides <see cref="SynchronizeCollection{TEntity, TDTO, TId}(ICollection{TEntity}, ICollection{TDTO}, Operation)"/> method
    /// for Add, Update or Delete entity collection in DB.
    /// </summary>
    public class EntitySynchronizer : IEntitySynchronizer<DbContext>
    {
        private readonly DbContext _context;
        private readonly IMapper? _mapper;
        private readonly ILogger<EntitySynchronizer> _logger;
        public DbContext Context => _context;
        public IMapper? Mapper => _mapper;

        public EntitySynchronizer(DbContext context, IMapper mapper, ILogger<EntitySynchronizer> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }
        public EntitySynchronizer(DbContext context, ILogger<EntitySynchronizer> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Implementation of <see cref="IEntitySynchronizer{ТContext}.SynchronizeCollection{TEntity, TDTO, TId}(ICollection{TEntity}, ICollection{TDTO}, Operation)"/>
        /// Using three private methods to execute every operation.
        /// <list type="number">
        /// <item><see cref="ConvertAndAddCollectionAsync{TEntity, TDTO, TId}(ICollection{TEntity}, ICollection{TDTO})"/></item>
        /// <item><see cref="UpdateCollectionAsync{TEntity, TDTO, TId}(ICollection{TDTO})"/></item>
        /// <item><see cref="DeleteCollectionAsync{TEntity, TDTO, TId}(ICollection{TDTO})"/></item>
        /// </list>
        /// </summary>
        /// <typeparam name="TEntity">Provides entity type (class).</typeparam>
        /// <typeparam name="TDTO">Provides DTO type bound to given TEntity</typeparam>
        /// <typeparam name="TId">The ID type (value type) for the given Entity and DTO.</typeparam>
        /// <param name="entityList">List of entities in DB. (Recommendation: retrieve them right before calling the method.)</param>
        /// <param name="dtoList">List of DTOs that will be used.</param>
        /// <param name="operation">Instruction for the synchronizer (Add, Update, Delete).</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Throws when either entityList or dtoList is null.</exception>
        public async Task<SynchronizeResult> SynchronizeCollection<TEntity, TDTO, TId>(ICollection<TEntity> entityList, ICollection<TDTO> dtoList, Operation operation)
            where TId : IEquatable<TId>
            where TEntity : class, IEntity<TId>
            where TDTO : class, IDTO<TId, TEntity>
        {
            if (entityList == null || dtoList == null) 
            {
                throw new ArgumentNullException("Entity list or DTO list cannot be null.");
            }
            try
            {
                return operation switch
                {
                    Operation.Add => await ConvertAndAddCollectionAsync<TEntity, TDTO, TId>(entityList, dtoList),
                    Operation.Update => await UpdateCollectionAsync<TEntity, TDTO, TId>(dtoList),
                    Operation.Delete => await DeleteCollectionAsync<TEntity, TDTO, TId>(dtoList),
                    _ => new SynchronizeResult("Invalid operation !", succseed: false)
                };
            }
            catch (Exception err)
            {
                _logger.LogError($"[ERROR] {err.Message}\n{err.StackTrace}");
                return new SynchronizeResult($"Synchronization failed: {err.Message}", succseed: false);
            }
        }
        /// <summary>
        /// Extracting only IDs from entityList in a HashSet for better performance,
        /// then creates new entities from given dto list using mapper and add them in the DB.
        /// </summary>
        /// <typeparam name="TEntity">Provides entity type (class).</typeparam>
        /// <typeparam name="TDTO">Provides DTO type bound to given TEntity</typeparam>
        /// <typeparam name="TId">The ID type (value type) for the given Entity and DTO.</typeparam>
        /// <param name="entityList">List of entities in DB. (Recommendation: retrieve them right before calling the method.)</param>
        /// <param name="dtoList">List of DTOs that will be used.</param>
        /// <returns></returns>
        private async Task<SynchronizeResult> ConvertAndAddCollectionAsync<TEntity, TDTO, TId>(ICollection<TEntity> entityList, ICollection<TDTO> dtoList)
            where TId : IEquatable<TId>
            where TEntity : class, IEntity<TId>
            where TDTO : class, IDTO<TId, TEntity>
        {
            try
            {
                var existingIds = new HashSet<TId>(entityList.Select(e => e.Id));
                var newEntities = dtoList
                    .Where(dto => !existingIds.Contains(dto.Id))
                    .Select(dto => _mapper!.Map<TEntity>(dto))
                    .ToList();

                if(newEntities.Count > 0)
                {
                    await _context.AddRangeAsync(newEntities);
                    var addedEntities = await _context.SaveChangesAsync();
                    return new SynchronizeResult($"{addedEntities} entities added to DB.", succseed: true);
                }
                return new SynchronizeResult("No entities were added.", succseed: false);
            }
            catch (Exception err) 
            {
                _logger.LogError($"[ERROR] {err.Message}\n{err.StackTrace}");
                return new SynchronizeResult("Something went wrong while adding entities.", succseed: false);
            }
        }
        /// <summary>
        /// Takes collection of DTOs, checks if they exist in DB and update only the changed values.
        /// </summary>
        /// <typeparam name="TEntity">Provides entity type (class).</typeparam>
        /// <typeparam name="TDTO">Provides DTO type bound to given TEntity</typeparam>
        /// <typeparam name="TId">The ID type (value type) for the given Entity and DTO.</typeparam>
        /// <param name="dtoList">List of DTOs that will be used.</param>
        /// <returns></returns>
        private async Task<SynchronizeResult> UpdateCollectionAsync<TEntity, TDTO, TId>(ICollection<TDTO> dtoList)
            where TId : IEquatable<TId>
            where TEntity : class, IEntity<TId>
            where TDTO : class, IDTO<TId, TEntity>
        {
            try
            {
                var dtoIds = dtoList.Select(dto => dto.Id).ToList();
                var entitiesInDb = await _context.Set<TEntity>()
                    .Where(e => dtoIds.Contains(e.Id))
                    .ToListAsync();

                int updatedEntities = 0;
                foreach(var entity in entitiesInDb)
                {
                    var dto = dtoList.FirstOrDefault(dto => dto.Id.Equals(entity.Id));
                    if (dto != null)
                    {
                        _context.Entry(entity).CurrentValues.SetValues(dto);
                        updatedEntities++;
                    }
                }

                if(updatedEntities > 0)
                {
                    await _context.SaveChangesAsync();
                    return new SynchronizeResult($"{updatedEntities} entities updated successfully !", succseed: true);
                }

                return new SynchronizeResult("Couldn't find any entities to update.", succseed: false);
            }
            catch (Exception err)
            {
                _logger.LogError($"[ERROR] {err.Message}\n{err.StackTrace}");
                return new SynchronizeResult("Something went wrong while updating entities !", succseed: false);
            }
        }
        /// <summary>
        /// Takes collection of DTOs, check if they exist in DB and delete them all.
        /// </summary>
        /// <typeparam name="TEntity">Provides entity type (class).</typeparam>
        /// <typeparam name="TDTO">Provides DTO type bound to given TEntity</typeparam>
        /// <typeparam name="TId">The ID type (value type) for the given Entity and DTO.</typeparam>
        /// <param name="dtoList">List of DTOs that will be used.</param>
        /// <returns></returns>
        private async Task<SynchronizeResult> DeleteCollectionAsync<TEntity, TDTO, TId>(ICollection<TDTO> dtoList)
            where TId : IEquatable<TId>
            where TEntity : class, IEntity<TId>
            where TDTO : class, IDTO<TId, TEntity>
        {
            try
            {
                var dtoIds = dtoList.Select(dto => dto.Id).ToList();
                var entitiesToDelete = await _context.Set<TEntity>()
                    .Where(e => dtoIds.Contains(e.Id))
                    .ToListAsync();

                if (entitiesToDelete.Count > 0)
                {
                    _context.RemoveRange(entitiesToDelete);
                    var deletedEntities = await _context.SaveChangesAsync();
                    return new SynchronizeResult($"{deletedEntities} entities deleted successfully from database.", succseed: true);
                }
                return new SynchronizeResult("No entities to delete !", succseed: false);
            }
            catch(Exception err)
            {
                _logger.LogError($"[ERROR] {err.Message}\n{err.StackTrace}");
                return new SynchronizeResult("Something went wrong while deleting entities !", succseed: false );
            }
        }
    }
}
