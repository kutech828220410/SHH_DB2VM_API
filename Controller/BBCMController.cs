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
using SQLUI;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using HIS_DB_Lib;
using System.Text.Json.Serialization;
namespace DB2VM.Controller
{

    public class BBCMClass
    {
        [JsonPropertyName("MEDCODE")]
        public string 藥品碼 { get; set; }
        [JsonPropertyName("MEDNAME")]
        public string 門診名稱 { get; set; }
        [JsonPropertyName("ENGNAME")]
        public string 藥品學名 { get; set; }
        [JsonPropertyName("UDNAME")]
        public string 住院名稱 { get; set; }
        [JsonPropertyName("EASYNAME")]
        public string 藥品簡名 { get; set; }
        [JsonPropertyName("UNIT")]
        public string 包裝數量 { get; set; }
        [JsonPropertyName("UDPKG")]
        public string 包裝單位_住院 { get; set; }
        [JsonPropertyName("OPDPKG")]
        public string 包裝單位_門診 { get; set; }
        [JsonPropertyName("ISEMG")]
        public string 警訊藥品 { get; set; }
        [JsonPropertyName("DRUGKIND")]
        public string 類別 { get; set; }
        [JsonPropertyName("RESTRIC")]
        public string 管制級別 { get; set; }
        [JsonPropertyName("SKDIACODE")]
        public string 扣庫代碼 { get; set; }
        [JsonPropertyName("UD_STATUS")]
        public string 開檔狀態_住院 { get; set; }
        [JsonPropertyName("OPD_STATUS")]
        public string 開檔狀態_門診 { get; set; }

    }


    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBCMController : ControllerBase
    {
        private enum enum_pharmacy
        {
            OPD,
            UD,
        }
        private enum enum_STATUS : int
        {
            常備 = 1,
            暫時缺藥,
            已取消,
            停用中,
        }
        static string MySQL_server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
        static string MySQL_database = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
        static string MySQL_userid = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
        static string MySQL_password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
        static string MySQL_port = $"{ConfigurationManager.AppSettings["MySQL_port"]}";
        [Route("{value}")]
        [HttpGet]
        public string Get(string? Code , string value)
        {
            if (value.StringIsEmpty()) return "pharmacy is empty!";
            SQLControl sQLControl_BBCM = new SQLControl(MySQL_server, "dbvm", "medicine_page_cloud", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);

            string URL = "http://10.125.254.212/MedDataAPI/api/GetMedData";
            string jsonString = Basic.Net.WEBApiGet($"{URL}");
            List<BBCMClass> bBCMClasses = jsonString.JsonDeserializet<List<BBCMClass>>();
            List<BBCMClass> bBCMClasses_buf = new List<BBCMClass>();
            if (Code.StringIsEmpty() == false)
            {
                bBCMClasses_buf = (from temp in bBCMClasses
                                   where temp.藥品碼 == Code
                                   select temp).ToList();
            }
            else
            {
                bBCMClasses_buf = bBCMClasses;
            }
            string json_result = "";
            if(value == "OPD")
            {
                json_result = Function_UpdateMed(sQLControl_BBCM, bBCMClasses_buf, enum_pharmacy.OPD);

            }
            if (value == "UD")
            {
                json_result = Function_UpdateMed(sQLControl_BBCM, bBCMClasses_buf, enum_pharmacy.UD);

            }
            return json_result;
        }

        private string Function_UpdateMed(SQLControl sQLControl, List<BBCMClass> bBCMClasses , enum_pharmacy pharmacy)
        {
            CheckCreatTable(sQLControl);
            List<object[]> list_value = sQLControl.GetAllRows(null);
            List<object[]> list_value_buf = new List<object[]>();
            List<object[]> list_value_add = new List<object[]>();
            List<object[]> list_value_replace = new List<object[]>();
            List<object[]> list_value_all = new List<object[]>();
            for (int i = 0; i < bBCMClasses.Count; i++)
            {
                string 藥品碼 = bBCMClasses[i].藥品碼;
                list_value_buf = list_value.GetRows((int)enum_雲端藥檔.藥品碼, 藥品碼);

                object[] value = new object[new enum_雲端藥檔().GetLength()];
           
                value[(int)enum_雲端藥檔.藥品碼] = bBCMClasses[i].藥品碼;
                value[(int)enum_雲端藥檔.料號] = bBCMClasses[i].扣庫代碼;
                value[(int)enum_雲端藥檔.藥品學名] = bBCMClasses[i].藥品學名;
                value[(int)enum_雲端藥檔.中文名稱] = bBCMClasses[i].藥品學名;
                value[(int)enum_雲端藥檔.包裝數量] = bBCMClasses[i].包裝數量;
                if (bBCMClasses[i].警訊藥品 == "Y") bBCMClasses[i].警訊藥品 = true.ToString();
                else bBCMClasses[i].警訊藥品 = false.ToString();
                value[(int)enum_雲端藥檔.警訊藥品] = bBCMClasses[i].警訊藥品;
                value[(int)enum_雲端藥檔.類別] = bBCMClasses[i].類別;
                if (bBCMClasses[i].管制級別 == "M1") bBCMClasses[i].管制級別 = "1";
                if (bBCMClasses[i].管制級別 == "M2") bBCMClasses[i].管制級別 = "2";
                if (bBCMClasses[i].管制級別 == "M3") bBCMClasses[i].管制級別 = "3";
                if (bBCMClasses[i].管制級別 == "M4") bBCMClasses[i].管制級別 = "4";
                value[(int)enum_雲端藥檔.管制級別] = bBCMClasses[i].管制級別;
                if (pharmacy == enum_pharmacy.OPD)
                {
                    value[(int)enum_雲端藥檔.藥品名稱] = bBCMClasses[i].門診名稱;
                    value[(int)enum_雲端藥檔.包裝單位] = bBCMClasses[i].包裝單位_門診;
                    if (bBCMClasses[i].開檔狀態_門診.StringToInt32() >= 0)
                    {
                        enum_STATUS enum_STATUS = (enum_STATUS)bBCMClasses[i].開檔狀態_門診.StringToInt32();
                        value[(int)enum_雲端藥檔.開檔狀態] = enum_STATUS.GetEnumName();
                    }
                }
                else if (pharmacy == enum_pharmacy.UD)
                {
                    value[(int)enum_雲端藥檔.藥品名稱] = bBCMClasses[i].住院名稱;
                    value[(int)enum_雲端藥檔.包裝單位] = bBCMClasses[i].包裝單位_住院;
                    if (bBCMClasses[i].開檔狀態_住院.StringToInt32() >= 0)
                    {
                        enum_STATUS enum_STATUS = (enum_STATUS)bBCMClasses[i].開檔狀態_住院.StringToInt32();
                        value[(int)enum_雲端藥檔.開檔狀態] = enum_STATUS.GetEnumName();
                    }
                }

                if (list_value_buf.Count == 0)
                {
                    value[(int)enum_雲端藥檔.GUID] = Guid.NewGuid().ToString();
                    list_value_add.Add(value);
                }
                else
                {
                    value[(int)enum_雲端藥檔.GUID] = list_value_buf[0][(int)enum_雲端藥檔.GUID].ObjectToString();
                    list_value_replace.Add(value);
                }
    

            }
            if (list_value_add.Count > 0) sQLControl.AddRows(null, list_value_add);
            if (list_value_replace.Count > 0) sQLControl.UpdateByDefulteExtra(null, list_value_replace);
            list_value_all.LockAdd(list_value_add);
            list_value_all.LockAdd(list_value_replace);
            List<medClass> medClasses = list_value_all.SQLToClass<medClass, enum_雲端藥檔>();
            return medClasses.JsonSerializationt(true);
        }
        private void CheckCreatTable(SQLControl sQLControl)
        {
            Table table = new Table("medicine_page_cloud");
            table.AddColumnList("GUID", Table.StringType.VARCHAR, 50, Table.IndexType.PRIMARY);
            table.AddColumnList("藥品碼", Table.StringType.VARCHAR, 20, Table.IndexType.INDEX);
            table.AddColumnList("料號", Table.StringType.VARCHAR, 20, Table.IndexType.INDEX);
            table.AddColumnList("中文名稱", Table.StringType.VARCHAR, 300, Table.IndexType.None);
            table.AddColumnList("藥品名稱", Table.StringType.VARCHAR, 300, Table.IndexType.None);
            table.AddColumnList("藥品學名", Table.StringType.VARCHAR, 300, Table.IndexType.None);
            table.AddColumnList("藥品群組", Table.StringType.VARCHAR, 300, Table.IndexType.None);
            table.AddColumnList("健保碼", Table.StringType.VARCHAR, 50, Table.IndexType.None);
            table.AddColumnList("包裝單位", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("包裝數量", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("最小包裝單位", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("最小包裝數量", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("藥品條碼1", Table.StringType.VARCHAR, 200, Table.IndexType.None);
            table.AddColumnList("藥品條碼2", Table.StringType.TEXT, 200, Table.IndexType.None);
            table.AddColumnList("警訊藥品", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("高價藥品", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("生物製劑", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("管制級別", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table.AddColumnList("類別", Table.StringType.VARCHAR, 500, Table.IndexType.None);
            table.AddColumnList("開檔狀態", Table.StringType.VARCHAR, 10, Table.IndexType.None);

            if (!sQLControl.IsTableCreat()) sQLControl.CreatTable(table);
            else sQLControl.CheckAllColumnName(table, true);
        }
    }
}
