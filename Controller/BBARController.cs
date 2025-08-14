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
using System.Text.Json.Serialization;

using HIS_DB_Lib;
namespace DB2VM
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBARController : ControllerBase
    {
        private class BBAR_OPD_Class
        {
            [JsonPropertyName("KEY")]
            public string PRI_KEY { get; set; }
            [JsonPropertyName("BARCODE")]
            public string 藥袋條碼 { get; set; }
            [JsonPropertyName("MEDCODE")]
            public string 藥品碼 { get; set; }
            [JsonPropertyName("MEDNAME")]
            public string 藥品名稱 { get; set; }
            [JsonPropertyName("PROCDTTM")]
            public string 開方日期 { get; set; }
            [JsonPropertyName("PATCODE")]
            public string 病歷號 { get; set; }
            [JsonPropertyName("PATNAME")]
            public string 病人姓名 { get; set; }
            [JsonPropertyName("BRYPE")]
            public string 藥局代碼 { get; set; }
            [JsonPropertyName("BRYPR")]
            public string 藥袋類型 { get; set; }
            [JsonPropertyName("DOS")]
            public string 批序 { get; set; }
            [JsonPropertyName("VALUE")]
            public string 交易量 { get; set; }
            [JsonPropertyName("SD")]
            public string 單次劑量 { get; set; }
            [JsonPropertyName("DUNIT")]
            public string 劑量單位 { get; set; }
            [JsonPropertyName("RROUTE")]
            public string 途徑 { get; set; }
            [JsonPropertyName("FREQ")]
            public string 頻次 { get; set; }
            [JsonPropertyName("NOTE")]
            public string 備註 { get; set; }
            [JsonPropertyName("CTYPE")]
            public string 費用別 { get; set; }
            [JsonPropertyName("WARD")]
            public string 病房 { get; set; }
            [JsonPropertyName("BEDNO")]
            public string 床號 { get; set; }
            [JsonPropertyName("ORDERSTART")]
            public string 醫囑開始時間 { get; set; }
            [JsonPropertyName("ORDEREND")]
            public string 結方日期 { get; set; }
            [JsonPropertyName("ODRDATE")]
            public string 展藥時間 { get; set; }
        }
        static string MySQL_server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
        static string MySQL_database = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
        static string MySQL_userid = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
        static string MySQL_password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
        static string MySQL_port = $"{ConfigurationManager.AppSettings["MySQL_port"]}";

        public class Icp_Date : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                DateTime date0 = x.StringToDateTime();
                DateTime date1 = y.StringToDateTime();
                return DateTime.Compare(date1, date0);

            }
        }
        [Route("{BarCode}")]
        [HttpGet]
        public string Get(string? BarCode)
        {
            //if (BarCode.Substring(BarCode.Length - 5, 5).StringIsInt32() == false)
            //{
            //    BarCode = BarCode.Substring(0, 11);
            //}
            return Get_Code(BarCode , "BarCode");
        }
        [Route("code/{BarCode}")]
        [HttpGet]
        public string Get_Code(string? BarCode ,string? IsCodeMode)
        {
            BarCode = BarCode.Trim();
            returnData returnData = new returnData();
            returnData.Method = "OPD/{BarCode}";
            if (BarCode.StringIsEmpty())
            {
                returnData.Code = -200;
                returnData.Result = "條碼空白!";
                return returnData.JsonSerializationt();
            }
            try
            {
                string code = "";
                BarCode = BarCode.Split(' ')[0].Trim();
                if (IsCodeMode.StringIsEmpty() == true)
                {
                     BarCode = BarCode.Split(' ')[0].Trim();

                    //if (BarCode.Substring(BarCode.Length - 5, 5).StringIsInt32() == false && BarCode.Length > 11)
                    //{
                    //    code = BarCode.Substring(11, BarCode.Length - 11);
                    //}
                }
                //if (BarCode.Length >= 11)
                //{
                //    code = BarCode.Substring(11, BarCode.Length - 11);
                //}


                SQLControl sQLControl_醫囑資料 = new SQLControl(MySQL_server, MySQL_database, "order_list", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);
                string URL = "";
                string URL_startup = BarCode.Substring(0,1);
                if (URL_startup == "O" || URL_startup == "E") URL = $"http://10.125.254.212/MedDataAPI/api/GetOpdMed?BARCODE={BarCode}";
                else
                {
                    URL_startup = "I";
                    URL = $"http://10.125.254.212/MedDataAPI/api/GetIpdMed?BARCODE={BarCode}";
                }
                string json = Net.WEBApiGet(URL);
                List<BBAR_OPD_Class> bBAR_OPD_Classes = json.JsonDeserializet<List<BBAR_OPD_Class>>();
                for(int i = 0; i < bBAR_OPD_Classes.Count; i++)
                {
                    if(bBAR_OPD_Classes[i].展藥時間.Length == 7)
                    {
                        string year = bBAR_OPD_Classes[i].展藥時間.Substring(0, 3);
                        string month = bBAR_OPD_Classes[i].展藥時間.Substring(3, 2);
                        string day = bBAR_OPD_Classes[i].展藥時間.Substring(5, 2);

                        year = (year.StringToInt32() + 1911).ToString();
                        bBAR_OPD_Classes[i].展藥時間 = $"{year}-{month}-{day}";
                    }
                    else
                    {
                        bBAR_OPD_Classes[i].展藥時間 = "";
                    }
                }
                List<object[]> list_value = bBAR_OPD_Classes.ClassToSQL<BBAR_OPD_Class, enum_醫囑資料>();

                if(URL_startup == "I")
                {
                    List<object[]> list_value_buf = new List<object[]>();
                    list_value_buf = list_value.GetRowsInDate((int)enum_醫囑資料.展藥時間, DateTime.Now);
                    if (list_value_buf.Count == 0)
                    {
                        List<string> dates = (from temp in list_value
                                              select temp[(int)enum_醫囑資料.展藥時間].ToDateString()).Distinct().ToList();
                        dates.Sort(new Icp_Date());
                        if (dates.Count > 0)
                        {
                            list_value_buf = list_value.GetRowsInDate((int)enum_醫囑資料.展藥時間, dates[0].StringToDateTime());
                            list_value = list_value_buf;
                        }
                    }
                    else
                    {
                        list_value = list_value_buf;
                    }
                }
                
                List<object[]> list_醫囑資料 = sQLControl_醫囑資料.GetRowsByDefult(null, (int)enum_醫囑資料.藥袋條碼, BarCode);
                List<object[]> list_醫囑資料_buf = new List<object[]>();
                List<object[]> list_醫囑資料_add = new List<object[]>();
                List<object[]> list_醫囑資料_replace = new List<object[]>();
                if (URL_startup == "E" && BarCode.Length >= 11)
                {
                    code = BarCode.Substring(11, BarCode.Length - 11);
                    if (code.StringIsEmpty() == false)
                    {
                        string result_code = code.Split(' ')[0].Trim();
                        list_value = list_value.GetRows((int)enum_醫囑資料.藥品碼, result_code);
                        //orderClasses = orderClasses.Where(item => item.藥品碼 == result_code).ToList();
                    }
                }
                
 

                for (int i = 0; i < list_value.Count; i++)
                {
                    string 藥品碼 = list_value[i][(int)enum_醫囑資料.藥品碼].ObjectToString();
                    string 頻次 = list_value[i][(int)enum_醫囑資料.頻次].ObjectToString();
                    string 開方日期 = list_value[i][(int)enum_醫囑資料.開方日期].ObjectToString();
                    string str_交易量 = list_value[i][(int)enum_醫囑資料.交易量].ObjectToString();
                    string 展藥時間 = list_value[i][(int)enum_醫囑資料.展藥時間].ToDateString();
                    if(展藥時間.StringIsEmpty())
                    {
                        展藥時間 = 開方日期;
                    }
                    string PRI_KEY = $"{BarCode}{藥品碼}{頻次}{開方日期}{str_交易量}";
                    if(URL_startup == "I")
                    {
                        PRI_KEY = $"{BarCode}{藥品碼}{頻次}{開方日期}{str_交易量}{展藥時間}";
                    }
                    double temp0 = -1;
                    if (double.TryParse(str_交易量, out temp0) == false) continue;

                    int 交易量 = (int)Math.Ceiling(temp0) * -1;
      
                    list_醫囑資料_buf = (from temp in list_醫囑資料
                                     where temp[(int)enum_醫囑資料.PRI_KEY].ObjectToString().Contains(PRI_KEY)
                                     select temp).ToList();
                    if (list_醫囑資料_buf.Count == 0)
                    {
                        list_value[i][(int)enum_醫囑資料.GUID] = Guid.NewGuid().ToString();
                        if (list_value[i][(int)enum_醫囑資料.結方日期].ToDateTimeString().StringIsEmpty()) list_value[i][(int)enum_醫囑資料.結方日期] = DateTime.Now.ToDateTimeString();
                        list_value[i][(int)enum_醫囑資料.狀態] = enum_醫囑資料_狀態.未過帳.GetEnumName();
                        list_value[i][(int)enum_醫囑資料.藥袋條碼] = BarCode;
                        list_value[i][(int)enum_醫囑資料.交易量] = 交易量.ToString();
                        list_value[i][(int)enum_醫囑資料.展藥時間] = 展藥時間;
                        list_value[i][(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                        list_value[i][(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString();
                        list_value[i][(int)enum_醫囑資料.過帳時間] = DateTime.MinValue.ToDateTimeString();
                        list_value[i][(int)enum_醫囑資料.就醫時間] = DateTime.MinValue.ToDateTimeString();

                        list_醫囑資料_add.Add(list_value[i]);
                    }
                    else
                    {
                        list_value[i][(int)enum_醫囑資料.GUID] = list_醫囑資料_buf[0][(int)enum_醫囑資料.GUID].ObjectToString();
                        if (list_value[i][(int)enum_醫囑資料.結方日期].ToDateTimeString().StringIsEmpty()) list_value[i][(int)enum_醫囑資料.結方日期] = DateTime.Now.ToDateTimeString();

                        list_value[i][(int)enum_醫囑資料.PRI_KEY] = PRI_KEY;
                        list_value[i][(int)enum_醫囑資料.藥袋條碼] = BarCode;
                        list_value[i][(int)enum_醫囑資料.展藥時間] = 展藥時間;
                        list_value[i][(int)enum_醫囑資料.交易量] = 交易量.ToString();
                        list_value[i][(int)enum_醫囑資料.產出時間] = DateTime.Now.ToDateTimeString();
                        list_value[i][(int)enum_醫囑資料.狀態] = list_醫囑資料_buf[0][(int)enum_醫囑資料.狀態];
                        if (list_value[i][(int)enum_醫囑資料.過帳時間].ToDateTimeString().StringIsEmpty())
                        {
                            list_value[i][(int)enum_醫囑資料.過帳時間] = DateTime.MinValue.ToDateTimeString();
                        }
                        if (list_value[i][(int)enum_醫囑資料.就醫時間].ToDateTimeString().StringIsEmpty())
                        {
                            list_value[i][(int)enum_醫囑資料.就醫時間] = DateTime.MinValue.ToDateTimeString();
                        }



                        list_醫囑資料_replace.Add(list_value[i]);
                    }
                }
                List<OrderClass> orderClasses = list_value.SQLToClass<OrderClass, enum_醫囑資料>();

                

                Task.Run(new Action(delegate
                {
                    if (list_醫囑資料_add.Count > 0) sQLControl_醫囑資料.AddRows(null, list_醫囑資料_add);
                    if (list_醫囑資料_replace.Count > 0) sQLControl_醫囑資料.UpdateByDefulteExtra(null, list_醫囑資料_replace);
                }));

                

                returnData.Code = 200;
                returnData.Result = $"條碼掃描成功! 新增<{list_醫囑資料_add.Count }>筆 修改<{list_醫囑資料_replace.Count}>筆";
                returnData.Data = orderClasses;
                return returnData.JsonSerializationt(true);
            }
            catch(Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }
          

        }
    }
}
