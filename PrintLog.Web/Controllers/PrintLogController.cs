using AutoMapper;
using Exceptionless;
using Gofive.Common.Core.Models;
using LinqKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrintLog.DAL.Data;
using PrintLog.DAL.Models;
using PrintLog.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrintLog.Web.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class PrintLogController : ControllerBase {
        private readonly IServiceProvider serviceProvider;
        private readonly IMapper mapper;

        public PrintLogController(IServiceProvider serviceProvider, IMapper mapper) {
            this.serviceProvider = serviceProvider;
            this.mapper = mapper;
        }

        [HttpPost("DataTable")]
        public ActionResult DataTable(DataTableRequestModel<PrinterLog> model) {
            if (model == null) return BadRequest();
            try {
                using (var scope = serviceProvider.CreateScope()) {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PrintlogDbContext>();

                    ExpressionStarter<PrinterLog> predicate = PredicateBuilder.New<PrinterLog>();
                    //predicate.And(a => a.);


                    IEnumerable<PrinterLog> data = dbContext.PrinterLogs.Where(predicate);
                    DataTableResponse<PrinterLog> response = new DataTableResponse<PrinterLog>() {
                        DataSource = data.Take(model.Take).Skip(model.Skip),
                        Count = data.Count()
                    };

                    return Ok(response);
                }
            } catch (Exception ex) {
                ex.ToExceptionless().Submit();
                return BadRequest();
            }
        }
    }
}
