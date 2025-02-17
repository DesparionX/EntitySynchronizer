using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.EntitySynchronizer
{
    /// <summary>
    /// Simple class used as return type for the <see cref="EntitySynchronizer"/>.
    /// Provides message and task status.
    /// </summary>
    public class SynchronizeResult
    {
        public virtual string? Message { get; }
        public virtual bool Succeeded { get; }

        public SynchronizeResult(string? message, bool succseed)
        {
            Message = message;
            Succeeded = succseed;
        }
    }
}
