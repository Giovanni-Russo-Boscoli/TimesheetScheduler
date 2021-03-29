using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimesheetScheduler.Interface
{
    public interface IUtilService
    {
        decimal FetchVat();

        double FetchRequiredHours();

    }
}
