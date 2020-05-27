using Exceptionless;
using Microsoft.Extensions.DependencyInjection;
using PrintLog.DAL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrintLog.Hangfire.Jobs {
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

                    var detailExpires = dbContext.PrinterLogDetails.Where(w => w.DateCreated <= limit);
                    dbContext.PrinterLogDetails.RemoveRange(detailExpires);

                    var mainExpires = dbContext.PrinterLogs.Where(w => w.DateCreated <= limit);
                    dbContext.PrinterLogs.RemoveRange(mainExpires);

                    dbContext.SaveChanges();
                }
            } catch(Exception ex) {
                ex.ToExceptionless().Submit();
            }
        }
    }
}
