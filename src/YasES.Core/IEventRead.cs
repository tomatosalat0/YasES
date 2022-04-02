using System.Collections.Generic;

namespace YasES.Core
{
    public interface IEventRead
    {
        IEnumerable<IReadEventMessage> Read(ReadPredicate predicate);
    }
}
