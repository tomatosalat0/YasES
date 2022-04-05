using System.Collections.Generic;

namespace YasES.Core
{
    public interface IEventRead
    {
        IEnumerable<IStoredEventMessage> Read(ReadPredicate predicate);
    }
}
