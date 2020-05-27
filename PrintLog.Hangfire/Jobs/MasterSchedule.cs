using Exceptionless;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using PrintLog.DAL.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrintLog.Hangfire.Jobs {
    public class MasterSchedule {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public MasterSchedule(IServiceScopeFactory serviceScopeFactory) {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public void UpdateJob() {
            try {
                using (var scope = serviceScopeFactory.CreateScope()) {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PrintlogDbContext>();
                    var prisma = scope.ServiceProvider.GetRequiredService<Prisma>();
                    var shiki = scope.ServiceProvider.GetRequiredService<Shiki>();

                    var lstPrinter = dbContext.MasterTypes.Where(w => w.ParentTypeId == 1);

                    foreach (var printer in lstPrinter) {
                        if (printer.RefNo == 2000) {
                            RecurringJob.AddOrUpdate(printer.TypeName, () => prisma.ReadLog(printer.TypeId), "*/10 11 * * *", TimeZoneInfo.Local, "default");
                        } else if (printer.RefNo == 2001) {
                            RecurringJob.AddOrUpdate(printer.TypeName, () => shiki.ReadLog(printer.TypeId), "*/10 11 * * *", TimeZoneInfo.Local, "default");
                        }
                    }
                }
            } catch (Exception ex) {
                ex.ToExceptionless().Submit();
            }
        }
    }
}
