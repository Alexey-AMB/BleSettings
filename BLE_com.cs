using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

using System.Data;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

using System.Threading;

using System.Runtime.InteropServices;

using Windows.Devices.Bluetooth.Advertisement;


namespace BLE_setup
{
    public enum DataFormat
    {
        ASCII = 0,
        UTF8,
        Dec,
        Hex,
        Bin,
    }

    //public struct stCommand
    //{
    //    byte signature;  //стартовая сигнатура 223
    //    byte cmd;        //команда из InCommand
    //    Int16 lenbuf;    //длинна буфера с данными
    //    byte crc;
    //}

    public enum OutAsk
    {
        ASK_OK,
        ASK_ERROR,
        ASK_NEXT,
        ASK_MALLOC_ERROR,
        ASK_ERROR_CRC
    }

     public enum InCommandBase : UInt32
        {
            CMD_NONE,               //test command                          | без буфера
            CMD_GET_SETTINGS,       //пришли настройки на ПК                | без буфера
            CMD_SET_SETTINGS,       //установи настройки                    / структура с настройками базы
            CMD_SET_TIME,           //установи время часов                  / uint32 время в UNIX формате
            CMD_GET_AKKVOLTAGE,     //пришли напряжение на аккумуляторе     | без буфера
            CMD_SET_BLINK,          //помигай лампочками                    | без буфера
            CMD_WRITE_CARD,         //запиши данные на карту
            CMD_READ_CARD,          //считай данные с карты                 | без буфера
            CMD_NEXT,               //пришли следующий блок                 | без буфера
            CMD_MODE_SLEEP,         //переход в сон                         | без буфера
            CMD_MODE_WAIT,          //переход в ожидание                    | без буфера
            CMD_MODE_ACTIVE,        //переход в активный режим              | без буфера
            CMD_WRITE_CARD_NUM,     //записать номер в карточку             / 4 байта номер карточки или мастер-карточки
            CMD_CLEAR_CARD,         //очисти карточку                       | без буфера
            CMD_SET_TIMES_RUN,      //установи время начала и конца забега  / два uint32 начало и конец соревнований
            CMD_GET_VERSION         //Версия софта int32
        }

    public enum InCommandTag : UInt32
    {
        CMD_NONE,               //test command
        CMD_GET_SETTINGS,       //пришли настройки на ПК
        CMD_SET_SETTINGS,       //установи настройки
        CMD_SET_TIME,           //установи время часов
        CMD_GET_AKKVOLTAGE,     //пришли напряжение на аккумуляторе
        CMD_SET_BLINK,          //помигай лампочками
        //CMD_WRITE_DATA,         //запиши данные
        CMD_READ_DATA,          //считай данные в буфере номер блока от 2 до 15
        CMD_SET_MODE_RUN,
        CMD_SET_MODE_CONN,
        CMD_SET_MODE_SLEEP,
        CMD_NEXT,
        CMD_GET_VERSION
    }

    public enum WORKMODE_BASE : byte
        {
            MODE_ACTIVE,
            MODE_WAIT,
            MODE_SLEEP
        }

        public enum WORKTYPE : byte
        {
            TYPE_START,
            TYPE_BASE_MAIN,
            TYPE_FINISH,
            TYPE_CLEAR,
            TYPE_CHECK,
            TYPE_READCARD
        }

