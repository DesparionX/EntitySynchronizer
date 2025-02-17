using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.EntitySynchronizer
{
    public interface IDTO<T, TEntity> where T : IEquatable<T> where TEntity : class, IEntity<T>
    {
        T Id { get; set; }
        TEntity Entity { get; }
        
    }
}
