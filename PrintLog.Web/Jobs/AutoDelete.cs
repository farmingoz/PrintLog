using Exceptionless;
using Microsoft.Extensions.DependencyInjection;
using PrintLog.DAL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrintLog.Web.Jobs {
    public class AutoDelete {
        private readonly IServiceProvider serviceProvider;

        public AutoDelete(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
        }

        public void DeleteLog() {
            try {
                using (var scope = serviceProvider.CreateScope()) {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PrintlogDbContext>();
                    DateTime limit = DateTime.Now.AddDays(-90).Date;

                    var mainExpires = dbContext.MachineLogs.Where(w => w.DateCreated <= limit);
                    dbContext.MachineLogs.RemoveRange(mainExpires);

                    var detailExpires = dbContext.MachineLogDetails.Where(w => w.DateCreated <= limit);
                    dbContext.MachineLogDetails.RemoveRange(detailExpires);
                }
            } catch(Exception ex) {
                ex.ToExceptionless().Submit();
            }
        }
    }
}
