using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrintLog.Web.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class PrintLogController : ControllerBase {

        private readonly ILogger<PrintLogController> _logger;

        public PrintLogController(ILogger<PrintLogController> logger) {
            _logger = logger;
        }

        
    }
}