    public enum WORKMODE_TAG : byte
    {
        MODE_CONNECT,
        MODE_RUN,
        MODE_SLEEP
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SPORT_BASE_SETTINGS
    {
        public WORKMODE_BASE mode_station;       //режим работы
        public WORKTYPE type_station;       //тип станции
        public byte num_station;        //номер станции
        public UInt32 timeut_station;     //время через которое станция уснет в сек.
        public byte powerble_station;   //мощность BLE передатчика станции от 0 (-21 дБ) до 12 (+5 дБ) см ll.h строка 404
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] password_station;  //пароль BLE
        public byte gain_KM;            //усиление антенны контактных меток
        public UInt32 timer_KM;           //период поиска приложенных контактных меток в МС
        public byte settins_KM;         //мелкие настройки задачи контактных меток
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] ar_secure_key;   //ключи шифрования для карточек
        public byte signature;          //сигнатура SIGNATURE_EPROM_SETTINGS
        public byte service1;           //сервисные данные. бит 0 - инверсия лампочек (платы версии 0 вкл 0, платы версии 1 вкл 1);
        public byte service2;           //сервисные данные. не используется
        public byte service3;           //сервисные данные. не используется
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SPORT_TAG_SETTINGS   //len=42
    {
        public WORKMODE_TAG mode_tag;           //режим работы
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] name_tag;       //имя метки
        public Int32 timeut_conn;        //время через которое метка уснет в сек. в режиме соединения
                                       //int         timeut_run;         //время через которое метка уснет в сек. в режиме забега
        public byte powerble_tag;       //мощность BLE передатчика метки от 0 (-21 дБ) до 12 (+5 дБ) см ll.h строка 404
        public sbyte treshold_tag;       //порог чувствительности метки  -40=30cm, -60=200cm, 100=6m
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] password_tag;   //пароль BLE

        //USER_SETTINGS        //len=103
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] fam;        //фамилия
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] imj;        //имя
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] otch;       //отчество
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] group;       //группа
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] razr;        //разряд
        public UInt16 godrojd;        //год рождения
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] colectiv;   //название коллектива
        public byte zabeg;          //номер забега
        public UInt16 startnum;       //стартовый номер
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] starttime;   //время старта
        public byte lgota;          //есть ли льгота
        public byte arenda;         //свой чип или арендованный
        public byte signature;      //сигнатура SIGNATURE_EPROM_SETTINGS
    }

    public enum MyTypeBleDevice
    {
        UNKNOW,
        BASE,
        TAG
    }

    public class stMyBleDevice
    {
        public string sBleId;
        public string sName;
        public string sBleMacAddr;
        public MyTypeBleDevice type;
        public ulong uBleAddr;
        public bool bIsActive;
        public bool bIsTime;
        public uint uAkk;
        public bool bIsAlarm;
    }

    public struct stCommand
    {
        public byte signature;  //стартовая сигнатура 223
        public byte cmd;        //команда из InCommand
        public UInt16 lenbuf;    //длинна буфера с данными
        public byte crc;
    }

    public static class BLE_com
    {
        public static object oLock = new object();
        public const int SizeStCommand = 5;

        //private int iCurrCommand = 0;
        //private bool bIsOK = false;
        private static int iExpectedLen = 0;
        private static int iCurrCmd = 0;
        private static int iExpectedCrc = 0;
        private static int iRecivedLen = 0;
        private static int iSendedLen = 0;
        private static byte[] pBuffIn = null;
        private static int iBuffInLen = 0;
        public static byte[] pBuffOut = null;
        public static UInt16 iBuffOutLen = 0;
        public static byte cCmdNext = 8;

        public const byte SIGNATURE_COMMAND = 223;
        private const byte MYDATATRANSFER_MYBUFIN1_LEN = 23;

        public static ConcurrentDictionary<string, stMyBleDevice> BleList = new ConcurrentDictionary<string, stMyBleDevice>();
        public static bool bBaseFound = true;
        public static bool bTagFound = true;

        static BluetoothLEAdvertisementWatcher _watcher = null;

        //==================================================================
        public delegate void HaveBuff(byte[] buff);
        public static event HaveBuff BuffChaged;

        public delegate void HaveError();
        public static event HaveError BuffError;

        public delegate void HaveUpdateList();
        public static event HaveUpdateList RefreshList;

        //==================================================================

        private static BluetoothLEDevice _selectedDevice = null;

        private static List<BluetoothLEAttributeDisplay> _services = new List<BluetoothLEAttributeDisplay>();
        private static BluetoothLEAttributeDisplay _selectedService = null;

        private static List<BluetoothLEAttributeDisplay> _characteristics = new List<BluetoothLEAttributeDisplay>();
        //static BluetoothLEAttributeDisplay _selectedCharacteristic = null;

        private static List<GattCharacteristic> _subscribers = new List<GattCharacteristic>();
        private static TimeSpan _timeout = TimeSpan.FromSeconds(3);
        private static async Task<int> OpenDevice(string deviceName)
        {
            int retVal = 0;
            if (!string.IsNullOrEmpty(deviceName))
            {
                string foundId = deviceName;    // Utilities.GetIdByNameOrNumber(devs, deviceName);

                // If device is found, connect to device and enumerate all services
                if (!string.IsNullOrEmpty(foundId))
                {
                    //_selectedCharacteristic = null;
                    _selectedService = null;
                    _services.Clear();

                    try
                    {
                        // only allow for one connection to be open at a time
                        if (_selectedDevice != null) CloseDevice();

                        _selectedDevice = await BluetoothLEDevice.FromIdAsync(foundId).AsTask().TimeoutAfter(_timeout);

                        var result = await _selectedDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                        if (result.Status == GattCommunicationStatus.Success)
                        {
                            for (int i = 0; i < result.Services.Count; i++)
                            {
                                var serviceToDisplay = new BluetoothLEAttributeDisplay(result.Services[i]);
                                _services.Add(serviceToDisplay);
                            }
                        }
                        else
                        {
                            retVal += 1;
                        }
                    }
                    catch
                    {
                        retVal += 1;
                    }
                }
                else
                {
                    retVal += 1;
                }
            }
            else
            {
                retVal += 1;
            }
            return retVal;
        }

        private static void CloseDevice()
        {
            // Remove all subscriptions
            if (_subscribers.Count > 0) Unsubscribe();

            if (_selectedDevice != null)
            {
                _services?.ForEach((s) => { s.service?.Dispose(); });
                _services?.Clear();
                _characteristics?.Clear();
                _selectedDevice?.Dispose();
            }
            _selectedDevice = null;
        }

        private static async void Unsubscribe()
        {
            //if (_subscribers == null) return;
            foreach (var sub in _subscribers)
            {
                try
                {
                    await sub.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                    sub.ValueChanged -= Characteristic_ValueChanged;
                }
                catch { }
            }
            _subscribers.Clear();
        }

        private static void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var newValue = FormatValue(args.CharacteristicValue, DataFormat.Hex);

            //synchronizationContext.Post(new SendOrPostCallback(o =>
            //{
            //    this.label1.Text = /*"Value changed for " + sender.Uuid + ": " + */ newValue;

            //}), null);

            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out byte[] data);
            GetBuffer(data);
        }

        async static Task<int> SetService(string serviceName)
        {
            int retVal = 0;
            if (_selectedDevice != null)
            {
                if (!string.IsNullOrEmpty(serviceName))
                {
                    string foundName = serviceName;
                    // If device is found, connect to device and enumerate all services
                    if (!string.IsNullOrEmpty(foundName))
                    {
                        var attr = _services.FirstOrDefault(s => s.Name.Equals(foundName));
                        IReadOnlyList<GattCharacteristic> characteristics = new List<GattCharacteristic>();

                        try
                        {
                            // Ensure we have access to the device.
                            var accessStatus = await attr.service.RequestAccessAsync();
                            if (accessStatus == DeviceAccessStatus.Allowed)
                            {
                                // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                                // and the new Async functions to get the characteristics of unpaired devices as well. 
                                var result = await attr.service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                                if (result.Status == GattCommunicationStatus.Success)
                                {
                                    characteristics = result.Characteristics;
                                    _selectedService = attr;
                                    _characteristics.Clear();

                                    if (characteristics.Count > 0)
                                    {
                                        for (int i = 0; i < characteristics.Count; i++)
                                        {
                                            var charToDisplay = new BluetoothLEAttributeDisplay(characteristics[i]);
                                            _characteristics.Add(charToDisplay);
                                        }
                                    }
                                    else
                                    {
                                        retVal += 1;
                                    }
                                }
                                else
                                {
                                    retVal += 1;
                                }
                            }
                            // Not granted access
                            else
                            {
                                retVal += 1;
                            }
                        }
                        catch //(Exception ex)
                        {
                            retVal += 1;
                        }
                    }
                    else
                    {
                        retVal += 1;
                    }
                }
                else
                {
                    retVal += 1;
                }
            }
            else
            {
                retVal += 1;
            }

            return retVal;
        }

        //async static Task<int> WriteCharacteristic(string param, int iCa)
        //{
        //    int retVal = 0;
        //    if (_selectedDevice != null)
        //    {
        //        if (!string.IsNullOrEmpty(param))
        //        {
        //            if (_characteristics.Count < iCa) return 1;
        //            var attr = _characteristics[iCa];

        //            var buffer = FormatData(param, DataFormat.Hex);

        //            if (attr != null && attr.characteristic != null)
        //            {
        //                // Write data to characteristic
        //                GattWriteResult result = await attr.characteristic.WriteValueWithResultAsync(buffer);
        //                if (result.Status != GattCommunicationStatus.Success)
        //                {
        //                    retVal += 1;
        //                }
        //            }
        //            else
        //            {
        //                retVal += 1;
        //            }

        //        }
        //    }
        //    else
        //    {
        //        retVal += 1;
        //    }
        //    return retVal;
        //}

        async static Task<int> WriteCharacteristic(IBuffer buffer, int iCa)
        {
            int retVal = 0;
            if (_selectedDevice != null)
            {
                if (_characteristics.Count < iCa) return 1;
                var attr = _characteristics[iCa];

                if (attr != null && attr.characteristic != null)
                {
                    // Write data to characteristic
                    GattWriteResult result = await attr.characteristic.WriteValueWithResultAsync(buffer);
                    if (result.Status != GattCommunicationStatus.Success)
                    {
                        retVal += 1;
                    }
                }
                else
                {
                    retVal += 1;
                }
            }
            else
            {
                retVal += 1;
            }
            return retVal;
        }

        async static Task<int> SubscribeToCharacteristic(int iCa)
        {
            int retVal = 0;

            var attr = _characteristics[iCa];
            if (attr != null && attr.characteristic != null)
            {
                // First, check for existing subscription
                if (!_subscribers.Contains(attr.characteristic))
                {
                    var status = await attr.characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Success)
                    {
                        _subscribers.Add(attr.characteristic);
                        attr.characteristic.ValueChanged += Characteristic_ValueChanged;
                    }
                    else
                    {
                        retVal += 1;
                    }
                }
                else
                {
                    retVal += 1;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Format data for writing by specific format
        /// </summary>
        /// <param name="data"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private static IBuffer FormatData(string data, DataFormat format)
        {
            try
            {
                // For text formats, use CryptographicBuffer
                if (format == DataFormat.ASCII || format == DataFormat.UTF8)
                {
                    return CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);
                }
                else
                {
                    string[] values = data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    byte[] bytes = new byte[values.Length];

                    for (int i = 0; i < values.Length; i++)
                        bytes[i] = Convert.ToByte(values[i], (format == DataFormat.Dec ? 10 : (format == DataFormat.Hex ? 16 : 2)));

                    var writer = new DataWriter();
                    writer.ByteOrder = ByteOrder.LittleEndian;
                    writer.WriteBytes(bytes);

                    return writer.DetachBuffer();
                }
            }
            catch //(Exception error)
            {
                return null;
            }
        }

        /// <summary>
        /// This function converts IBuffer data to string by specified format
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private static string FormatValue(IBuffer buffer, DataFormat format)
        {
            byte[] data;
            CryptographicBuffer.CopyToByteArray(buffer, out data);

            switch (format)
            {
                case DataFormat.ASCII:
                    return Encoding.ASCII.GetString(data);

                case DataFormat.UTF8:
                    return Encoding.UTF8.GetString(data);

                case DataFormat.Dec:
                    return string.Join(" ", data.Select(b => b.ToString("00")));

                case DataFormat.Hex:
                    return BitConverter.ToString(data).Replace("-", " ");

                case DataFormat.Bin:
                    var s = string.Empty;
                    foreach (var b in data) s += Convert.ToString(b, 2).PadLeft(8, '0') + " ";
                    return s;

                default:
                    return Encoding.ASCII.GetString(data);
            }
        }

        //==================================================================

        private static void GetBuffer(byte[] buf)
        {
            if ((iRecivedLen == 0) && (iExpectedLen == 0))
            {        //не принят еще не один блок
                if (buf[0] == 223)
                {
                    //if (buf[1] == 0) bIsOK = true;
                    //else bIsOK = false;

                    iExpectedLen = (buf[3] << 8) + buf[2];
                    iCurrCmd = buf[1];
                    iExpectedCrc = buf[4];

                    if (iExpectedLen == 0)
                    {
                        ExecuteCommand(false);
                        return;
                    }
                    else
                    {
                        pBuffIn = new byte[iExpectedLen];
                        iBuffInLen = iExpectedLen;
                        if (iExpectedLen <= (buf.Length - SizeStCommand))
                        {
                            Array.Copy(buf, SizeStCommand, pBuffIn, 0, iExpectedLen);
                            if (GetCRC8(pBuffIn, (UInt16)iExpectedLen) == iExpectedCrc)
                            {
                                ExecuteCommand(true);
                                return;
                            }
                            else
                            { //сообщить об ошибке
                                //SendCommand(OutAsk.ASK_ERROR_CRC, false);
                                iExpectedLen = 0;
                                iCurrCmd = 0;
                                iExpectedCrc = 0;
                                return;
                            }
                        }
                        else
                        { //если данные не влезли в один буфер
                            Array.Copy(buf, SizeStCommand, pBuffIn, 0, buf.Length - SizeStCommand);
                            iRecivedLen = buf.Length - SizeStCommand;
                            SendCommand(cCmdNext, false);
                            return;
                        }
                    }
                }
                else
                {   //не сошлась сигнатура
                    //SendCommand(OutAsk.ASK_ERROR, false);
                    iExpectedLen = 0;
                    iCurrCmd = 0;
                    iExpectedCrc = 0;
                }
            }

            if (iExpectedLen > iRecivedLen)
            {
                if (iExpectedLen - iRecivedLen > buf.Length)
                {
                    Array.Copy(buf, 0, pBuffIn, iRecivedLen, buf.Length);
                    iRecivedLen += buf.Length;
                    SendCommand(cCmdNext, false);
                    return;
                }
                else
                {
                    Array.Copy(buf, 0, pBuffIn, iRecivedLen, iExpectedLen - iRecivedLen);
                    if (GetCRC8(pBuffIn, (UInt16)iExpectedLen) == iExpectedCrc)
                    {
                        ExecuteCommand(true);
                        return;
                    }
                    else
                    { //сообщить об ошибке
                        //SendCommand(OutAsk.ASK_ERROR_CRC, false);
                        iExpectedLen = 0;
                        iCurrCmd = 0;
                        iExpectedCrc = 0;
                        return;
                    }
                }
            }
            else
            {   //странная ситуация, но такое было
                iRecivedLen = 0;
                iExpectedLen = 0;
                iSendedLen = 0;

                pBuffIn = null;
            }
        }

        private static byte GetCRC8(byte[] buf, UInt16 len)
        {
            byte crc = 0;
            for (int i = 0; i < len; i++)
            {
                crc += (byte)buf[i];
            }

            return (byte)crc;
        }

        public static /*async Task*/ void SendCommand(byte cmd, bool bHaveBuf)
        {
            stCommand cmdOut;
            byte[] buf = null;
            byte len = 0;

            if (iSendedLen == 0)
            {
                cmdOut.cmd = (byte)cmd;
                cmdOut.signature = SIGNATURE_COMMAND;
                cmdOut.lenbuf = 0;
                cmdOut.crc = 0;
                if ((bHaveBuf) && (pBuffOut != null))
                {
                    cmdOut.lenbuf = iBuffOutLen;
                    cmdOut.crc = (byte)GetCRC8(pBuffOut, iBuffOutLen);
                }

                if ((cmdOut.lenbuf + SizeStCommand) > MYDATATRANSFER_MYBUFIN1_LEN)
                {   // все не влезет в один буфер
                    len = MYDATATRANSFER_MYBUFIN1_LEN;
                    iSendedLen = len - SizeStCommand;
                }
                else
                {   // влезет в один буфер
                    len = (byte)(cmdOut.lenbuf + SizeStCommand);
                    iSendedLen = 0;
                }

                buf = new byte[len];

                Array.Copy(GetBytes(cmdOut), buf, SizeStCommand);
                if ((bHaveBuf) && (pBuffOut != null)) Array.Copy(pBuffOut, 0, buf, SizeStCommand, len - SizeStCommand);
                WriteToBle(buf);
                buf = null;
                if ((iSendedLen == 0) && (bHaveBuf)) //передача закончена
                {
                    pBuffOut = null;
                }
            }
            else
            {
                if ((iBuffOutLen - iSendedLen) > MYDATATRANSFER_MYBUFIN1_LEN)
                {   // все не влезет в один буфер
                    len = MYDATATRANSFER_MYBUFIN1_LEN;
                    //iSendedLen += len;
                }
                else
                {   // влезет в один буфер
                    len = (byte)(iBuffOutLen - iSendedLen);
                    //iSendedLen = 0;
                }

                buf = new byte[len];

                if ((bHaveBuf) && (pBuffOut != null)) Array.Copy(pBuffOut, iSendedLen, buf, 0, len);
                WriteToBle(buf);

                iSendedLen += len;
                if (iSendedLen == iBuffOutLen) //передача закончена
                {
                    pBuffOut = null;
                    iSendedLen = 0;
                }
            }

        }

        private static void ExecuteCommand(bool bHaveBuf)
        {
            switch (iCurrCmd)
            {
                case ((int)OutAsk.ASK_NEXT):
                    SendCommand((byte)InCommandBase.CMD_NONE, true);
                    return;
                //break;
                case ((int)OutAsk.ASK_OK):
                    if (BuffChaged != null) BuffChaged(pBuffIn);
                    break;
                case ((int)OutAsk.ASK_ERROR):
                    BuffError();
                    break;
            }

            iExpectedLen = 0;
            iCurrCmd = 0;
            iExpectedCrc = 0;
            iRecivedLen = 0;

            pBuffIn = null;
            iBuffInLen = 0;
        }

        public static byte[] GetBytes(SPORT_BASE_SETTINGS str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static byte[] GetBytes(stCommand str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static byte[] GetBytes(SPORT_TAG_SETTINGS str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        //public static async Task WriteToBleOneShot(string sSelID, byte[] bytes)
        //{
        //    string sSelServ = "Custom Service: f000ba33-0451-4000-b000-000000000000";
        //    var writer = new DataWriter();
        //    writer.ByteOrder = ByteOrder.LittleEndian;
        //    writer.WriteBytes(bytes);

        //    try
        //    {
        //        await OpenDevice(sSelID);

        //        await SetService(sSelServ);

        //        await SubscribeToCharacteristic(0);

        //        await WriteCharacteristic(writer.DetachBuffer(), 1);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Ошибка соединения. " + ex.Message);
        //    }
        //    finally
        //    {
        //        CloseDevice();
        //    }
        //}

        public static async Task<bool> OpenBle(string sSelID, MyTypeBleDevice type)
        {
            string sSelServ = null;
            if (type == MyTypeBleDevice.BASE) sSelServ = "Custom Service: f000ba33-0451-4000-b000-000000000000";
            if (type == MyTypeBleDevice.TAG) sSelServ = "Custom Service: f000ba43-0451-4000-b000-000000000000";
            bool bRet = true;
            try
            {
                await OpenDevice(sSelID);

                await SetService(sSelServ);

                await SubscribeToCharacteristic(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка соединения. \r\n" + "Устройство не найдено, обновите список устройств. \r\n\r\n\r\n" +
                                "Подробно: " + ex.Message, "Ошибка соединения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CloseDevice();
                bRet = false;
            }
            finally
            {

            }
            return bRet;
        }

        private static async Task WriteToBle(byte[] bytes)
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(bytes);

            try
            {
                await WriteCharacteristic(writer.DetachBuffer(), 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка записи. " + ex.Message);
            }
            finally
            {

            }
        }

        public static void CloseBle()
        {
            CloseDevice();
        }

        public static void StartDiscoveryAdv()
        {
            _watcher = new BluetoothLEAdvertisementWatcher();
            _watcher.ScanningMode = BluetoothLEScanningMode.Active;
            _watcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter
            {
                InRangeThresholdInDBm = -75,
                OutOfRangeThresholdInDBm = -76,
                OutOfRangeTimeout = TimeSpan.FromSeconds(2),
                SamplingInterval = TimeSpan.FromSeconds(2)
            };

            _watcher.AdvertisementFilter =
                 new BluetoothLEAdvertisementFilter
                 {
                     Advertisement =
                              new BluetoothLEAdvertisement
                              {
                                  ServiceUuids =
                                            { 
                                                //new Guid("0000ba33-0000-1000-8000-00805f9b34fb")
                                            }
                              }
                 };

            //my

            _watcher.AdvertisementFilter.Advertisement.ServiceUuids.Clear();
            //_watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(new Guid("0000ba33-0000-1000-8000-00805f9b34fb"));
            //_watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(new Guid("0000ba43-0000-1000-8000-00805f9b34fb"));


            //_watcher.AdvertisementFilter.Advertisement.ServiceUuids.forEach(uuid => {
            //    var uuidString = // format uuid as GUID string
            //    this._advertisementWatcher.advertisementFilter.advertisement.serviceUuids.add(uuidString);
            //});

            _watcher.Received += _watcher_Received;
            _watcher.Stopped += _watcher_Stopped;
            _watcher.Start();
        }

        public static void StopDiscoveryAdv()
        {
            if (_watcher == null) return;
            _watcher.Stop();

            Thread.Sleep(1000);
            //while (_watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopped) Thread.Sleep(1);
            _watcher.Received -= _watcher_Received;
            _watcher.Stopped -= _watcher_Stopped;
            _watcher = null;
        }

        private static async void _watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            bool bIsBase = false;
            bool bIsTag = false;
            BluetoothLEDevice device = null;

            try
            {
                device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if (device == null) return;

                foreach (var s in args.Advertisement.ServiceUuids)
                {
                    if (s.ToString() == "0000ba33-0000-1000-8000-00805f9b34fb") bIsBase = true;
                    if (s.ToString() == "0000ba43-0000-1000-8000-00805f9b34fb") bIsTag = true;
                }

                if ((bIsBase && bBaseFound) || (bIsTag && bTagFound))
                {
                    lock (oLock)
                    {
                        if (!BleList.ContainsKey(device.DeviceId))
                        {
                            stMyBleDevice mbd = new stMyBleDevice();
                            mbd.sName = device.Name;
                            mbd.sBleMacAddr = device.BluetoothAddress.ToString("X12");
                            mbd.sBleId = device.DeviceId;
                            mbd.uBleAddr = device.BluetoothAddress;
                            if (bIsBase) mbd.type = MyTypeBleDevice.BASE;
                            if (bIsTag) mbd.type = MyTypeBleDevice.TAG;


                            if (bIsBase)
                            {
                                var dataSections = args.Advertisement.DataSections;
                                var sectionData = dataSections[2];
                                byte[] data = new byte[sectionData.Data.Length];
                                using (var reader = DataReader.FromBuffer(sectionData.Data))
                                {
                                    reader.ReadBytes(data);
                                }

                                byte[] patternA = new byte[3] { Convert.ToByte('A'), Convert.ToByte('S'), Convert.ToByte('E') };
                                byte[] patternS = new byte[3] { Convert.ToByte('a'), Convert.ToByte('s'), Convert.ToByte('e') };
                                if (IndexOf(data, patternA) >= 0) mbd.bIsActive = true;

                                int istartLen = -1;
                                if (IndexOf(data, patternA) >= 0) istartLen = IndexOf(data, patternA) + 6;
                                if (IndexOf(data, patternS) >= 0) istartLen = IndexOf(data, patternS) + 6;

                                Int32 baseTime = 0;
                                byte byteStatus = 0;
                                if (istartLen > 0)
                                {
                                    baseTime = BitConverter.ToInt32(data, istartLen);
                                    if (data.Length > istartLen + 4)
                                        byteStatus = data[istartLen + 4];
                                }

                                Int32 unixTimestampNow = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                                if (Math.Abs(baseTime - unixTimestampNow) < 10) mbd.bIsTime = true;


                                if ((byteStatus & (1 << 2)) > 0) mbd.bIsAlarm = true;
                                mbd.uAkk = (uint)byteStatus & 0x03;

                                //string DataString;
                                //if (data.Length > 9)
                                //{
                                //    DataString = Encoding.ASCII.GetString(data, 0, data.Length);
                                //}
                            }



                            BleList.GetOrAdd(device.DeviceId, mbd);
                            RefreshList();
                        }
                    }
                }
            }
            catch// (Exception ex)
            { }

            finally
            {
                if (device != null) device.Dispose();
            }
            device = null;
        }

        private static void _watcher_Stopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            string errorMsg = null;
            if (args != null)
            {
                switch (args.Error)
                {
                    case BluetoothError.Success:
                        //errorMsg = "WatchingSuccessfullyStopped";
                        break;
                    case BluetoothError.RadioNotAvailable:
                        errorMsg = "ErrorNoRadioAvailable";
                        break;
                    case BluetoothError.ResourceInUse:
                        errorMsg = "ErrorResourceInUse";
                        break;
                    case BluetoothError.DeviceNotConnected:
                        errorMsg = "ErrorDeviceNotConnected";
                        break;
                    case BluetoothError.DisabledByPolicy:
                        errorMsg = "ErrorDisabledByPolicy";
                        break;
                    case BluetoothError.NotSupported:
                        errorMsg = "ErrorNotSupported";
                        break;
                }
            }
            if (errorMsg != null)
            {

            }

        }

        private static int IndexOf(byte[] input, byte[] pattern)
        {
            byte firstByte = pattern[0];
            int index = -1;

            if ((index = Array.IndexOf(input, firstByte)) >= 0)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (index + i >= input.Length ||
                     pattern[i] != input[index + i]) return -1;
                }
            }

            return index;
        }

        public static stMyBleDevice GetMbdFromList(string sBleMacAddr)
        {
            foreach (stMyBleDevice mbd in BleList.Values)
            {
                if (mbd.sBleMacAddr == sBleMacAddr)
                {
                    return mbd;
                }
            }

            return null;
        }

        public static void DelFromList(string sBleMacAddr)
        {
            string sKey = null;
            foreach (stMyBleDevice mbd in BleList.Values)
            {
                if (mbd.sBleMacAddr == sBleMacAddr)
                {
                    sKey = mbd.sBleId;
                    break;
                }
            }
            if (sKey != null)
            {
                stMyBleDevice ret = new stMyBleDevice();
                BleList.TryRemove(sKey, out ret);
            }
        }
    }
}
