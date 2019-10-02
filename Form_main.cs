using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using BLE_setup;
using System.Runtime.InteropServices;

namespace BleSettings
{   
    public partial class Form_main : Form
    {
        public class BaseWorkType
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
        private readonly SynchronizationContext synchronizationContext;
        private bool bBaseOrTag = true;
        private DataGridViewSelectedRowCollection currSelectedRow = null;

        private int iCurrCommand = -1;

        //========================================================================
        public Form_main()
        {
            InitializeComponent();

            synchronizationContext = SynchronizationContext.Current;

            AddColumns();

            this.panel_buttonTag.Parent = this.groupBox2;
            this.panel_buttonTag.Dock = DockStyle.Fill;
            this.panel_buttonBase.Parent = this.groupBox2;
            this.panel_buttonBase.Dock = DockStyle.Fill;
            this.panel_buttonBase.BringToFront();

            this.panel_start.BringToFront();
            this.panel_start.Dock = DockStyle.Fill;
        }

        private void Form_main_Load(object sender, EventArgs e)
        {                        
            BLE_com.RefreshList += BLE_com_RefreshList;
            BLE_com.BuffChaged += BLE_com_BuffChanged;
            BLE_com.BuffError += BLE_com_BuffError;
            BLE_com.StartDiscoveryAdv();

            lock (BLE_com.oLock)
            {
                BLE_com.bBaseFound = true;
                BLE_com.bTagFound = false;
                BLE_com.BleList.Clear();
            }
        }
        
        private void Form_main_FormClosing(object sender, FormClosingEventArgs e)
        {
            BLE_com.StopDiscoveryAdv();
            BLE_com.RefreshList -= BLE_com_RefreshList;
            BLE_com.BuffChaged -= BLE_com_BuffChanged;
            BLE_com.BuffError -= BLE_com_BuffError;
        }

        private void Button_showBase_Click(object sender, EventArgs e)
        {
            bBaseOrTag = true;
            this.button_showBase.BackColor = Color.Lime;
            this.button_showTag.BackColor = DefaultBackColor;
            dataGridView1.Rows.Clear();
            lock (BLE_com.oLock)
            {
                //BLE_com.StopDiscoveryAdv();
                BLE_com.bBaseFound = true;
                BLE_com.bTagFound = false;
                BLE_com.BleList.Clear();
                //BLE_com.StartDiscoveryAdv();
            }
            this.panel_buttonBase.BringToFront();
        }

        private void Button_showTag_Click(object sender, EventArgs e)
        {
            bBaseOrTag = false;
            this.button_showTag.BackColor = Color.Lime;
            this.button_showBase.BackColor = DefaultBackColor;
            dataGridView1.Rows.Clear();
            lock (BLE_com.oLock)
            {
                BLE_com.bBaseFound = false;
                BLE_com.bTagFound = true;
                BLE_com.BleList.Clear();                
            }
            this.panel_buttonTag.BringToFront();
        }
            
        private void DataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            currSelectedRow = this.dataGridView1.SelectedRows;
        }

        //========================================================================

