// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Firefly.Client.Common;
using Firefly.Client.Control;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Firefly.Client.Service
{
    public class IRTekHttpClient
    {
        private static readonly HttpClient downlodClient = new HttpClient();

        private static readonly ILog _logger = LogManager.GetLogger(typeof(IRTekHttpClient));
        private static HttpClient _client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(Constants.TIMEOUT_REQUEST_DEFAULT) };

        public static async Task<ApiResult<T>> SendAsyncWithNoAuth<T>(HttpMethod httpMethod, string url, object obj = null, int timeout = Constants.TIMEOUT_REQUEST_DEFAULT)
        {
            ApiResult<T> result = new ApiResult<T>();

            try
            {
                string responseContent = "";
                HttpRequestMessage request = new HttpRequestMessage(httpMethod, url);
                if (obj != null)
                {
                    StringContent data = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                    request.Content = data;
                }
                using (HttpResponseMessage res = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!res.IsSuccessStatusCode)
                    {
                        result.Code = (int)res.StatusCode;
                        return result;
                    }

                    responseContent = await res.Content.ReadAsStringAsync();
                    JObject jobject = JObject.Parse(responseContent);
                    if (jobject != null && jobject.ContainsKey("code") && !string.Equals(jobject["code"].ToString(), "0"))
                    {
                        try
                        {
                            result.Code = Convert.ToInt32(jobject["code"]);
                            result.Msg = jobject["msg"].ToString();
                            if (DateTime.TryParse(jobject["timestamp"].ToString(), out DateTime timestamp))
                            {
                                result.Timestamp = timestamp;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        result = JsonConvert.DeserializeObject<ApiResult<T>>(responseContent);
                    }
                }
                string objStr = "";
                if (obj != null)
                {
                    objStr = JsonConvert.SerializeObject(obj);
                }
                _logger.Debug($"SendApi {httpMethod} {url} \nrequest {objStr}\nresponse {responseContent}");
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException());
                //throw new NetworkException(url, JsonConvert.SerializeObject(obj));
                result.Code = ResponseCode.FAILED;
            }

            return result;
        }

        public static async Task<ApiResult<T>> SendAsync<T>(HttpMethod httpMethod, string url, object obj = null, int timeout = Constants.TIMEOUT_REQUEST_DEFAULT)
        {
            ApiResult<T> result = new ApiResult<T>();

            try
            {
                string responseContent = "";
                HttpRequestMessage request = new HttpRequestMessage(httpMethod, url);
                if (obj != null)
                {
                    StringContent data = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                    request.Content = data;
                }

                using (HttpResponseMessage res = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!res.IsSuccessStatusCode)
                    {
                        result.Code = (int)res.StatusCode;
                        return result;
                    }

                    responseContent = await res.Content.ReadAsStringAsync();
                    JObject jobject = JObject.Parse(responseContent);
                    if (jobject != null && jobject.ContainsKey("code") && !string.Equals(jobject["code"].ToString(), "0"))
                    {
                        try
                        {
                            result.Code = Convert.ToInt32(jobject["code"]);
                            result.Msg = jobject["msg"].ToString();
                            if (DateTime.TryParse(jobject["timestamp"].ToString(), out DateTime timestamp))
                            {
                                result.Timestamp = timestamp;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        result = JsonConvert.DeserializeObject<ApiResult<T>>(responseContent);
                    }
                }
                string objStr = "";
                if (obj != null)
                {
                    objStr = JsonConvert.SerializeObject(obj);
                }
                _logger.Debug($"SendApi {httpMethod} {url} \nrequest {objStr}\nresponse {responseContent}");
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException());
                //throw new NetworkException(url, JsonConvert.SerializeObject(obj));
                result.Code = ResponseCode.FAILED;
            }

            return result;
        }

        public static async Task<PaginationApiResult<T>> SendPaginationAsync<T>(HttpMethod httpMethod, string url, object obj = null, int timeout = Constants.TIMEOUT_REQUEST_DEFAULT)
        {
            PaginationApiResult<T> result = new PaginationApiResult<T>();

            try
            {
                string responseContent = "";
                HttpRequestMessage request = new HttpRequestMessage(httpMethod, url);
                if (obj != null)
                {
                    StringContent data = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                    request.Content = data;
                }
                string objStr = "";
                if (obj != null)
                {
                    objStr = JsonConvert.SerializeObject(obj);
                }
                _logger.Debug($"SendApi {httpMethod} {url}" +
                    $"\n\rrequest {objStr}");
                using (HttpResponseMessage res = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!res.IsSuccessStatusCode)
                    {
                        result.Code = (int)res.StatusCode;
                        return result;
                    }

                    responseContent = await res.Content.ReadAsStringAsync();
                    JObject jobject = JObject.Parse(responseContent);
                    if (jobject != null && jobject.ContainsKey("code") && !string.Equals(jobject["code"].ToString(), "0"))
                    {
                        try
                        {
                            result.Code = Convert.ToInt32(jobject["code"]);
                            result.Msg = jobject["msg"].ToString();
                            if (DateTime.TryParse(jobject["timestamp"].ToString(), out DateTime timestamp))
                            {
                                result.Timestamp = timestamp;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        result = JsonConvert.DeserializeObject<PaginationApiResult<T>>(responseContent);
                    }
                }
                _logger.Debug($"response {responseContent}");
            }
            catch (Exception e)
            {
                _logger.Error(e);
                //throw new NetworkException(url, JsonConvert.SerializeObject(obj));
            }

            return result;
        }

        public static async Task<FileDataObject> DownlodFileAsync(string url)
        {
            try
            {
                if (LiteDb.LocalImagePath.TryGetExistingLocalPath(url, out string localPath))
                {
                    byte[] localBytes = File.ReadAllBytes(localPath);
                    return new FileDataObject(Path.GetFileName(localPath), localBytes);
                }

                HttpResponseMessage res = await downlodClient.GetAsync(url);
                using (Stream contentStream = await res.Content.ReadAsStreamAsync())
                {
                    if (string.IsNullOrWhiteSpace(res.Content.Headers?.ContentDisposition?.FileName))
                    {
                        return new FileDataObject(string.Empty, null);
                    }
                    byte[] bs = new byte[contentStream.Length];
                    contentStream.Read(bs, 0, bs.Length);
                    return new FileDataObject(res.Content.Headers.ContentDisposition.FileName, bs);
                }
            }
            catch
            {
                return new FileDataObject(string.Empty, null);
            }
        }

        public static async Task<ApiResult<T>> UploadFileAsync<T>(string url, MultipartFormDataContent content)
        {
            ApiResult<T> result = new ApiResult<T>();

            try
            {
                string responseContent = "";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;
                using (HttpResponseMessage res = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!res.IsSuccessStatusCode)
                    {
                        result.Code = (int)res.StatusCode;
                        return result;
                    }

                    responseContent = await res.Content.ReadAsStringAsync();
                    JObject jobject = JObject.Parse(responseContent);
                    if (jobject != null && jobject.ContainsKey("code") && !string.Equals(jobject["code"].ToString(), "0"))
                    {
                        try
                        {
                            result.Code = Convert.ToInt32(jobject["code"]);
                            result.Msg = jobject["msg"].ToString();
                            if (DateTime.TryParse(jobject["timestamp"].ToString(), out DateTime timestamp))
                            {
                                result.Timestamp = timestamp;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        result = JsonConvert.DeserializeObject<ApiResult<T>>(responseContent);
                    }
                }
                _logger.Debug($"UploadFile {url} \nrequest \nresponse {responseContent}");
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException());
                //throw new NetworkException(url, JsonConvert.SerializeObject(obj));
                result.Code = ResponseCode.FAILED;
            }

            return result;
        }

        public static string GetParamStr(object param)
        {
            return JsonConvert.SerializeObject(param).Replace("\":", "=").Replace(",\"", "&").Replace("\"", "").Replace("{", "").Replace("}", "");
        }

    }

    public class NetworkException : Exception
    {
        private readonly string _url;
        private readonly string _param;

        public NetworkException()
        {
        }

        public NetworkException(string url, string param)
        {
            _url = url;
            _param = param;
        }

        public override string ToString()
        {
            return $"Url: {_url} Param: {_param}\n{base.ToString()}";
        }
    }

    public class ApiResult<T>
    {
        public ApiResult() : this(default(T))
        { }

        public ApiResult(T data = default(T)) : this(data, ResponseCode.SUCCESS)
        {
            Data = data;
        }

        public ApiResult(T data, int code)
        {
            Data = data;
            Code = code;
        }

        #region Properties

        /// <summary>
        /// 响应码
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>
        /// 响应信息
        /// </summary>
        [JsonProperty("msg")]
        public string Msg { get; set; }

        /// <summary>
        /// 时间戳(毫秒)
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 返回结果
        /// </summary>
        [JsonProperty("data")]
        public T Data { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => Code == ResponseCode.SUCCESS;

        public bool IsUnauthorized => Code == ResponseCode.ADMIN_CONFIG_ACCOUNT_UNAUTHORIZED;

        public bool IsForbidden => Code == ResponseCode.ADMIN_CONFIG_ACCOUNT_FORBIDDEN;

        #endregion Properties
    }

    public class PaginationApiResult<T> : ApiResult<T>
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        [JsonProperty("pageNo")]
        public int PageNo { get; set; }

        /// <summary>
        /// 每页数量
        /// </summary>
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class ResponseCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        public const int SUCCESS = 0;

        /// <summary>
        /// 通用错误码
        /// </summary>
        public const int FAILED = -1;

        #region 110000 ~ 119999: admin server

        #region 110000 ~ 110099: device

        public const int ADMIN_DEVICE_GET_LIST_FAILED = 110000;

        public const int ADMIN_DEVICE_GET_FAILED = 110001;

        public const int ADMIN_DEVICE_ADD_FAILED = 110002;

        public const int ADMIN_DEVICE_EXISTED = 110003;

        public const int ADMIN_DEVICE_UPDATE_FAILED = 110004;

        public const int ADMIN_DEVICE_DELETE_FAILED = 110005;

        public const int ADMIN_DEVICE_AGENT_DELETE_FAILED = 110006;

        public const int ADMIN_DEVICE_NOT_EXISTED = 110007;

        public const int ADMIN_DEVICE_AGENT_NOT_EXISTED = 110008;

        public const int ADMIN_DEVICE_AGENT_OFFLINE = 110009;

        public const int ADMIN_DEVICE_AGENT_ADD_FAILED = 110010;

        public const int ADMIN_DEVICE_AGENT_UPDATE_FAILED = 110011;

        #endregion 110000 ~ 110099: device

        #region 110100 ~ 110199: agent manage

        public const int ADMIN_AGENT_GET_LIST_FAILED = 110100;

        public const int ADMIN_AGENT_GET_FAILED = 110101;

        public const int ADMIN_AGENT_DEVICE_GET_LIST_FAILED = 110102;

        public const int ADMIN_AGENT_ADD_FAILED = 110103;

        public const int ADMIN_AGENT_EXISTED = 110104;

        public const int ADMIN_AGENT_UPDATE_FAILED = 110105;

        public const int ADMIN_AGENT_DELETE_FAILED = 110106;

        public const int ADMIN_AGENT_OFFLINE = 110107;

        public const int ADMIN_AGENT_NOT_EXISTED = 110108;

        #endregion 110100 ~ 110199: agent manage

        #region 110200 ~ 110299： region manage

        public const int ADMIN_REGION_GET_LIST_FAILED = 110200;

        public const int ADMIN_REGION_GET_FAILED = 110201;

        public const int ADMIN_REGION_ADD_FAILED = 110202;

        public const int ADMIN_REGION_EXISTED = 110203;

        public const int ADMIN_REGION_UPDATE_FAILED = 110204;

        public const int ADMIN_REGION_DELETE_FALIED = 110205;

        public const int ADMIN_REGION_NOT_EXISTED = 110206;

        #endregion 110200 ~ 110299： region manage

        #region 110300 ~ 110399: event data

        public const int ADMIN_EVENT_ALARM_GET_LIST_FAILED = 110300;

        public const int ADMIN_EVENT_ALARM_ADD_FAILED = 110301;

        public const int ADMIN_EVENT_ALARM_UPDATE_FALIED = 110302;

        #endregion 110300 ~ 110399: event data

        #region 110400 ~ 110499: config

        public const int ADMIN_CONFIG_SYSTEM_GET_FAILED = 110400;

        public const int ADMIN_CONFIG_SYSTEM_UPDATE_FAILED = 110401;

        public const int ADMIN_CONFIG_ALARM_GET_FAILED = 110402;

        public const int ADMIN_CONFIG_ALARM_UPDATE_FAILED = 110403;

        /// <summary>
        /// 对应的报警设备不存在
        /// </summary>
        public const int ADMIN_CONFIG_ALARM_NOT_EXISTED = 110404;

        #endregion 110400 ~ 110499: config

        #region 110500 ~ 599: Account

        /// <summary>
        /// 用户不存在
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_NOT_EXISTED = 110500;

        /// <summary>
        /// 用户密码错误
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_INVALID_PASSWORD = 110501;

        /// <summary>
        /// Token错误
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_UNAUTHORIZED = 110502;

        /// <summary>
        /// 用户权限不足
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_FORBIDDEN = 110503;

        /// <summary>
        /// 用户创建失败，用户已存在
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_EXISTED = 110504;

        /// <summary>
        /// 用户类型不存在
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_ROLE_NOT_EXISTED = 110505;

        /// <summary>
        /// 用户创建失败
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_CREATE_USER_FAILED = 110506;

        /// <summary>
        /// 用户删除失败
        /// </summary>
        public const int ADMIN_CONFIG_ACCOUNT_DELETE_USER_FAILED = 110507;

        #endregion 110500 ~ 599: Account

        #endregion 110000 ~ 119999: admin server

        #region 120000 ~ 129999: storage server

        #region 120000 ~ 120099: server manager

        public const int STORE_SERVER_REGISTER_NOT_MATCH = 120000;

        public const int STORE_SERVER_REGISTER_FAILD = 120001;

        #endregion 120000 ~ 120099: server manager

        #region 120100 ~ 120199: nvr manager

        public const int STORE_NVR_GET_LIST_FAILED = 120100;

        public const int STORE_NVR_GET_FAILED = 120101;

        public const int STORE_NVR_ADD_FAILED = 120102;

        public const int STORE_NVR_EXISTED = 120103;

        public const int STORE_NVR_UPDATE_FAILED = 120104;

        public const int STORE_NVR_DELETE_FAILED = 120105;

        public const int STORE_NVR_NOT_EXISTED = 120106;

        #endregion 120100 ~ 120199: nvr manager

        #region 120200 ~ 120299: device match channel

        public const int STORE_DEVICE_CHANNEL_ADD_FAILED = 120200;

        public const int STORE_DEVICE_CHANNEL_DELETE_FAILED = 120201;

        public const int STORE_DEVICE_CHANNEL_UPDATE_FAILED = 120202;

        public const int STORE_CHANNEL_GET_LIST_FAILED = 120203;

        public const int STORE_CHANNEL_GET_LIST_BY_DEVICE_FAILED = 120204;

        #endregion 120200 ~ 120299: device match channel

        #endregion 120000 ~ 129999: storage server

        #region 200000 ~ 209999: fire monitor server

        #region 200000 ~ 200099: server manager

        public const int FIRE_SERVER_REGISTER_NOT_MATCH = 200000;

        public const int FIRE_SERVER_REGISTER_FAILD = 200001;

        #endregion 200000 ~ 200099: server manager

        #region 200100 ~ 200199: fire data query

        public const int FIRE_DATA_GET_LIST_FAILED = 200100;

        public const int FIRE_DATA_GET_IMAGE_FAILED = 200101;

        public const int FIRE_DATA_GET_IMAGE_ZIP_FAILED = 200102;

        #endregion 200100 ~ 200199: fire data query

        #region 200200 ~ 200299: device manager

        public const int FIRE_DEVICE_GET_CONF_FAILED = 200200;

        public const int FIRE_DEVICE_UPDATE_CONF_FAILED = 200201;

        public const int FIRE_DEVICE_ADD_FAILED = 200202;

        public const int FIRE_DEVICE_DELETE_FAILED = 200203;

        public const int FIRE_DEVICE_ENGINE_GET_PARAM_FAILED = 200204;

        public const int FIRE_DEVICE_ENGINE_GET_STATUS_FAILED = 200205;

        public const int FIRE_DEVICE_ENGINE_SET_PRESET_FAILED = 200206;

        public const int FIRE_DEVICE_ENGINE_PTZ_CONTROL_FAILED = 200207;

        public const int FIRE_DEVICE_NOT_EXISTED = 200208;

        public const int FIRE_DEVICE_INVALID_PRESET = 200209;

        public const int FIRE_DEVICE_INVALID_LINE = 200210;

        public const int FIRE_DEVICE_LIMITED_LINE = 200211;

        public const int FIRE_DEVICE_GET_INFO_FAILED = 200212;

        public const int FIRE_DEVICE_OVERRIDE_CONFIG_FAILED = 200213;

        public const int FIRE_DEVICE_INVALID_SPOT = 200214;

        public const int FIRE_DEVICE_LIMITED_SPOT = 200215;

        public const int FIRE_DEVICE_INVALID_REGION = 200216;

        public const int FIRE_DEVICE_LIMITED_REGION = 200217;

        public const int FIRE_DEVICE_INVALID_REF_TEMP = 200218;

        public const int FIRE_DEVICE_INVALID_OFFSET_TEMP = 200219;

        public const int FIRE_DEVICE_INVALID_SHIELD_MARKER = 200220;

        public const int FIRE_DEVICE_INVALID_MARKER = 200221;

        public const int FIRE_DEVICE_INVALID_ALARM_OUTPUT = 200222;

        public const int FIRE_DEVICE_ABILITY_NOT_MATCH = 200223;

        public const int FIRE_DEVICE_INVALID_SENSITIVITY = 200224;

        public const int FIRE_DEVICE_INVALID_PTZ = 200225;

        #endregion 200200 ~ 200299: device manager

        #region 200300 ~ 200399: web interface

        public const int FIRE_EVENT_SUBSCRIBE_FAILED = 200300;

        public const int FIRE_EVENT_SUBSCRIBE_INVALID_EVENT = 200301;

        public const int FIRE_EVENT_UNSUBSCRIBE_FAILED = 200302;

        public const int FIRE_EVENT_DEVICE_NOT_EXISTED = 200303;

        public const int FIRE_EVENT_DEVICE_OFFLINE = 200304;

        public const int FIRE_EVENT_CONNECTION_NOT_EXISTED = 200305;

        #endregion 200300 ~ 200399: web interface

        #endregion 200000 ~ 209999: fire monitor server

        #region 210000~219999: equipment monitor server

        #region 210000 ~ 210099: server manager

        public const int EQUIPMENT_REGIST_SERVETYPE_MISMATCH = 210000;

        public const int EQUIPMENT_SERVER_REGISTER_FAILD = 210001;

        #endregion 210000 ~ 210099: server manager

        #region 210100~210199: equipment data query

        public const int EQUIPMENT_EQUIPMENT_DATA_GET_FAILED = 210100;

        public const int EQUIPMENT_EQUIPMENT_DEFECT_GET_FAILED = 210101;

        #endregion 210100~210199: equipment data query

        #region 210200~210299: device manager

        public const int EQUIPMENT_DEVICE_GET_CONF_FAILED = 210200;

        public const int EQUIPMENT_DEVICE_UPDATE_CONF_FAILED = 210201;

        public const int EQUIPMENT_DEVICE_ADD_FAILED = 210202;

        public const int EQUIPMENT_DEVICE_DELETE_FAILED = 210203;

        public const int EQUIPMENT_DEVICE_ENGINE_GET_PARAM_FAILED = 210204;

        public const int EQUIPMENT_DEVICE_ENGINE_GET_STATUS_FAILED = 210205;

        public const int EQUIPMENT_DEVICE_ENGINE_SET_PRESET_FAILED = 210206;

        public const int EQUIPMENT_DEVICE_ENGINE_PTZ_CONTROL_FAILED = 210207;

        public const int EQUIPMENT_DEVICE_NOT_EXISTED = 210208;

        public const int EQUIPMENT_DEVICE_INVALID_PRESET = 210209;

        public const int EQUIPMENT_DEVICE_INVALID_LINE = 210210;

        public const int EQUIPMENT_DEVICE_LIMITED_LINE = 210211;

        public const int EQUIPMENT_DEVICE_GET_INFO_FAILED = 210212;

        public const int EQUIPMENT_DEVICE_OVERRIDE_CONFIG_FAILED = 210213;

        public const int EQUIPMENT_DEVICE_INVALID_SPOT = 210214;

        public const int EQUIPMENT_DEVICE_LIMITED_SPOT = 210215;

        public const int EQUIPMENT_DEVICE_INVALID_REGION = 210216;

        public const int EQUIPMENT_DEVICE_LIMITED_REGION = 210217;

        public const int EQUIPMENT_DEVICE_INVALID_REF_TEMP = 210218;

        public const int EQUIPMENT_DEVICE_INVALID_OFFSET_TEMP = 210219;

        public const int EQUIPMENT_DEVICE_INVALID_SHIELD_MARKER = 210220;

        public const int EQUIPMENT_DEVICE_INVALID_MARKER = 210221;

        public const int EQUIPMENT_DEVICE_INVALID_ALARM_OUTPUT = 210222;

        public const int EQUIPMENT_DEVICE_ABILITY_NOT_MATCH = 210223;

        public const int EQUIPMENT_DEVICE_INVALID_SENSITIVITY = 210224;

        public const int EQUIPMENT_DEVICE_INVALID_PTZ = 210225;

        public const int EQUIPMENT_DEVICE_SET_INSTANT_TASK_FAILED = 210226;

        #endregion 210200~210299: device manager

        #region 210300~200399: web interface

        public const int EQUIPMENT_EVENT_SUBSCRIBE_FAILED = 210300;

        public const int EQUIPMENT_EVENT_SUBSCRIBE_INVALID_EVENT = 210301;

        public const int EQUIPMENT_EVENT_UNSUBSCRIBE_FAILED = 210302;

        public const int EQUIPMENT_EVENT_DEVICE_NOT_EXISTED = 210303;

        public const int EQUIPMENT_EVENT_DEVICE_OFFLINE = 210304;

        public const int EQUIPMENT_EVENT_CONNECTION_NOT_EXISTED = 210305;

        #endregion 210300~200399: web interface

        #region 210400~210499: ledger management

        public const int EQUIPMENT_REGION_ADD_FAILED = 210400;

        public const int EQUIPMENT_REGION_DELETED_FAILED = 210401;

        public const int EQUIPMENT_REGION_UPDATE_FAILED = 210402;

        public const int EQUIPMENT_REGION_GET_FAILED = 210403;

        public const int EQUIPMENT_REGION_GET_LIST_FAILED = 210404;

        public const int EQUIPMENT_REGION_NOT_EXIST = 210405;

        public const int EQUIPMENT_EQUIPMENT_ADD_FAILED = 210406;

        public const int EQUIPMENT_EQUIPMENT_DELETED_FAILED = 210407;

        public const int EQUIPMENT_EQUIPMENT_UPDATE_FAILED = 210408;

        public const int EQUIPMENT_EQUIPMENT_GET_FAILED = 210409;

        public const int EQUIPMENT_EQUIPMENT_GET_LIST_FAILED = 210410;

        public const int EQUIPMENT_EQUIPMENT_NOT_EXIST = 210411;

        public const int EQUIPMENT_POINT_ADD_FAILED = 210412;

        public const int EQUIPMENT_POINT_DELETED_FAILED = 210413;

        public const int EQUIPMENT_POINT_UPDATE_FAILED = 210414;

        public const int EQUIPMENT_POINT_GET_FAILED = 210415;

        public const int EQUIPMENT_POINT_NOT_EXIST = 210416;

        public const int EQUIPMENT_REGION_EXISTED = 210417;

        public const int EQUIPMENT_REGION_PARENT_NOT_EXIST = 210418;

        public const int EQUIPMENT_REGION_CHILD_EXISTED = 210419;

        public const int EQUIPMENT_REGION_EQUIPMENT_EXISTED = 210420;

        public const int EQUIPMENT_EQUIPMENT_EXISTED = 210421;

        public const int EQUIPMENT_EQUIPMENT_POINT_EXISTED = 210422;

        public const int EQUIPMENT_POINT_EXISTED = 210423;

        #endregion 210400~210499: ledger management

        #endregion 210000~219999: equipment monitor server

        #region 220000 ~ 229999: environment monitor server

        #region 200000 ~ 200099: server manager

        public const int ENVIRONMENT_SERVER_REGISTER_NOT_MATCH = 220000;

        public const int ENVIRONMENT_SERVER_REGISTER_FAILED = 220001;

        #endregion 200000 ~ 200099: server manager

        #region 200100 ~ 200199: fire data query

        public const int ENVIRONMENT_DATA_GET_LIST_FAILED = 220100;

        #endregion 200100 ~ 200199: fire data query

        #region 200200 ~ 200299: device manager

        public const int ENVIRONMENT_DEVICE_GET_CONF_FAILED = 220200;

        public const int ENVIRONMENT_DEVICE_UPDATE_CONF_FAILED = 220201;

        public const int ENVIRONMENT_DEVICE_ADD_FAILED = 220202;

        public const int ENVIRONMENT_DEVICE_DELETE_FAILED = 220203;

        public const int ENVIRONMENT_DEVICE_ENGINE_GET_PARAM_FAILED = 220204;

        public const int ENVIRONMENT_DEVICE_ENGINE_GET_STATUS_FAILED = 220205;

        public const int ENVIRONMENT_DEVICE_NOT_EXISTED = 220206;

        public const int ENVIRONMENT_DEVICE_GET_INFO_FAILED = 220207;

        public const int ENVIRONMENT_DEVICE_OVERRIDE_CONFIG_FAILED = 220208;

        public const int ENVIRONMENT_DEVICE_ABILITY_NOT_MATCH = 220209;

        public const int ENVIRONMENT_DEVICE_CHANNEL_NOT_MATCH = 220210;

        #endregion 200200 ~ 200299: device manager

        #region 200300 ~ 200399: web interface

        public const int ENVIRONMENT_EVENT_SUBSCRIBE_FAILED = 220300;

        public const int ENVIRONMENT_EVENT_SUBSCRIBE_INVALID_EVENT = 220301;

        public const int ENVIRONMENT_EVENT_UNSUBSCRIBE_FAILED = 220302;

        public const int ENVIRONMENT_EVENT_DEVICE_NOT_EXISTED = 220303;

        public const int ENVIRONMENT_EVENT_DEVICE_OFFLINE = 220304;

        public const int ENVIRONMENT_EVENT_CONNECTION_NOT_EXISTED = 220305;

        #endregion 200300 ~ 200399: web interface

        #endregion 220000 ~ 229999: environment monitor server

        #region 250000 ~ 259999: pd monitor server

        #region 250000 ~ 250099: server manager

        public const int PD_SERVER_REGISTER_NOT_MATCH = 250000;

        public const int PD_SERVER_REGISTER_FAILD = 250001;

        #endregion 250000 ~ 250099: server manager

        #region 250200 ~ 250299: device manager

        public const int PD_DEVICE_GET_CONF_FAILED = 250200;

        public const int PD_DEVICE_UPDATE_CONF_FAILED = 250201;

        public const int PD_DEVICE_ADD_FAILED = 250202;

        public const int PD_DEVICE_DELETE_FAILED = 250203;

        public const int PD_DEVICE_ENGINE_GET_PARAM_FAILED = 250204;

        public const int PD_DEVICE_ENGINE_GET_STATUS_FAILED = 250205;

        public const int PD_DEVICE_NOT_EXISTED = 250208;

        public const int PD_DEVICE_GET_INFO_FAILED = 250212;

        public const int PD_DEVICE_OVERRIDE_CONFIG_FAILED = 250213;

        public const int PD_DEVICE_ABILITY_NOT_MATCH = 250223;

        public const int PD_DEVICE_UPDATE_FAILED = 250224;

        #endregion 250200 ~ 250299: device manager

        #region 200300 ~ 200399: web interface

        public const int PD_EVENT_SUBSCRIBE_FAILED = 250300;

        public const int PD_EVENT_SUBSCRIBE_INVALID_EVENT = 250301;

        public const int PD_EVENT_UNSUBSCRIBE_FAILED = 250302;

        public const int PD_EVENT_DEVICE_NOT_EXISTED = 250303;

        public const int PD_EVENT_DEVICE_OFFLINE = 250304;

        public const int PD_EVENT_CONNECTION_NOT_EXISTED = 250305;

        #endregion 200300 ~ 200399: web interface

        #endregion 250000 ~ 259999: pd monitor server
    }
}