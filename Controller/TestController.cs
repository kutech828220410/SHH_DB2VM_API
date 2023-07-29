using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Data;
using System.Configuration;
using Basic;
using Oracle.ManagedDataAccess.Client;
using HIS_DB_Lib;
namespace DB2VM
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
   
        
        // GET api/values
        [HttpGet]
        public string Get()
        {


            var localIpAddress = HttpContext.Connection.LocalIpAddress?.ToString();
            var localPort = HttpContext.Connection.LocalPort;
            var protocol = HttpContext.Request.IsHttps ? "https" : "http";
            returnData returnData = new returnData();
            returnData.Code = 200;
            returnData.Result = $"Api test sucess!{protocol}://{localIpAddress}:{localPort}";

            string DB = ConfigurationManager.AppSettings["DB"];
            string Server = ConfigurationManager.AppSettings["Server"];
            string VM_Server = ConfigurationManager.AppSettings["VM_Server"];
            string VM_DB = ConfigurationManager.AppSettings["VM_DB"];

            List<string> strs = new List<string>();
            strs.Add($"local Server : {Server}");
            strs.Add($"local Database : {DB}");
            strs.Add($"VM Server : {VM_Server}");
            strs.Add($"VM Database : {VM_DB}");


            returnData.Data = strs;


            return returnData.JsonSerializationt(true);


        }


    }
}
