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

                    var lstShiki = dbContext.MasterTypes.Where(w => w.ParentTypeId == 1000);
                    foreach (var shikiPrinter in lstShiki) {
                        RecurringJob.AddOrUpdate(shikiPrinter.TypeName, () => shiki.ReadLog(shikiPrinter.TypeId), "*/10 11 * * *", TimeZoneInfo.Local, "default");
                    }

                    var lstPrisma = dbContext.MasterTypes.Where(w => w.ParentTypeId == 2000);
                    foreach (var prismaPrinter in lstPrisma) {
                        RecurringJob.AddOrUpdate(prismaPrinter.TypeName, () => prisma.ReadLog(prismaPrinter.TypeId), "*/10 11 * * *", TimeZoneInfo.Local, "default");
                    }
                }
            } catch (Exception ex) {
                ex.ToExceptionless().Submit();
            }
        }
    }
}
