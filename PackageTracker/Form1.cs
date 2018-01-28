using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PackageTracker
{
    public partial class Form1 : Form
    {
        CaptureDeviceList deviceList;
        bool detected = false;
        string ip = String.Empty;
        string packagesInfo = String.Empty;
        List<string> dstIps = new List<string>();
        public delegate void InvokeDelegate();
        bool detectedOnce = false;
        ToolTip tt;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            // метод для получения списка устройств
            deviceList = CaptureDeviceList.Instance;

            int i = 0;

            foreach (var item in deviceList)
            {
                ICaptureDevice captureDevice = deviceList[i];
                captureDevice.OnPacketArrival += new PacketArrivalEventHandler(Program_OnPacketArrival);
                captureDevice.Open(DeviceMode.Promiscuous, 1000);

                captureDevice.StartCapture();

                Thread.Sleep(1000);
                captureDevice.OnPacketArrival -= new PacketArrivalEventHandler(Program_OnPacketArrival);

                if (ip != "")
                {
                    listBox1.Items.Add("№" + i + " - " + ip);
                }
                i++;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.ReadOnly = true;
            detectedOnce = false;
            label5.Text = "Scanning...";
            button1.Enabled = false;
            button3.Enabled = false;
            if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("You did not select the device to be scanned in the list on the left.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                progressBar1.Value = 0;
                button1.Enabled = true;
                button3.Enabled = true;
                timer1.Stop();
                label5.Text = String.Empty;
                return;
            }
            int deviceSelected = listBox1.FindString(listBox1.SelectedItem.ToString());
            //textBox2.Clear();
            PacketArrivalEventHandler emptyHandler = null;
            // выбираем первое устройство в спсике (для примера)
            ICaptureDevice captureDevice = deviceList[deviceSelected];
            // регистрируем событие, которое срабатывает, когда пришел новый пакет

            captureDevice.OnPacketArrival += new PacketArrivalEventHandler(Program_OnPacketArrival);
            // открываем в режиме promiscuous, поддерживается также нормальный режим
            captureDevice.Open(DeviceMode.Promiscuous, 1000);
            // начинаем захват пакетов
            (new Thread(delegate ()
            {
                packagesInfo = String.Empty;
                captureDevice.StartCapture();
                Thread.Sleep(30200);

                captureDevice.OnPacketArrival -= new PacketArrivalEventHandler(Program_OnPacketArrival);
                //textBox2.Text = packageInfo;
                textBox2.BeginInvoke(new InvokeDelegate(InvokeMethod));
                textBox1.BeginInvoke(new InvokeDelegate(InvokeMethodResult));

            })).Start();
            timer1.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.ScrollBars = ScrollBars.Vertical;
        }

        void Program_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            PacketArrivalEventHandler empty = null;
            // парсинг всего пакета
            Packet packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            // получение только IP пакета из всего фрейма
            var ipPacket = IpPacket.GetEncapsulated(packet);
            var tcpPacket = TcpPacket.GetEncapsulated(packet);

            if (ipPacket != null)
            {
                DateTime time = e.Packet.Timeval.Date;
                int len = e.Packet.Data.Length;
                // IP адрес отправителя
                var srcIp = ipPacket.SourceAddress.ToString();
                // IP адрес получателя
                var dstIp = ipPacket.DestinationAddress.ToString();
                dstIps.Add(dstIp);
                //MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                //var result = MessageBox.Show("Отправитель - " + srcIp + "\n" + "Адресат - " + dstIp, "Package is detected!");
                if (srcIp.Substring(0, 3) == "192")
                {
                    ip = srcIp;
                }

                if (dstIp.Substring(0, 3) == "192")
                {
                    ip = dstIp;
                }

                if (srcIp == textBox2.Text || dstIp == textBox2.Text)
                {
                    detected = true;
                }

                string port = "";
                if (tcpPacket != null)
                    port = tcpPacket.SourcePort.ToString();

                if (detected)
                {
                    //listBox2.Items.Add("Отправитель - " + srcIp + "; Адресат - " + dstIp + ";");
                    packagesInfo += "Отправитель - " + srcIp + "; Адресат - " + dstIp + ";" + " Порт: " + port + ";" + Environment.NewLine;
                    detectedOnce = true;
                }
            }
        }
        public void InvokeMethod()
        {
            if (detectedOnce)
            {
                textBox1.BackColor = Color.Red;
                textBox1.Text = packagesInfo;
            }
        }

        public void InvokeMethodResult()
        {
            if (!detectedOnce)
            {
                MessageBox.Show("Suspicious devices are not detected.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value < 300)
            {
                progressBar1.Value = progressBar1.Value + 1;
            }
            else
            {
                textBox2.ReadOnly = false;
                label5.Text = String.Empty;
                progressBar1.Value = 0;
                button1.Enabled = true;
                button3.Enabled = true;
                timer1.Stop();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            button2.Enabled = true;
        }

        private void textBox1_MouseHover(object sender, EventArgs e)
        {
            tt = new ToolTip();
            tt.Show("Select the package for more information about it.", textBox1);
        }
    }
}