        private void BLE_com_RefreshList()
        {
            //bUpdateList = true;

            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                UpdateGridDevices();
            }), null);

            
        }

        private void SendCommand(InCommandBase cmd, byte[] databuf)
        {
            iCurrCommand = (byte)cmd;
            if (databuf != null)
            {
                BLE_com.pBuffOut = databuf;
                BLE_com.iBuffOutLen = (UInt16)databuf.Length;

                BLE_com.SendCommand((byte)cmd, true);
            }
            else
            {
                BLE_com.SendCommand((byte)cmd, false);
            }
        }

        private void SendCommand(InCommandTag cmd, byte[] databuf)
        {
            iCurrCommand = (byte)cmd;
            if (databuf != null)
            {
                BLE_com.pBuffOut = databuf;
                BLE_com.iBuffOutLen = (UInt16)databuf.Length;

                BLE_com.SendCommand((byte)cmd, true);
            }
            else
            {
                BLE_com.SendCommand((byte)cmd, false);
            }
        }

        private async void ButtonCommandSend(InCommandBase cmd, byte[] databuf, bool bRemovefromList)
        {
            if (currSelectedRow == null) return;
            if (currSelectedRow.Count == 0) return;

            DisableButtons(false);

            Form_progress fp = null;
            if (currSelectedRow.Count > 1)
            {
                fp = new Form_progress(currSelectedRow.Count);
                fp.Location = this.Location;
                fp.Show();
            }
            foreach (DataGridViewRow r in currSelectedRow)
            {
                stMyBleDevice mbd = BLE_com.GetMbdFromList(r.Cells[1].Value.ToString());
                if (mbd == null) continue;

                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandBase.CMD_NEXT;

                    SendCommand(cmd, databuf);
                    if (fp != null) fp.AddProgressValue();
                    WaitCurrComm();
                    if (bRemovefromList)
                    {
                        BLE_com.DelFromList(mbd.sBleMacAddr);
                        UpdateGridDevices();
                    }
                }
                BLE_com.CloseBle();
            }

            if (fp != null) fp.Close();

            DisableButtons(true);
        }

        private async void ButtonCommandSend(InCommandTag cmd, byte[] databuf, bool bRemovefromList)
        {
            if (currSelectedRow == null) return;
            if (currSelectedRow.Count == 0) return;

            DisableButtons(false);

            Form_progress fp = null;
            if (currSelectedRow.Count > 1)
            {
                fp = new Form_progress(currSelectedRow.Count);
                fp.Location = this.Location;
                fp.Show();
            }
            foreach (DataGridViewRow r in currSelectedRow)
            {
                stMyBleDevice mbd = BLE_com.GetMbdFromList(r.Cells[1].Value.ToString());
                if (mbd == null) continue;

                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandTag.CMD_NEXT;

                    SendCommand(cmd, databuf);
                    if (fp != null) fp.AddProgressValue();
                    WaitCurrComm();
                    if (bRemovefromList)
                    {
                        BLE_com.DelFromList(mbd.sBleMacAddr);
                        UpdateGridDevices();
                    }
                }
                BLE_com.CloseBle();
            }

            if (fp != null) fp.Close();

            DisableButtons(true);
        }

        private void BLE_com_BuffChanged(byte[] pBuffIn)
        {
            //if (pBuffIn == null) return;
            if (bBaseOrTag)
            {
                switch (iCurrCommand)   //работаем с базами
                {
                    case (int)InCommandBase.CMD_GET_SETTINGS:
                        GCHandle handle = GCHandle.Alloc(pBuffIn, GCHandleType.Pinned);
                        SPORT_BASE_SETTINGS sbs = (SPORT_BASE_SETTINGS)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SPORT_BASE_SETTINGS));
                        handle.Free();
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            ShowBaseSettings(sbs);
                        }), null);
                        
                        break;
                    case (int)InCommandBase.CMD_GET_AKKVOLTAGE:
                        int iV = BitConverter.ToInt32(pBuffIn, 0);
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            this.label_battBase.Text = "" + ((float)iV / 1000000).ToString("F2") + " В.";
                        }), null);
                        break;
                    case (int)InCommandBase.CMD_GET_VERSION:
                        int iVer = BitConverter.ToInt32(pBuffIn, 0);
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            this.label_versBase.Text = "" + iVer.ToString() + ".";
                        }), null);
                        break;

                    case (int)InCommandBase.CMD_CLEAR_CARD:
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            //this.label1.Text = "OK.";
                        }), null);
                        break;
                    case (int)InCommandBase.CMD_WRITE_CARD_NUM:
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            //this.label2.Text = "OK.";
                        }), null);
                        break;
                    case (int)InCommandBase.CMD_READ_CARD:
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            ShowKMResult(pBuffIn);
                        }), null);
                        break;

                    default:
                        break;
                }                
            }
            else
            {
                switch (iCurrCommand)
                {
                    case (int)InCommandTag.CMD_GET_SETTINGS:
                        GCHandle handle = GCHandle.Alloc(pBuffIn, GCHandleType.Pinned);
                        SPORT_TAG_SETTINGS sbs = (SPORT_TAG_SETTINGS)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SPORT_TAG_SETTINGS));
                        handle.Free();

                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            ShowTagSettings(sbs);
                        }), null);
                        break;
                    case (int)InCommandTag.CMD_GET_AKKVOLTAGE:
                        int iV = BitConverter.ToInt32(pBuffIn, 0);
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            this.label_battTag.Text = "" + ((float)iV / 1000000).ToString("F2") + " В.";
                        }), null);
                        break;
                    case (int)InCommandTag.CMD_GET_VERSION:
                        int iVer = BitConverter.ToInt32(pBuffIn, 0);
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            this.label_versTag.Text = "" + iVer.ToString() + ".";
                        }), null);
                        break;
                    case (int)InCommandTag.CMD_READ_DATA:
                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            ShowTagResult(pBuffIn);
                        }), null);
                        break;
                    default:
                        break;
                }
            }

            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                this.label_status.Text = "OK.";
            }), null);

            iCurrCommand = -1;            
        }

        private void BLE_com_BuffError()
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                this.label_status.Text = "ERROR!";
            }), null);
            iCurrCommand = -1;
        }
        //========================================================================

        private void AddColumns()
        {
            dataGridView1.Columns.Add("columnName", "Имя");
            dataGridView1.Columns.Add("columnAddr", "Адрес");

            DataGridViewImageColumn imageColTime = new DataGridViewImageColumn();
            imageColTime.HeaderText = "Врм";
            imageColTime.ImageLayout = DataGridViewImageCellLayout.Stretch;
            imageColTime.Width = 40;
            dataGridView1.Columns.Add(imageColTime);

            DataGridViewImageColumn imageColBatt = new DataGridViewImageColumn();
            imageColBatt.HeaderText = "Бат";
            imageColBatt.ImageLayout = DataGridViewImageCellLayout.Stretch;
            imageColBatt.Width = 40;
            dataGridView1.Columns.Add(imageColBatt);

            DataGridViewImageColumn imageColAlarm = new DataGridViewImageColumn();
            imageColAlarm.HeaderText = "Авт";
            imageColAlarm.ImageLayout = DataGridViewImageCellLayout.Stretch;
            imageColAlarm.Width = 40;
            dataGridView1.Columns.Add(imageColAlarm);
        }

        private void WaitCurrComm()
        {
            int iCount = 0;
            while (iCurrCommand > 0)
            {
                Thread.Sleep(50);
                iCount++;
                if (iCount > 40) break;
            }
        }

        private void UpdateGridDevices()
        {
            dataGridView1.Rows.Clear();

            foreach (stMyBleDevice mbd in BLE_com.BleList.Values)
            {
                var row1 = new DataGridViewRow();
                //row1.Cells.Add(new DataGridViewImageCell { Value = new Bitmap("CHECK.ICO") });
                DataGridViewTextBoxCell celsName = new DataGridViewTextBoxCell();
                celsName.Value = mbd.sName;
                if (mbd.bIsActive) celsName.Style.BackColor = Color.LightGreen;
                row1.Cells.Add(celsName);
                row1.Cells.Add(new DataGridViewTextBoxCell { Value = mbd.sBleMacAddr });

                if (mbd.bIsTime) row1.Cells.Add(new DataGridViewImageCell { Value = new Bitmap(Properties.Resources.ok) });
                else row1.Cells.Add(new DataGridViewTextBoxCell { Value = "" });

                if (mbd.uAkk == 3)      row1.Cells.Add(new DataGridViewImageCell { Value = new Bitmap(Properties.Resources.bat80) });
                else if (mbd.uAkk == 2) row1.Cells.Add(new DataGridViewImageCell { Value = new Bitmap(Properties.Resources.bat50) });
                else if (mbd.uAkk == 1) row1.Cells.Add(new DataGridViewImageCell { Value = new Bitmap(Properties.Resources.bat20) });
                else if (mbd.uAkk == 0) row1.Cells.Add(new DataGridViewTextBoxCell { Value = "" });

                if (mbd.bIsAlarm) row1.Cells.Add(new DataGridViewImageCell { Value = new Bitmap(Properties.Resources.ok) });
                else row1.Cells.Add(new DataGridViewTextBoxCell { Value = "" });

                dataGridView1.Rows.Add(row1);
            }

            if (currSelectedRow != null)
            {
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    dataGridView1.Rows[i].Selected = false;
                    foreach (DataGridViewRow r in currSelectedRow)
                    {
                        if (r.Cells[1].Value.ToString() == dataGridView1.Rows[i].Cells[1].Value.ToString())
                            dataGridView1.Rows[i].Selected = true;
                    }

                }
            }

            this.dataGridView1.Refresh();
        }

        private void DisableButtons(bool bState)
        {
            foreach(Control c in this.panel_buttonBase.Controls)
            {
                c.Enabled = bState;
            }

            foreach (Control c in this.panel_buttonTag.Controls)
            {
                c.Enabled = bState;
            }
        }

        private void ShowBaseSettings(SPORT_BASE_SETTINGS sbs)
        {
            var dataSource = new List<BaseWorkType>();
            dataSource.Add(new BaseWorkType() { Name = "Стартовая", Value = 0 });
            dataSource.Add(new BaseWorkType() { Name = "Обычная", Value = 1 });
            dataSource.Add(new BaseWorkType() { Name = "Финишная", Value = 2 });
            dataSource.Add(new BaseWorkType() { Name = "Очистка", Value = 3 });
            dataSource.Add(new BaseWorkType() { Name = "Проверочная", Value = 4 });

            //Setup data binding
            this.comboBoxTypeBase.DataSource = dataSource;
            this.comboBoxTypeBase.DisplayMember = "Name";
            this.comboBoxTypeBase.ValueMember = "Value";

            this.numericUpDownGainKm.Value = sbs.gain_KM;

            this.numericUpDownPowerBleBase.Value = sbs.powerble_station;
            this.numericUpDownTimeoutBase.Value = sbs.timeut_station;
            this.numericUpDownTimerKm.Value = sbs.timer_KM;
            this.textBoxPasswordBase.Text = Encoding.Default.GetString(sbs.password_station);

            string sKey = "";
            foreach (byte b in sbs.ar_secure_key) sKey += b.ToString("X") + " ";

            this.textBoxKeyKM.Text = sKey;

            int wt = (int)sbs.type_station;
            this.comboBoxTypeBase.SelectedIndex = wt;
            this.numericUpDownNumBase.Value = sbs.num_station;
            if ((sbs.service1 & 0x01) == 1) this.checkBoxLedInverse.Checked = true;
            if (sbs.service2 > 99) sbs.service2 = 99;
            this.numericUpDownTimeWaitKM.Value = sbs.service2;



            //Form_SettingsBase fs = new Form_SettingsBase(s);
            //fs.StartPosition = FormStartPosition.Manual;
            //fs.Location = this.Location;
            //DialogResult dr = fs.ShowDialog();
            //if (dr == DialogResult.OK)
            //{
            //    //save settings
            //    byte[] ar2 = BLE_com.GetBytes(fs.returnSettings);
            //    SendCommand(InCommandBase.CMD_SET_SETTINGS, ar2);
            //}
        }

        private void ShowKMResult(byte[] res)
        {
            int iIndex = 4;
            byte[] qBaseTime = new byte[4];
            qBaseTime[3] = res[4];
            qBaseTime[2] = res[5];
            qBaseTime[1] = res[6];
            qBaseTime[0] = res[7];
            int iStartBlockTime = BitConverter.ToInt32(qBaseTime, 0);
            int ut01012019 = (int)(new DateTime(2019, 1, 1) - new DateTime(1970, 1, 1)).TotalSeconds;

            DateTime tStartBlockTime = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(iStartBlockTime);

            //
            DataSet ds = new DataSet();
            //DataTable dt;
            DataRow dr;
            DataColumn Numbase;
            DataColumn TimeBase;
            DataColumn TimeDelta;

            dt = new DataTable("Table1");
            Numbase = new DataColumn("NameBase", Type.GetType("System.String"));
            TimeBase = new DataColumn("Time", Type.GetType("System.String"));
            TimeDelta = new DataColumn("Delta", Type.GetType("System.String"));

            dt.Columns.Add(Numbase);
            dt.Columns.Add(TimeBase);
            dt.Columns.Add(TimeDelta);
            //


            if (iStartBlockTime > ut01012019)
            {
                int iNumbase = 0;
                DateTime tBaseTimePrev = tStartBlockTime;
                do
                {
                    iIndex += 4;

                    iNumbase = (int)res[iIndex];
                    if (iNumbase == 0) continue;

                    byte[] aBaseTime = new byte[4];
                    aBaseTime[3] = res[4];
                    aBaseTime[2] = res[iIndex + 1];
                    aBaseTime[1] = res[iIndex + 2];
                    aBaseTime[0] = res[iIndex + 3];

                    int iBaseTime = BitConverter.ToInt32(aBaseTime, 0);
                    DateTime tBaseTime = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(iBaseTime);


                    dr = dt.NewRow();
                    dr["NameBase"] = iNumbase.ToString();
                    if (iNumbase == 240) dr["NameBase"] = "Start";
                    if (iNumbase == 245) dr["NameBase"] = "Finish";
                    if (iNumbase == 248) dr["NameBase"] = "Check";
                    dr["Time"] = tBaseTime.ToString("dd.MM.yyyy hh:mm:ss");
                    dr["Delta"] = (tBaseTime - tBaseTimePrev).ToString();
                    dt.Rows.Add(dr);
                    tBaseTimePrev = tBaseTime;

                } while (iIndex < res.Length - 4);
                ds.Tables.Add(dt);

                this.dataGridView2.Visible = true;
                this.dataGridView2.AutoGenerateColumns = true;
                this.dataGridView2.DataSource = ds;
                this.dataGridView2.DataMember = "Table1";
                this.dataGridView2.Refresh();
            }
            else
            {
                //Блок не читается
                this.dataGridView2.Visible = false;
            }
            this.labelKmNumber.Text = "Карта № " + res[0].ToString();            
        }

        private void ExportDgvToXML(DataTable dt, string sFileName)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML|*.xml";
            sfd.FileName = sFileName;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    dt.WriteXml(sfd.FileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        //========================================================================
        #region Base_button
        private void Button_Base_Blink_Click(object sender, EventArgs e)
        {
            ButtonCommandSend(InCommandBase.CMD_SET_BLINK, null, false);
        }

        private async void Button_Base_Settings_Click(object sender, EventArgs e)
        {
            if (currSelectedRow == null) return;
            if (currSelectedRow.Count != 1)
            {
                MessageBox.Show("Выберите только одно устройство!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DisableButtons(false);

            this.panel_settingsBase.Dock = DockStyle.Fill;
            this.panel_settingsBase.BringToFront();
            this.panel_settingsBase.Enabled = true;

            stMyBleDevice mbd = BLE_com.GetMbdFromList(this.dataGridView1.SelectedRows[0].Cells[1].Value.ToString());
            if (mbd != null)
            {
                this.label_nameBase.Text = mbd.sName;
                this.label_MACBase.Text = mbd.sBleMacAddr;
                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandBase.CMD_NEXT;

                    SendCommand(InCommandBase.CMD_GET_VERSION, null);
                    WaitCurrComm();
                    SendCommand(InCommandBase.CMD_GET_AKKVOLTAGE, null);
                    WaitCurrComm();
                    SendCommand(InCommandBase.CMD_GET_SETTINGS, null);
                    WaitCurrComm();
                }

                BLE_com.CloseBle();
            }

            DisableButtons(true);
        }

        private void Button_Base_Sleep_Click(object sender, EventArgs e)
        {
            ButtonCommandSend(InCommandBase.CMD_MODE_WAIT, null, true);
        }

        private void Button_Base_Active_Click(object sender, EventArgs e)
        {
            ButtonCommandSend(InCommandBase.CMD_MODE_ACTIVE, null, true);
        }

        private async void Button_Base_TimeSet_Click(object sender, EventArgs e)
        {
            if (currSelectedRow == null) return;
            if (currSelectedRow.Count == 0) return;

            DisableButtons(false);

            Form_progress fp = null;
            if (currSelectedRow.Count > 1)
            {
                fp = new Form_progress(currSelectedRow.Count);
                fp.Location = this.Location;
                fp.Show();
            }
            foreach (DataGridViewRow r in currSelectedRow)
            {
                stMyBleDevice mbd = BLE_com.GetMbdFromList(r.Cells[1].Value.ToString());
                if (mbd == null) continue;

                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandBase.CMD_NEXT;

                    Int32 unixTimestamp = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    byte[] buf = BitConverter.GetBytes(unixTimestamp);

                    SendCommand(InCommandBase.CMD_SET_TIME, buf);
                    if (fp != null) fp.AddProgressValue();
                    WaitCurrComm();

                    BLE_com.DelFromList(mbd.sBleMacAddr);
                    UpdateGridDevices();
                }
                BLE_com.CloseBle();
            }

            if (fp != null) fp.Close();

            DisableButtons(true);
        }

        private void Button_Base_SetAlarm_Click(object sender, EventArgs e)
        {
            this.panel_alarmBase.Dock = DockStyle.Fill;
            this.panel_alarmBase.BringToFront();
            this.panel_alarmBase.Enabled = true;

            dateTimePickerRunStart.Value = DateTime.Now;
            dateTimePickerRunStop.Value = DateTime.Now;
        }

        private void Button_baseRunSetTime_Click(object sender, EventArgs e)
        {
            if(this.dateTimePickerRunStart.Value > this.dateTimePickerRunStop.Value)
            {
                MessageBox.Show("Время включения должно быть меньше, чем время выключения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Int32 unixTimestart = (Int32)(dateTimePickerRunStart.Value.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Int32 unixTimestop = (Int32)(dateTimePickerRunStop.Value.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            byte[] bufstart = BitConverter.GetBytes(unixTimestart);
            byte[] bufstop = BitConverter.GetBytes(unixTimestop);

            byte[] buf = new byte[8];

            Buffer.BlockCopy(bufstart, 0, buf, 0, 4);
            Buffer.BlockCopy(bufstop, 0, buf, 4, 4);

            ButtonCommandSend(InCommandBase.CMD_SET_TIMES_RUN, buf, true);
        }

        private void Button_Base_KM_Click(object sender, EventArgs e)
        {
            if (currSelectedRow == null) return;
            if (currSelectedRow.Count > 1)
            {
                MessageBox.Show("Выберите только одно устройство!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DisableButtons(false);
            this.panel_contaktBase.Dock = DockStyle.Fill;
            this.panel_contaktBase.BringToFront();
            this.panel_contaktBase.Enabled = true;

            this.dataGridView1.Enabled = false;
            this.button_showBase.Enabled = false;
            this.button_showTag.Enabled = false;
        }

        private void ButtonCancelBaseSettings_Click(object sender, EventArgs e)
        {
            this.panel_settingsBase.Enabled = false;
        }

        private void ButtonSaveBaseSettings_Click(object sender, EventArgs e)
        {
            SPORT_BASE_SETTINGS returnSettings = new SPORT_BASE_SETTINGS();

            returnSettings.mode_station = WORKMODE_BASE.MODE_ACTIVE;
            returnSettings.type_station = (BLE_setup.WORKTYPE)((BaseWorkType)this.comboBoxTypeBase.SelectedItem).Value;
            returnSettings.gain_KM = (byte)this.numericUpDownGainKm.Value;
            returnSettings.num_station = (byte)this.numericUpDownNumBase.Value;
            returnSettings.powerble_station = (byte)this.numericUpDownPowerBleBase.Value;
            returnSettings.timeut_station = (UInt32)this.numericUpDownTimeoutBase.Value;
            returnSettings.timer_KM = (UInt32)this.numericUpDownTimerKm.Value;
            returnSettings.service2 = (byte)this.numericUpDownTimeWaitKM.Value;
            returnSettings.password_station = new byte[10];
            Array.Copy(Encoding.Default.GetBytes(this.textBoxPasswordBase.Text), returnSettings.password_station, this.textBoxPasswordBase.Text.Length);

            this.textBoxKeyKM.Text = this.textBoxKeyKM.Text.TrimEnd(' ');
            string[] sKa = this.textBoxKeyKM.Text.Split(' ');
            if (sKa.Length != 6)
            {
                MessageBox.Show("В поле Ключи шифрования введите 6 чисел в шестнадцатиричном формате разделенных пробелами. Пример: C1 FF 35 01 AD 0E .");
                return;
            }
            returnSettings.ar_secure_key = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                returnSettings.ar_secure_key[i] = Convert.ToByte(sKa[i], 16);
            }
            returnSettings.signature = 223;
            if (this.checkBoxLedInverse.Checked) returnSettings.service1 = 1;

            byte[] ar2 = BLE_com.GetBytes(returnSettings);
            ButtonCommandSend(InCommandBase.CMD_SET_SETTINGS, ar2, false);
        }
        
        private async void Button_readKMcard_Click(object sender, EventArgs e)
        {
            stMyBleDevice mbd = BLE_com.GetMbdFromList(this.dataGridView1.SelectedRows[0].Cells[1].Value.ToString());
            if (mbd != null)
            {
                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandBase.CMD_NEXT;

                    SendCommand(InCommandBase.CMD_READ_CARD, null);
                    WaitCurrComm();
                }

                BLE_com.CloseBle();
            }
        }

        private async void Button_clearKMcard_Click(object sender, EventArgs e)
        {
            stMyBleDevice mbd = BLE_com.GetMbdFromList(this.dataGridView1.SelectedRows[0].Cells[1].Value.ToString());
            if (mbd != null)
            {
                ;
                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandBase.CMD_NEXT;

                    SendCommand(InCommandBase.CMD_CLEAR_CARD, null);
                    WaitCurrComm();
                }

                BLE_com.CloseBle();
            }
        }

        private async void Button_writeKMcardNum_Click(object sender, EventArgs e)
        {
            stMyBleDevice mbd = BLE_com.GetMbdFromList(this.dataGridView1.SelectedRows[0].Cells[1].Value.ToString());
            if (mbd != null)
            {
                ;
                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandBase.CMD_NEXT;

                    byte[] nblk = BitConverter.GetBytes((Int32)this.numericUpDownNumCard.Value);
                    SendCommand(InCommandBase.CMD_WRITE_CARD_NUM, nblk);
                    WaitCurrComm();
                }

                BLE_com.CloseBle();
            }
            
        }

        private async void Button_writeKMcardKEY_Click(object sender, EventArgs e)
        {
            stMyBleDevice mbd = BLE_com.GetMbdFromList(this.dataGridView1.SelectedRows[0].Cells[1].Value.ToString());
            if (mbd != null)
            {
                ;
                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandBase.CMD_NEXT;

                    byte[] nblk = BitConverter.GetBytes((Int32)16776960);
                    SendCommand(InCommandBase.CMD_WRITE_CARD_NUM, nblk);
                    WaitCurrComm();
                }

                BLE_com.CloseBle();
            }
        }

        private void Button_closeKMcard_Click(object sender, EventArgs e)
        {
            
            this.panel_contaktBase.Enabled = false;
            this.dataGridView1.Enabled = true;


            this.button_showBase.Enabled = true;
            this.button_showTag.Enabled = true;

            this.dataGridView2.DataSource = null;
            this.dataGridView2.Rows.Clear();
            this.dataGridView2.Refresh();

            DisableButtons(true);
        }

        private void Button_saveKMresult_Click(object sender, EventArgs e)
        {
            string s = "";
            s = this.labelKmNumber.Text.Trim();
            ExportDgvToXML(dt, s);
        }

        private void comboBoxType_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (((BaseWorkType)this.comboBoxTypeBase.SelectedItem).Name)
            {
                case "Стартовая":
                    this.numericUpDownNumBase.Value = 240;
                    break;

                case "Финишная":
                    this.numericUpDownNumBase.Value = 245;
                    break;

                case "Очистка":
                    this.numericUpDownNumBase.Value = 249;
                    break;

                case "Проверочная":
                    this.numericUpDownNumBase.Value = 248;
                    break;

                case "Обычная":
                    this.numericUpDownNumBase.Value = 1;
                    break;
            }
        }

        #endregion
        //========================================================================
        #region Tag_button
        private void Button_Tag_Blink_Click(object sender, EventArgs e)
        {
            ButtonCommandSend(InCommandTag.CMD_SET_BLINK, null, false);
        }

        private async void Button_Tag_Settings_Click(object sender, EventArgs e)
        {
            if (currSelectedRow == null) return;
            if (currSelectedRow.Count != 1)
            {
                MessageBox.Show("Выберите только одно устройство!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DisableButtons(false);

            this.panel_settingsTag.Dock = DockStyle.Fill;
            this.panel_settingsTag.BringToFront();
            this.panel_settingsTag.Enabled = true;

            stMyBleDevice mbd = BLE_com.GetMbdFromList(this.dataGridView1.SelectedRows[0].Cells[1].Value.ToString());
            if (mbd != null)
            {
                this.label_nameTag.Text = mbd.sName;
                this.label_MACTag.Text = mbd.sBleMacAddr;
                if (await BLE_com.OpenBle(mbd.sBleId, mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandTag.CMD_NEXT;

                    SendCommand(InCommandTag.CMD_GET_VERSION, null);
                    WaitCurrComm();
                    SendCommand(InCommandTag.CMD_GET_AKKVOLTAGE, null);
                    WaitCurrComm();
                    SendCommand(InCommandTag.CMD_GET_SETTINGS, null);
                    WaitCurrComm();
                }

                BLE_com.CloseBle();
            }

            DisableButtons(true);
        }

        private void Button_Tag_Sleep_Click(object sender, EventArgs e)
        {
            ButtonCommandSend(InCommandTag.CMD_SET_MODE_SLEEP, null, true);
        }

        private void Button_Tag_ModeRUN_Click(object sender, EventArgs e)
        {
            ButtonCommandSend(InCommandTag.CMD_SET_MODE_RUN, null, true);
        }

        byte[] nblk = new byte[1];
        DataTable dt;
        stMyBleDevice curr_mbd;
        private async void Button_Tag_GetData_Click(object sender, EventArgs e)
        {
            if (currSelectedRow == null) return;
            if (currSelectedRow.Count > 1)
            {
                MessageBox.Show("Выберите только одно устройство!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DisableButtons(false);
            this.panel_resultTag.Dock = DockStyle.Fill;
            this.panel_resultTag.BringToFront();
            this.panel_resultTag.Enabled = true;

            this.dataGridView1.Enabled = false;
            this.button_showBase.Enabled = false;
            this.button_showTag.Enabled = false;


            curr_mbd = BLE_com.GetMbdFromList(this.dataGridView1.SelectedRows[0].Cells[1].Value.ToString());
            if (curr_mbd != null)
            {
                if (await BLE_com.OpenBle(curr_mbd.sBleId, curr_mbd.type))
                {
                    BLE_com.cCmdNext = (byte)InCommandTag.CMD_NEXT;

                    nblk[0] = 2;
                    SendCommand(InCommandTag.CMD_READ_DATA, nblk);
                    WaitCurrComm();
                }
            }
        }

        private void ButtonZabegPrev_Click(object sender, EventArgs e)
        {
            if (nblk[0] < 2) return;

            EnableButtonsZabeg(false);
            nblk[0] -= 1;
            SendCommand(InCommandTag.CMD_READ_DATA, nblk);
        }

        private void ButtonZabegNext_Click(object sender, EventArgs e)
        {
            if (nblk[0] > 13) return;

            EnableButtonsZabeg(false);
            nblk[0] += 1;
            SendCommand(InCommandTag.CMD_READ_DATA, nblk);
        }

        private void ButtonSaveTagResult_Click(object sender, EventArgs e)
        {
            string s = "";
            if(curr_mbd != null) s = curr_mbd.sBleMacAddr + "_" + curr_mbd.sName.Trim(' ') + "_Забег_" + (nblk[0]-1).ToString();
            ExportDgvToXML(dt, s);
        }

        private void EnableButtonsZabeg(bool b)
        {
            if (!b)
            {
                this.buttonZabegNext.Enabled = false;
                this.buttonZabegPrev.Enabled = false;
            }
            else
            {
                this.buttonZabegNext.Enabled = true;
                this.buttonZabegPrev.Enabled = true;
                this.buttonSaveTagResult.Enabled = true;

                if (nblk[0] < 3) this.buttonZabegPrev.Enabled = false;
                if (nblk[0] > 13) this.buttonZabegNext.Enabled = false;
            }
        }

        private void Button_closeResultTag_Click(object sender, EventArgs e)
        {
            BLE_com.CloseBle();

            curr_mbd = null;

            this.panel_resultTag.Enabled = false;

            this.dataGridView1.Enabled = true;
            this.button_showBase.Enabled = true;
            this.button_showTag.Enabled = true;

            this.dataGridView3.DataSource = null;
            this.dataGridView3.Rows.Clear();
            this.dataGridView3.Refresh();

            DisableButtons(true);
        }
        
        private void ShowTagResult(byte[] res)
        {
            int iIndex = 0;
            int iStartBlockTime = BitConverter.ToInt32(res, iIndex);
            int ut01012019 = (int)(new DateTime(2019, 1, 1) - new DateTime(1970, 1, 1)).TotalSeconds;

            DateTime tStartBlockTime = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(iStartBlockTime);

            //
            DataSet ds = new DataSet();

            DataRow dr;
            DataColumn Numbase;
            DataColumn TimeBase;
            DataColumn TimeDelta;

            dt = new DataTable("Table1");
            Numbase = new DataColumn("NameBase", Type.GetType("System.String"));
            TimeBase = new DataColumn("Time", Type.GetType("System.String"));
            TimeDelta = new DataColumn("Delta", Type.GetType("System.String"));

            dt.Columns.Add(Numbase);
            dt.Columns.Add(TimeBase);
            dt.Columns.Add(TimeDelta);
            //


            if (iStartBlockTime > ut01012019)
            {
                int iNumbase = 0;
                DateTime tBaseTimePrev = tStartBlockTime;
                do
                {
                    iIndex += 4;

                    iNumbase = (int)res[iIndex];
                    if (iNumbase == 0) break;

                    byte[] aBaseTime = new byte[4];
                    aBaseTime[3] = res[3];
                    aBaseTime[2] = res[iIndex + 1];
                    aBaseTime[1] = res[iIndex + 2];
                    aBaseTime[0] = res[iIndex + 3];

                    int iBaseTime = BitConverter.ToInt32(aBaseTime, 0);
                    DateTime tBaseTime = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddSeconds(iBaseTime);


                    dr = dt.NewRow();
                    dr["NameBase"] = iNumbase.ToString();
                    if (iNumbase == 240) dr["NameBase"] = "Start";
                    if (iNumbase == 245) dr["NameBase"] = "Finish";
                    if (iNumbase == 248) dr["NameBase"] = "Check";
                    dr["Time"] = tBaseTime.ToString("dd.MM.yyyy hh:mm:ss");
                    dr["Delta"] = (tBaseTime - tBaseTimePrev).ToString();
                    dt.Rows.Add(dr);
                    tBaseTimePrev = tBaseTime;

                } while (iIndex < res.Length);
                ds.Tables.Add(dt);

                this.dataGridView3.Visible = true;
                this.dataGridView3.AutoGenerateColumns = true;
                this.dataGridView3.DataSource = ds;
                this.dataGridView3.DataMember = "Table1";
                this.dataGridView3.Refresh();
            }
            else
            {
                //Блок не читается
                this.dataGridView3.Visible = false;
            }
            this.labelNumZabeg.Text = (nblk[0]-1).ToString();
            EnableButtonsZabeg(true);
        }

        private void ShowTagSettings(SPORT_TAG_SETTINGS sts)
        {
            this.textBoxNameTag.Text = Encoding.Default.GetString(sts.name_tag);
            this.numericUpDown_timeOutTag.Value = CheckNUDvalue((int)sts.timeut_conn, this.numericUpDown_timeOutTag);
            this.numericUpDownTresholdTag.Value = CheckNUDvalue((int)sts.treshold_tag, this.numericUpDownTresholdTag);
            this.textBoxPasswordTag.Text = Encoding.Default.GetString(sts.password_tag);

            this.textBoxFamUserTag.Text = Encoding.Default.GetString(sts.fam);
            this.textBoxImjaUserTag.Text = Encoding.Default.GetString(sts.imj);
            this.textBoxOtchUserTag.Text = Encoding.Default.GetString(sts.otch);
            this.numericUpDownGodRojdUserTag.Value = CheckNUDvalue((int)sts.godrojd, this.numericUpDownGodRojdUserTag);
            this.textBoxColectivTag.Text = Encoding.Default.GetString(sts.colectiv);
            if (sts.arenda > 0) this.checkBoxClubTag.Checked = true;
        }

        private int CheckNUDvalue(int bin, NumericUpDown nud)
        {
            int iRet = bin;

            if (bin > nud.Maximum) iRet = (int)nud.Maximum;
            if (bin < nud.Minimum) iRet = (int)nud.Minimum;

            return iRet;
        }

        private void ButtonSaveSettingsTag_Click(object sender, EventArgs e)
        {
            SPORT_TAG_SETTINGS returnSettings = new SPORT_TAG_SETTINGS();

            returnSettings.name_tag = new byte[20];
            Array.Copy(Encoding.Default.GetBytes(this.textBoxNameTag.Text), returnSettings.name_tag, this.textBoxNameTag.Text.Length);
            returnSettings.timeut_conn = (Int32)this.numericUpDown_timeOutTag.Value;
            returnSettings.treshold_tag = (sbyte)this.numericUpDownTresholdTag.Value;
            returnSettings.password_tag = new byte[10];
            Array.Copy(Encoding.Default.GetBytes(this.textBoxPasswordTag.Text), returnSettings.password_tag, this.textBoxPasswordTag.Text.Length);

            returnSettings.fam = new byte[20];
            Array.Copy(Encoding.Default.GetBytes(this.textBoxFamUserTag.Text), returnSettings.fam, this.textBoxFamUserTag.Text.Length);
            returnSettings.imj = new byte[20];
            Array.Copy(Encoding.Default.GetBytes(this.textBoxImjaUserTag.Text), returnSettings.imj, this.textBoxImjaUserTag.Text.Length);
            returnSettings.otch = new byte[20];
            Array.Copy(Encoding.Default.GetBytes(this.textBoxOtchUserTag.Text), returnSettings.otch, this.textBoxOtchUserTag.Text.Length);
            returnSettings.godrojd = (UInt16)this.numericUpDownGodRojdUserTag.Value;
            returnSettings.colectiv = new byte[20];
            Array.Copy(Encoding.Default.GetBytes(this.textBoxColectivTag.Text), returnSettings.colectiv, this.textBoxColectivTag.Text.Length);

            if (this.checkBoxClubTag.Checked) returnSettings.arenda = 1;
            else returnSettings.arenda = 0;

            returnSettings.mode_tag = WORKMODE_TAG.MODE_CONNECT;
            returnSettings.powerble_tag = 5;
            returnSettings.group = new byte[4];
            returnSettings.razr = new byte[4];
            returnSettings.zabeg = 0;
            returnSettings.startnum = 0;
            returnSettings.starttime = new byte[7];
            returnSettings.lgota = 0;

            returnSettings.signature = 223;

            if (currSelectedRow == null) return;
            if (currSelectedRow.Count != 1)
            {
                MessageBox.Show("Выберите только одно устройство!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] ar2 = BLE_com.GetBytes(returnSettings);
            ButtonCommandSend(InCommandTag.CMD_SET_SETTINGS, ar2, false);
        }

        private void ButtonCancelSettingsTag_Click(object sender, EventArgs e)
        {
            this.panel_settingsTag.Enabled = false;
        }
        
        #endregion
        //========================================================================



    }
}
