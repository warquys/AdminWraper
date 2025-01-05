using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminWrapper;

public class ApplicationHander : IDisposable
{
    #region Properties & Variables
    private bool disposedValue;
    public readonly AppDomain domain;
    #endregion

    #region Constructor & Destructor
    public ApplicationHander(AppDomain domain)
    {
        this.domain = domain;
        domain.ProcessExit += Domain_ProcessExit;
        domain.UnhandledException += Domain_UnhandledException;
    }

    // ~ApplicationHander()
    // {
    //     // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
    //     Dispose(disposing: false);
    // }
    #endregion

    #region Methods
    public void Dispose()
    {
        // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                domain.ProcessExit -= Domain_ProcessExit;
                domain.UnhandledException -= Domain_UnhandledException;
            }

            disposedValue = true;
        }
    }


    private void Domain_ProcessExit(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Domain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        throw new NotImplementedException();
    }
    #endregion
}
