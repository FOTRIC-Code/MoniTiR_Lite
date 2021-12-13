// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Firefly.Client.Service.LiteDb;

namespace Firefly.Client.Service.Abstractions
{
    /// <summary>
    /// Reflection bridge to Firefly.Device.FireMonitor (net461) when deployed beside MoniTiRLite.exe.
    /// Avoids compile-time type conflicts (e.g. StreamType) between Client.Service and Firefly.Device.
    /// </summary>
    internal static class InProcFireEngineBridge
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InProcFireEngineBridge));
        private static readonly ConcurrentDictionary<string, object> Engines =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConditionalWeakTable<object, object> HandlersAttached =
            new ConditionalWeakTable<object, object>();
        private static readonly object HandlerMarker = new object();

        private static bool _initialized;
        private static object _factory;
        private static MethodInfo _tryGetOrAdd;
        private static MethodInfo _tryRemove;
        private static MethodInfo _tryUpdate;
        private static MethodInfo _tryGet;
        private static Type _deviceLoadBoType;
        private static Type _deviceBoType;
        private static Type _deviceAddressBoType;
        private static Type _deviceLoadArgsType;
        private static IFireCallbackDispatcher _dispatcher;
        private static int _fireServerId;

        public static void TryInitialize(IFireCallbackDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            if (_initialized)
            {
                return;
            }

            try
            {
                _fireServerId = LiteLocalAgentIds.Fire;
                Assembly deviceAsm = TryLoadAssembly("Firefly.Device");
                Assembly fireAsm = TryLoadAssembly("Firefly.Device.FireMonitor");
                if (deviceAsm == null || fireAsm == null)
                {
                    Logger.Info("Fire engine assemblies not found beside Client; in-proc host runs without native engines.");
                    _initialized = true;
                    return;
                }

                _deviceLoadBoType = deviceAsm.GetType("Firefly.Device.DeviceLoadBO");
                _deviceBoType = deviceAsm.GetType("Firefly.Device.DeviceBO");
                _deviceAddressBoType = deviceAsm.GetType("Firefly.Device.DeviceAddressBO");
                _deviceLoadArgsType = deviceAsm.GetType("Firefly.Device.DeviceLoadArgs");

                Type factoryType = fireAsm.GetType("Firefly.Device.FireMonitor.FireMonitorEngineFactory");
                PropertyInfo instanceProp = factoryType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                _factory = instanceProp?.GetValue(null);
                if (_factory == null)
                {
                    Logger.Warn("FireMonitorEngineFactory.Instance unavailable.");
                    _initialized = true;
                    return;
                }

                Type factoryRuntime = _factory.GetType();
                _tryGetOrAdd = factoryRuntime.GetMethod("TryGetOrAdd", new[] { typeof(string) });
                _tryRemove = factoryRuntime.GetMethod("TryRemove", new[] { typeof(string) });
                _tryUpdate = factoryRuntime.GetMethod("TryUpdate", new[] { typeof(string) });
                _tryGet = factoryRuntime.GetMethod("TryGet");

                FieldInfo loadingField = factoryRuntime.GetField("DeviceLoading");
                if (loadingField != null)
                {
                    Type handlerType = loadingField.FieldType;
                    Delegate handler = CreateDeviceLoadingDelegate(handlerType);
                    loadingField.SetValue(_factory, handler);
                }

                // Attach OnDataEvent before Init/SampleTask starts (otherwise early samples are dropped).
                BindEngineCreated(factoryRuntime, deviceAsm);

                Logger.Info("Bound FireMonitorEngineFactory via reflection.");
            }
            catch (Exception ex)
            {
                Logger.Warn("Engine bridge init failed: " + ex);
            }
            finally
            {
                _initialized = true;
            }
        }

        public static void TryShutdown()
        {
            foreach (string id in Engines.Keys.ToList())
            {
                TryRemoveDevice(id);
            }

            try
            {
                if (_factory is IDisposable d)
                {
                    d.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Engine factory dispose: " + ex.Message);
            }

            _factory = null;
            Engines.Clear();
        }

        public static bool TryEnsureDevice(string deviceId)
        {
            if (_tryGetOrAdd == null || _factory == null || string.IsNullOrEmpty(deviceId))
            {
                return false;
            }

            try
            {
                object result = _tryGetOrAdd.Invoke(_factory, new object[] { deviceId });
                object engine = UnwrapTaskResult(result);
                if (engine == null)
                {
                    return false;
                }

                Engines[deviceId] = engine;
                AttachEngineHandlers(deviceId, engine);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"TryEnsureDevice({deviceId}): {ex.Message}");
                return false;
            }
        }

        public static bool TryUpdateDevice(string deviceId)
        {
            if (_tryUpdate == null || _factory == null || string.IsNullOrEmpty(deviceId))
            {
                return TryEnsureDevice(deviceId);
            }

            try
            {
                if (!Engines.ContainsKey(deviceId))
                {
                    return TryEnsureDevice(deviceId);
                }

                object result = _tryUpdate.Invoke(_factory, new object[] { deviceId });
                object unwrapped = UnwrapTaskResult(result);
                return unwrapped is bool b ? b : true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"TryUpdateDevice({deviceId}): {ex.Message}");
                return false;
            }
        }

        public static void TryRemoveDevice(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            Engines.TryRemove(deviceId, out _);
            if (_tryRemove == null || _factory == null)
            {
                return;
            }

            try
            {
                _tryRemove.Invoke(_factory, new object[] { deviceId });
            }
            catch (Exception ex)
            {
                Logger.Warn($"TryRemoveDevice({deviceId}): {ex.Message}");
            }
        }

        public static DeviceParamDataObject TryGetParam(string deviceId)
        {
            object engine = GetEngine(deviceId);
            if (engine == null)
            {
                return null;
            }

            try
            {
                MethodInfo getParam = engine.GetType().GetMethod("GetParam", Type.EmptyTypes);
                object taskOrResult = getParam?.Invoke(engine, null);
                object param = UnwrapTaskResult(taskOrResult);
                if (param == null)
                {
                    return null;
                }

                // DeviceParamBO property names differ from the HTTP DeviceParamOutputModel JSON
                // contract the Client DTOs expect (deviceModel / softwareAbility / ...).
                return MapDeviceParam(JObject.FromObject(param));
            }
            catch (Exception ex)
            {
                Logger.Warn($"TryGetParam({deviceId}): {ex.Message}");
                return null;
            }
        }

        private static DeviceParamDataObject MapDeviceParam(JObject bo)
        {
            if (bo == null)
            {
                return null;
            }

            JToken software = bo["SoftwareAbilityBO"] ?? bo["softwareAbilityBO"] ?? bo["softwareAbility"];
            JToken hardware = bo["HardwareAbilityBO"] ?? bo["hardwareAbilityBO"] ?? bo["hardwareAbility"];
            JToken models = bo["DeviceInfoBOList"] ?? bo["deviceInfoBOList"] ?? bo["deviceModel"];

            var mapped = new JObject
            {
                ["softwareAbility"] = RemapSoftwareAbility(software as JObject),
                ["hardwareAbility"] = hardware,
                ["deviceModel"] = models ?? new JArray(),
            };
            DeviceParamDataObject result = mapped.ToObject<DeviceParamDataObject>() ?? new DeviceParamDataObject();
            if (result.DeviceModel == null)
            {
                result.DeviceModel = new List<DeviceModelDataObject>();
            }

            return result;
        }

        private static JObject RemapSoftwareAbility(JObject software)
        {
            if (software == null)
            {
                return null;
            }

            // Engine uses PTZSupport; Client DTO expects ptzSupport.
            JToken ptz = software["PTZSupport"] ?? software["ptzSupport"] ?? software["PtzSupport"];
            if (ptz != null)
            {
                software["ptzSupport"] = ptz;
            }

            return software;
        }

        public static DeviceStatusDataObject TryGetStatus(string deviceId)
        {
            object engine = GetEngine(deviceId);
            var status = new DeviceStatusDataObject
            {
                ID = deviceId,
                Status = DeviceStatus.Offline,
            };
            if (engine == null)
            {
                return status;
            }

            try
            {
                MethodInfo getStatus = engine.GetType().GetMethod("GetStatus", Type.EmptyTypes);
                object taskOrResult = getStatus?.Invoke(engine, null);
                object result = UnwrapTaskResult(taskOrResult);
                if (result == null)
                {
                    status.Status = DeviceStatus.Online;
                    return status;
                }

                JObject jo = JObject.FromObject(result);
                status.Status = DeviceStatus.Online;
                JToken running = jo["RunningStatus"] ?? jo["runningStatus"] ?? jo["Status"] ?? jo["status"];
                if (running != null && Enum.TryParse(running.ToString(), true, out DeviceStatus parsed))
                {
                    status.Status = parsed;
                }


                return status;
            }
            catch (Exception ex)
            {
                Logger.Warn($"TryGetStatus({deviceId}): {ex.Message}");
                status.Status = DeviceStatus.Online;
                return status;
            }
        }

        public static DeviceStreamDO TryGetStream(string deviceId, StreamType streamType)
        {
            object engine = GetEngine(deviceId);
            if (engine == null)
            {
                return null;
            }

            try
            {
                MethodInfo getStream = engine.GetType().GetMethod("GetStream");
                if (getStream == null)
                {
                    return null;
                }

                ParameterInfo[] ps = getStream.GetParameters();
                object arg = (int)streamType;
                if (ps.Length == 1 && ps[0].ParameterType.IsEnum)
                {
                    arg = Enum.ToObject(ps[0].ParameterType, (int)streamType);
                }

                object streamBo = getStream.Invoke(engine, new[] { arg });
                if (streamBo == null)
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<DeviceStreamDO>(JsonConvert.SerializeObject(streamBo));
            }
            catch (Exception ex)
            {
                Logger.Warn($"TryGetStream({deviceId}): {ex.Message}");
                return null;
            }
        }

        public static bool TryPtzPreset(string deviceId, PTZPresetDataObject preset)
        {
            return InvokeBool(deviceId, "PTZPreset", methodParams =>
            {
                // HTTP contract uses "data" (PresetConfig); engine BO uses PresetBO.
                JObject jo = preset == null ? new JObject() : JObject.FromObject(preset);
                JToken data = jo["data"] ?? jo["PresetConfig"] ?? jo["presetConfig"];
                if (data != null && data.Type != JTokenType.Null)
                {
                    jo["PresetBO"] = data;
                }

                object bo = jo.ToObject(methodParams[0].ParameterType)
                    ?? Activator.CreateInstance(methodParams[0].ParameterType);
                PropertyInfo presetBo = methodParams[0].ParameterType.GetProperty("PresetBO");
                if (presetBo != null && presetBo.GetValue(bo) == null)
                {
                    presetBo.SetValue(bo, Activator.CreateInstance(presetBo.PropertyType));
                }

                return new[] { bo };
            });
        }

        public static bool TryPtzControl(string deviceId, PTZControlDataObject control)
        {
            return InvokeBool(deviceId, "PTZControl", methodParams =>
            {
                // Client calls the stop flag "stop"; the engine BO calls it "Action".
                JObject jo = JObject.FromObject(control);
                JToken stop = jo["stop"];
                if (stop != null)
                {
                    jo["Action"] = stop;
                }

                return new[] { jo.ToObject(methodParams[0].ParameterType) };
            });
        }

        public static bool TryPtzCruise(string deviceId, PTZCruiseDataObject cruise)
        {
            return InvokeBool(deviceId, "PTZCruise", methodParams =>
                new[] { ToEngineType(cruise, methodParams[0].ParameterType) });
        }

        public static bool TryRestoreCruise(string deviceId)
        {
            return InvokeBool(deviceId, "RestoreCruiseTask", _ => new object[0]);
        }

        public static bool TrySetAlarmOut(string deviceId, List<AlarmOutDataObject> alarmOuts)
        {
            return InvokeBool(deviceId, "SetAlarmOut", methodParams =>
            {
                Type listType = methodParams[0].ParameterType;
                Type boType = listType.GetGenericArguments()[0];
                object list = Activator.CreateInstance(listType);
                MethodInfo add = listType.GetMethod("Add");
                Type commandType = boType.GetProperty("Command")?.PropertyType;

                foreach (AlarmOutDataObject dto in alarmOuts ?? new List<AlarmOutDataObject>())
                {
                    // AlarmOutBO has no parameterless constructor.
                    object command = commandType == null
                        ? (object)(int)dto.Command
                        : Enum.ToObject(commandType, (int)dto.Command);
                    add.Invoke(list, new[] { Activator.CreateInstance(boType, dto.ChannelID, command) });
                }

                return new[] { list };
            });
        }

        public static bool TrySetOsdCustomer(string deviceId, int channelId, OSDCustomerDO osd)
        {
            return InvokeBool(deviceId, "SetOSDCustomer", methodParams =>
                new[] { (object)channelId, ToEngineType(osd, methodParams[1].ParameterType) });
        }

        /// <summary>
        /// Returns the captured file as a local path; in-proc there is no file HTTP route.
        /// </summary>
        public static ImageInfo TryCaptureImage(string deviceId, int channel)
        {
            object engine = GetEngine(deviceId);
            if (engine == null)
            {
                return null;
            }

            try
            {
                MethodInfo capture = engine.GetType().GetMethod("CaptureImage");
                object result = UnwrapTaskResult(capture?.Invoke(engine, new object[] { channel }));
                if (result == null)
                {
                    return null;
                }

                JObject jo = JObject.FromObject(result);
                string root = jo.Value<string>("RootPath") ?? jo.Value<string>("rootPath");
                string file = jo.Value<string>("FilePath") ?? jo.Value<string>("filePath");
                int channelId = jo.Value<int?>("ChannelID") ?? jo.Value<int?>("channelID") ?? channel;
                int fileType = jo.Value<int?>("FileType") ?? jo.Value<int?>("fileType") ?? 0;

                return new ImageInfo
                {
                    ChannelID = channelId,
                    FileType = (FileType)fileType,
                    URL = string.IsNullOrEmpty(root) ? file : System.IO.Path.Combine(root, file ?? string.Empty),
                };
            }
            catch (Exception ex)
            {
                Logger.Warn($"TryCaptureImage({deviceId}): {ex.Message}");
                return null;
            }
        }

        private static bool InvokeBool(string deviceId, string methodName, Func<ParameterInfo[], object[]> argsFactory)
        {
            object engine = GetEngine(deviceId);
            if (engine == null)
            {
                Logger.Warn($"{methodName}({deviceId}): engine not available.");
                return false;
            }

            try
            {
                MethodInfo method = engine.GetType().GetMethod(methodName);
                if (method == null)
                {
                    Logger.Warn($"{methodName}({deviceId}): method not found on engine.");
                    return false;
                }

                object result = UnwrapTaskResult(method.Invoke(engine, argsFactory(method.GetParameters())));

                // void / Task methods report success by not throwing.
                return !(result is bool b) || b;
            }
            catch (Exception ex)
            {
                Logger.Warn($"{methodName}({deviceId}): {(ex.InnerException ?? ex).Message}");
                return false;
            }
        }

        private static object ToEngineType(object dto, Type engineType)
        {
            return dto == null
                ? null
                : JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dto), engineType);
        }

        /// <summary>
        /// Soft recovery: re-ensure engines that disappeared or report offline.
        /// Native AV still kills the process; this only covers soft failures.
        /// </summary>
        public static void WatchdogTick(IEnumerable<string> deviceIds)
        {
            if (_factory == null || deviceIds == null)
            {
                return;
            }

            foreach (string id in deviceIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!Engines.ContainsKey(id))
                {
                    TryEnsureDevice(id);
                }
            }
        }

        private static object GetEngine(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return null;
            }

            if (Engines.TryGetValue(deviceId, out object engine))
            {
                return engine;
            }

            if (_tryGet != null && _factory != null)
            {
                object[] args = { deviceId, null };
                try
                {
                    object ok = _tryGet.Invoke(_factory, args);
                    if (ok is bool b && b && args[1] != null)
                    {
                        Engines[deviceId] = args[1];
                        AttachEngineHandlers(deviceId, args[1]);
                        return args[1];
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static void BindEngineCreated(Type factoryRuntime, Assembly deviceAsm)
        {
            FieldInfo createdField = factoryRuntime.GetField("EngineCreated");
            if (createdField == null)
            {
                return;
            }

            Type deviceEngineType = deviceAsm.GetType("Firefly.Device.DeviceEngine");
            if (deviceEngineType == null)
            {
                return;
            }

            MethodInfo attach = typeof(InProcFireEngineBridge).GetMethod(
                nameof(OnEngineCreated), BindingFlags.NonPublic | BindingFlags.Static);
            ParameterExpression pId = Expression.Parameter(typeof(string), "id");
            ParameterExpression pEng = Expression.Parameter(deviceEngineType, "engine");
            MethodCallExpression call = Expression.Call(
                attach,
                pId,
                Expression.Convert(pEng, typeof(object)));
            LambdaExpression lambda = Expression.Lambda(createdField.FieldType, call, pId, pEng);
            createdField.SetValue(_factory, lambda.Compile());
        }

        private static void OnEngineCreated(string deviceId, object engine)
        {
            if (engine == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(deviceId))
            {
                Engines[deviceId] = engine;
            }

            AttachEngineHandlers(deviceId ?? string.Empty, engine);
        }

        private static void AttachEngineHandlers(string deviceId, object engine)
        {
            try
            {
                if (HandlersAttached.TryGetValue(engine, out _))
                {
                    return;
                }

                HandlersAttached.Add(engine, HandlerMarker);

                Type engineType = engine.GetType();
                FieldInfo monitorField = FindInstanceField(engineType, "OnMonitorEvent");
                FieldInfo dataField = FindInstanceField(engineType, "OnDataEvent");
                FieldInfo netField = FindInstanceField(engineType, "OnNetEvent");
                SetHandlerField(engine, monitorField, CreateVoidObjectHandler(nameof(OnMonitorEventProxy)));
                SetHandlerField(engine, dataField, CreateVoidObjectHandler(nameof(OnDataEventProxy)));
                SetHandlerField(engine, netField, CreateVoidObjectHandler(nameof(OnNetEventProxy)));
            }
            catch (Exception ex)
            {
                Logger.Warn($"AttachEngineHandlers({deviceId}): {ex.Message}");
            }
        }

        private static FieldInfo FindInstanceField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(name, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        }

        private static void SetHandlerField(object target, FieldInfo field, Func<Type, Delegate> factory)
        {
            if (field == null)
            {
                return;
            }

            Delegate our = factory(field.FieldType);
            object existing = field.GetValue(target);
            if (existing == null)
            {
                field.SetValue(target, our);
            }
            else
            {
                field.SetValue(target, Delegate.Combine((Delegate)existing, our));
            }
        }

        private static Func<Type, Delegate> CreateVoidObjectHandler(string methodName)
        {
            return delegateType =>
            {
                MethodInfo core = typeof(InProcFireEngineBridge).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo invoke = delegateType.GetMethod("Invoke");
                ParameterInfo[] ps = invoke.GetParameters();
                ParameterExpression p0 = Expression.Parameter(ps[0].ParameterType, "sender");
                ParameterExpression p1 = Expression.Parameter(ps[1].ParameterType, "args");
                MethodCallExpression call = Expression.Call(
                    core,
                    Expression.Convert(p0, typeof(object)),
                    Expression.Convert(p1, typeof(object)));
                return Expression.Lambda(delegateType, call, p0, p1).Compile();
            };
        }

        private static void OnMonitorEventProxy(object sender, object args)
        {
            try
            {
                if (args == null)
                {
                    return;
                }

                JObject jo = JObject.FromObject(args);
                string deviceId = jo.Value<string>("DeviceID") ?? jo.Value<string>("deviceID");
                string deviceName = jo.Value<string>("DeviceName") ?? jo.Value<string>("deviceName");
                // Newtonsoft serializes DeviceEventType as int by default (Alarm=1); also accept the name.
                string eventType = NormalizeDeviceEventType(jo["EventType"] ?? jo["eventType"]);
                JToken data = jo["Data"] ?? jo["data"];

                if (string.Equals(eventType, "Alarm", StringComparison.OrdinalIgnoreCase))
                {
                    PublishAlarmsFromMonitor(deviceId, deviceName, data, jo["FileList"] ?? jo["fileList"]);
                }

                // Forward a generic event envelope for DeviceManager subscribers.
                var evt = new EventMessageBO
                {
                    DeviceID = deviceId,
                    EventType = MapMonitorEventType(eventType),
                    Event = data == null ? "{}" : data.ToString(Formatting.None),
                };
                _dispatcher?.PublishEvent(_fireServerId, JsonConvert.SerializeObject(evt));
            }
            catch (Exception ex)
            {
                Logger.Error("OnMonitorEventProxy failed.", ex);
            }
        }

        private static void OnDataEventProxy(object sender, object args)
        {
            try
            {
                if (args == null)
                {
                    return;
                }

                JObject jo = JObject.FromObject(args);
                string deviceId = jo.Value<string>("DeviceID") ?? jo.Value<string>("deviceID");
                JToken data = jo["Data"] ?? jo["data"];
                string dataEventType = (jo["DataEventType"] ?? jo["dataEventType"])?.ToString() ?? string.Empty;
                bool isSample = IsSampleDataEvent(dataEventType);

                // Newtonsoft serializes DataEventType as int (Sample=1); also accept the name.
                if (isSample && data != null)
                {
                    PersistSample(data);
                }

                // StatusMonitor / DeviceDetail bind SampleOutputModel.ImageInfo.url.
                // In-proc FireDataBO.ImageInfo only has FilePath+RootPath — rewrite for UI.
                JToken eventData = isSample ? NormalizeSampleEventImageUrls(data) : data;

                var evt = new EventMessageBO
                {
                    DeviceID = deviceId,
                    EventType = EventType.Fire_DataEvent,
                    Event = eventData == null ? "{}" : eventData.ToString(Formatting.None),
                };
                _dispatcher?.PublishEvent(_fireServerId, JsonConvert.SerializeObject(evt));
            }
            catch (Exception ex)
            {
                Logger.Error("OnDataEventProxy failed.", ex);
            }
        }

        /// <summary>
        /// Firefly.Device.DataEventType.Sample == 1. Avoid compile-time Firefly.Device reference.
        /// </summary>
        private static bool IsSampleDataEvent(string dataEventType)
        {
            if (string.IsNullOrWhiteSpace(dataEventType))
            {
                return false;
            }

            if (string.Equals(dataEventType, "Sample", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return int.TryParse(dataEventType, out int value) && value == 1;
        }

        private static void OnNetEventProxy(object sender, object args)
        {
            // Online/offline is reflected via GetStatus / host state; avoid flooding UI.
        }

        private static void PublishAlarmsFromMonitor(string deviceId, string deviceName, JToken data, JToken fileList)
        {
            if (data == null)
            {
                return;
            }

            DateTime alarmTime = data.Value<DateTime?>("AlarmTime")
                ?? data.Value<DateTime?>("alarmTime")
                ?? DateTime.Now;
            DateTime arrivalTime = data.Value<DateTime?>("ArrivalTime")
                ?? data.Value<DateTime?>("arrivalTime")
                ?? DateTime.Now;
            long timestamp = data.Value<long?>("Timestamp") ?? data.Value<long?>("timestamp") ?? 0;
            string name = data.Value<string>("DeviceName") ?? data.Value<string>("deviceName") ?? deviceName;
            string id = data.Value<string>("DeviceID") ?? data.Value<string>("deviceID") ?? deviceId;
            JToken preset = data["Preset"] ?? data["preset"];
            int presetId = preset?.Value<int?>("ID") ?? preset?.Value<int?>("id") ?? 0;
            string presetName = preset?.Value<string>("Name") ?? preset?.Value<string>("name");

            List<AlarmImageInfoDataObject> images = new List<AlarmImageInfoDataObject>();
            if (fileList is JArray files)
            {
                foreach (JToken f in files)
                {
                    string path = f.Value<string>("FilePath") ?? f.Value<string>("filePath");
                    string root = f.Value<string>("RootPath") ?? f.Value<string>("rootPath");
                    int channel = f.Value<int?>("ChannelID") ?? f.Value<int?>("channelID") ?? 0;
                    string url = ResolveLocalImageUrl(root, path);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        images.Add(new AlarmImageInfoDataObject { ChannelID = channel, Url = url });
                    }
                }
            }

            JToken markers = data["MarkerList"] ?? data["markerList"];
            if (markers is JArray arr && arr.Count > 0)
            {
                foreach (JToken marker in arr)
                {
                    string level = NormalizeAlarmLevel(marker["AlarmLevel"] ?? marker["alarmLevel"]);
                    if (string.IsNullOrEmpty(level) || string.Equals(level, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var alarm = new AlarmDO
                    {
                        DeviceID = id,
                        DeviceName = name,
                        Timestamp = timestamp,
                        AlarmTime = alarmTime,
                        ArrivalTime = arrivalTime,
                        AlarmLevel = level,
                        AlarmRegion = marker.Value<string>("MarkerName") ?? marker.Value<string>("markerName"),
                        AlarmType = NormalizeRuleType(marker["RuleType"] ?? marker["ruleType"]),
                        PresetID = presetId,
                        PresetName = presetName,
                        ImageInfo = images,
                        IsConfirmed = false,
                    };
                    _dispatcher?.PublishAlarm(alarm);
                }
            }
            else
            {
                _dispatcher?.PublishAlarm(new AlarmDO
                {
                    DeviceID = id,
                    DeviceName = name,
                    Timestamp = timestamp,
                    AlarmTime = alarmTime,
                    ArrivalTime = arrivalTime,
                    PresetID = presetId,
                    PresetName = presetName,
                    ImageInfo = images,
                    IsConfirmed = false,
                });
            }
        }


        /// <summary>
        /// FireDataBO.ImageInfo uses FilePath/RootPath; client SampleOutputModel expects {channelID,url}.
        /// </summary>
        private static JToken NormalizeSampleEventImageUrls(JToken data)
        {
            if (!(data is JObject obj))
            {
                return data;
            }

            JObject clone = (JObject)obj.DeepClone();
            string root = clone.Value<string>("RootPath") ?? clone.Value<string>("rootPath");
            JToken images = clone["ImageInfo"] ?? clone["imageInfo"];
            if (!(images is JArray imageArr) || imageArr.Count == 0)
            {
                return clone;
            }

            JArray normalized = new JArray();
            foreach (JToken img in imageArr)
            {
                if (img == null || img.Type == JTokenType.Null)
                {
                    continue;
                }

                string path = img.Value<string>("FilePath")
                    ?? img.Value<string>("filePath")
                    ?? img.Value<string>("URL")
                    ?? img.Value<string>("url");
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                string imgRoot = img.Value<string>("RootPath") ?? img.Value<string>("rootPath") ?? root;
                int channelId = img.Value<int?>("ChannelID") ?? img.Value<int?>("channelID") ?? 0;
                string url = ResolveLocalImageUrl(imgRoot, path);
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                normalized.Add(new JObject
                {
                    ["channelID"] = channelId,
                    ["url"] = url,
                });
            }

            clone.Remove("ImageInfo");
            clone["imageInfo"] = normalized;
            return clone;
        }


        private static void PersistSample(JToken data)
        {
            bool needRecordData = data.Value<bool?>("NeedRecordData") ?? data.Value<bool?>("needRecordData") ?? false;
            bool needRecordImage = data.Value<bool?>("NeedRecordImage") ?? data.Value<bool?>("needRecordImage") ?? false;
            // Every sample raises OnDataEvent; only persist when data and/or image recording is due.
            if (!needRecordData && !needRecordImage)
            {
                return;
            }

            string deviceId = data.Value<string>("DeviceID") ?? data.Value<string>("deviceID");
            string deviceName = data.Value<string>("DeviceName") ?? data.Value<string>("deviceName");
            int presetId = data.Value<int?>("PresetID") ?? data.Value<int?>("presetID") ?? 0;
            string presetName = data.Value<string>("PresetName") ?? data.Value<string>("presetName");
            long timestamp = data.Value<long?>("Timestamp") ?? data.Value<long?>("timestamp") ?? 0;
            DateTime happen = data.Value<DateTime?>("HappenTime") ?? data.Value<DateTime?>("happenTime") ?? DateTime.UtcNow;
            string root = data.Value<string>("RootPath") ?? data.Value<string>("rootPath");

            string imagePath;
            string filesJson;
            BuildSampleFiles(data, root, needRecordImage, out imagePath, out filesJson);

            JToken markers = data["MarkerList"] ?? data["markerList"];

            if (markers is JArray arr && arr.Count > 0)
            {
                foreach (JToken marker in arr)
                {
                    LiteFireDataRepository.InsertSample(
                        deviceId,
                        deviceName,
                        presetId,
                        presetName,
                        marker.Value<string>("MarkerName") ?? marker.Value<string>("markerName"),
                        marker.Value<float?>("MaxTemp") ?? marker.Value<float?>("maxTemp"),
                        marker.Value<float?>("MinTemp") ?? marker.Value<float?>("minTemp"),
                        marker.Value<float?>("AvgTemp") ?? marker.Value<float?>("avgTemp"),
                        marker.Value<float?>("RuleTemp") ?? marker.Value<float?>("ruleTemp"),
                        timestamp,
                        happen,
                        root,
                        imagePath,
                        filesJson);
                }
            }
            else
            {
                // No markers (empty preset / temp read failed): still insert one row so photos & samples show in data center.
                LiteFireDataRepository.InsertSample(
                    deviceId, deviceName, presetId, presetName, null,
                    null, null, null, null, timestamp, happen, root, imagePath, filesJson);
            }
        }

        private static void BuildSampleFiles(JToken data, string rootPath, bool needRecordImage, out string imagePath, out string filesJson)
        {
            imagePath = null;
            filesJson = null;

            if (!needRecordImage)
            {
                return;
            }

            JToken images = data["ImageInfo"] ?? data["imageInfo"];
            if (!(images is JArray imageArr) || imageArr.Count == 0)
            {
                return;
            }

            var imageInfos = new List<ImageInfo>();
            foreach (JToken img in imageArr)
            {
                string path = img.Value<string>("FilePath")
                    ?? img.Value<string>("filePath")
                    ?? img.Value<string>("URL")
                    ?? img.Value<string>("url");
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                string imgRoot = img.Value<string>("RootPath") ?? img.Value<string>("rootPath") ?? rootPath ?? string.Empty;
                int channelId = img.Value<int?>("ChannelID") ?? img.Value<int?>("channelID") ?? 0;
                FileType fileType = FileType.None;
                JToken fileTypeToken = img["FileType"] ?? img["fileType"];
                if (fileTypeToken != null)
                {
                    if (fileTypeToken.Type == JTokenType.Integer)
                    {
                        fileType = (FileType)fileTypeToken.Value<int>();
                    }
                    else if (Enum.TryParse(fileTypeToken.ToString(), true, out FileType parsed))
                    {
                        fileType = parsed;
                    }
                }

                try
                {
                    LiteFireDataRepository.InsertFile(path, (int)fileType, imgRoot);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"InsertFile failed for sample image '{path}': {ex.Message}");
                }

                string absolute = LocalImagePath.ResolveAbsolute(imgRoot, path) ?? path;
                if (imagePath == null)
                {
                    imagePath = path;
                }

                imageInfos.Add(new ImageInfo(channelId, absolute, fileType));
            }

            if (imageInfos.Count > 0)
            {
                filesJson = JsonConvert.SerializeObject(imageInfos);
            }
        }

        private static EventType MapMonitorEventType(string eventType)
        {
            if (string.Equals(eventType, "Alarm", StringComparison.OrdinalIgnoreCase))
            {
                return EventType.Fire_AlarmEvent;
            }

            if (string.Equals(eventType, "CruiseStatus", StringComparison.OrdinalIgnoreCase))
            {
                return EventType.Fire_CruiseStatusEvent;
            }

            if (string.Equals(eventType, "CruisePosition", StringComparison.OrdinalIgnoreCase))
            {
                return EventType.Fire_CruisePositionEvent;
            }

            if (string.Equals(eventType, "AlarmInput", StringComparison.OrdinalIgnoreCase))
            {
                return EventType.Fire_AlarmInputEvent;
            }

            return EventType.Fire_DataEvent;
        }

        /// <summary>
        /// Firefly.Device.DeviceEventType: None=0, Alarm=1, AlarmInput=2, VirtualDeviceAlarm=3,
        /// CruiseStatus=4, CruisePosition=5, Defect=6, AlarmStatusChanged=7, OnlineStatusChanged=8.
        /// </summary>
        private static string NormalizeDeviceEventType(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            int? numeric = null;
            if (token.Type == JTokenType.Integer)
            {
                numeric = token.Value<int>();
            }
            else if (int.TryParse(token.ToString(), out int parsed))
            {
                numeric = parsed;
            }

            if (numeric.HasValue)
            {
                switch (numeric.Value)
                {
                    case 1: return "Alarm";
                    case 2: return "AlarmInput";
                    case 3: return "VirtualDeviceAlarm";
                    case 4: return "CruiseStatus";
                    case 5: return "CruisePosition";
                    case 6: return "Defect";
                    case 7: return "AlarmStatusChanged";
                    case 8: return "OnlineStatusChanged";
                    default: return string.Empty;
                }
            }

            return token.ToString();
        }

        /// <summary>
        /// FireMonitorAlarmLevel: None=0, Alert1=1, Alert2=2, Alarm=3.
        /// </summary>
        private static string NormalizeAlarmLevel(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            int? numeric = null;
            if (token.Type == JTokenType.Integer)
            {
                numeric = token.Value<int>();
            }
            else if (int.TryParse(token.ToString(), out int parsed))
            {
                numeric = parsed;
            }

            if (numeric.HasValue)
            {
                switch (numeric.Value)
                {
                    case 0: return "None";
                    case 1: return "Alert1";
                    case 2: return "Alert2";
                    case 3: return "Alarm";
                    default: return numeric.Value.ToString();
                }
            }

            return token.ToString();
        }

        private static string NormalizeRuleType(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            return token.ToString();
        }

        /// <summary>
        /// Alarm UI binds Image.Source to Url (http historically). In-proc SaveFile returns a
        /// relative path under AlarmRootPath — combine and expose as file:/// URI.
        /// </summary>
        private static string ResolveLocalImageUrl(string rootPath, string relativeOrAbsolutePath)
        {
            string absolute = LocalImagePath.ResolveAbsolute(rootPath, relativeOrAbsolutePath);
            if (string.IsNullOrWhiteSpace(absolute))
            {
                return relativeOrAbsolutePath;
            }

            if (!File.Exists(absolute))
            {
                Logger.Warn($"Alarm image file missing: {absolute}");
            }

            return LocalImagePath.ToFileUri(absolute) ?? absolute;
        }

        private static Delegate CreateDeviceLoadingDelegate(Type handlerType)
        {
            MethodInfo core = typeof(InProcFireEngineBridge).GetMethod(
                nameof(BuildDeviceLoadBo), BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo invoke = handlerType.GetMethod("Invoke");
            Type returnType = invoke.ReturnType; // Task<DeviceLoadBO>
            Type loadBoType = returnType.GetGenericArguments()[0];
            ParameterInfo[] ps = invoke.GetParameters();
            ParameterExpression p0 = Expression.Parameter(ps[0].ParameterType, "sender");
            ParameterExpression p1 = Expression.Parameter(ps[1].ParameterType, "args");

            MethodCallExpression buildCall = Expression.Call(
                core,
                Expression.Convert(p1, typeof(object)));
            UnaryExpression castLoad = Expression.Convert(buildCall, loadBoType);
            MethodInfo fromResult = typeof(Task).GetMethod("FromResult").MakeGenericMethod(loadBoType);
            MethodCallExpression taskCall = Expression.Call(fromResult, castLoad);
            return Expression.Lambda(handlerType, taskCall, p0, p1).Compile();
        }

        private static object BuildDeviceLoadBo(object args)
        {
            try
            {
                string deviceId = null;
                if (args != null)
                {
                    PropertyInfo idProp = args.GetType().GetProperty("DeviceID")
                        ?? args.GetType().GetProperty("deviceID");
                    deviceId = idProp?.GetValue(args) as string;
                }

                if (string.IsNullOrEmpty(deviceId) || _deviceLoadBoType == null || _deviceBoType == null)
                {
                    return null;
                }

                DeviceDO device = LiteDeviceRepository.Get(deviceId);
                if (device == null)
                {
                    return null;
                }

                object deviceBo = Activator.CreateInstance(_deviceBoType);
                _deviceBoType.GetProperty("ID")?.SetValue(deviceBo, device.ID);
                _deviceBoType.GetProperty("Name")?.SetValue(deviceBo, device.Name);
                _deviceBoType.GetProperty("ProtocolType")?.SetValue(deviceBo, device.ProtocolType);

                object addressList = Activator.CreateInstance(typeof(List<>).MakeGenericType(_deviceAddressBoType));
                MethodInfo add = addressList.GetType().GetMethod("Add");
                int addrCount = 0;
                if (device.DeviceAddress != null)
                {
                    foreach (DeviceAddressDataObject addr in device.DeviceAddress)
                    {
                        object abo = Activator.CreateInstance(_deviceAddressBoType);
                        _deviceAddressBoType.GetProperty("ChannelID")?.SetValue(abo, addr.ChannelID);
                        _deviceAddressBoType.GetProperty("Type")?.SetValue(abo, addr.Type);
                        _deviceAddressBoType.GetProperty("ProtocolType")?.SetValue(abo, addr.ProtocolType);
                        _deviceAddressBoType.GetProperty("IP")?.SetValue(abo, addr.IP);
                        _deviceAddressBoType.GetProperty("Port")?.SetValue(abo, addr.Port);
                        _deviceAddressBoType.GetProperty("Username")?.SetValue(abo, addr.Username);
                        _deviceAddressBoType.GetProperty("Password")?.SetValue(abo, addr.Password);
                        _deviceAddressBoType.GetProperty("ParamID")?.SetValue(abo, addr.ParamID);
                        add.Invoke(addressList, new[] { abo });
                        addrCount++;
                    }
                }

                _deviceBoType.GetProperty("DeviceAddressList")?.SetValue(deviceBo, addressList);

                object loadBo = Activator.CreateInstance(_deviceLoadBoType);
                _deviceLoadBoType.GetProperty("DeviceBO")?.SetValue(loadBo, deviceBo);
                string conf = string.IsNullOrWhiteSpace(device.Conf) ? "{}" : device.Conf;
                _deviceLoadBoType.GetProperty("DeviceConfigStr")?.SetValue(loadBo, conf);
                return loadBo;
            }
            catch (Exception ex)
            {
                Logger.Error("BuildDeviceLoadBo failed.", ex);
                return null;
            }
        }

        private static bool _resolveHooked;

        /// <summary>
        /// Lib Stream.Core is built against System.Text.Json 5.0.0.2; Client ships 6.0.0.2.
        /// App.config redirects help Load context; hook covers residual mismatches.
        /// </summary>
        private static void EnsureAssemblyResolveHook()
        {
            if (_resolveHooked)
            {
                return;
            }

            _resolveHooked = true;
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                try
                {
                    AssemblyName requested = new AssemblyName(args.Name);
                    if (string.Equals(requested.Name, "System.Text.Json", StringComparison.OrdinalIgnoreCase))
                    {
                        string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.Text.Json.dll");
                        if (System.IO.File.Exists(path))
                        {
                            return Assembly.LoadFrom(path);
                        }
                    }
                }
                catch
                {
                }

                return null;
            };
        }

        private static Assembly TryLoadAssembly(string name)
        {
            EnsureAssemblyResolveHook();

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return a;
                }
            }

            // Prefer default load context (AppBase probing) so App.config bindingRedirects apply.
            // Avoid LoadFrom: it uses the LoadFrom context where redirects often fail for deps
            // such as Stream.Core -> System.Text.Json 5.0.0.2.
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name + ".dll");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    AssemblyName an = AssemblyName.GetAssemblyName(path);
                    return Assembly.Load(an);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Assembly.Load(" + name + ") failed: " + ex.Message);
                }
            }

            try
            {
                return Assembly.Load(name);
            }
            catch
            {
                return null;
            }
        }


        private static object UnwrapTaskResult(object result)
        {
            if (result == null)
            {
                return null;
            }

            Task task = result as Task;
            if (task == null)
            {
                return result;
            }

            task.ConfigureAwait(false).GetAwaiter().GetResult();
            PropertyInfo resultProp = result.GetType().GetProperty("Result");
            return resultProp?.GetValue(result);
        }
    }
}
