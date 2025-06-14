using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.Infra
{
    public static class DebugerChecker
    {
        public static readonly bool IsDebug;

        static DebugerChecker()
        {
#if DEBUG
            IsDebug = true;
#else
        IsDebug = false;
#endif
        }
    }
}
