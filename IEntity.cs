using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.EntitySynchronizer
{
    public interface IEntity<T> where T : IEquatable<T>
    {
        public T Id { get; set; }
    }
}
