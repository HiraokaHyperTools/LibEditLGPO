using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibEditLGPO.Helpers
{
    internal class ComObj<T> : IDisposable where T : class
    {
        private readonly T _instance;
        private bool _disposedValue;

        public ComObj(T instance)
        {
            _instance = instance;
        }

        public T Value => _instance;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Marshal.ReleaseComObject(_instance);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
