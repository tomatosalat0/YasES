using System;
using YasES.Core;

namespace YasES.Persistance.Sqlite
{
    internal interface IPageReader : IDisposable
    {
        void Start();

        bool TryReadNext(out IStoredEventMessage message);
    }
}
