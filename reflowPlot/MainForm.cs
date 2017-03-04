using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Globalization;

namespace reflowPlot
{
    public partial class MainForm : Form
    {
        private List<double> temperatureXList = new List<double>();
        private List<double> temperatureYList = new List<double>();

        private List<double> errorXList = new List<double>();
        private List<double> errorYList = new List<double>();

        private List<double> servoXList = new List<double>();
        private List<double> servoYList = new List<double>();

        private List<double> heaterXList = new List<double>();
        private List<double> heaterYList = new List<double>();

        private List<double> targetXList = new List<double>();
        private List<double> targetYList = new List<double>();

        string tempSeriesName = "Temperature";
        string errorSeriesName = "Error";
        string servoSeriesName = "Servo";
        string heaterSeriesName = "Heater";
        string targetSeriesName = "Target temp";

        double lastTimeValue = -1;

        BackgroundWorker comLineReader;

        SerialPort serialPort;
        string serialPortLine = "";
        bool serialConnected = false;

        public MainForm()
        {
            InitializeComponent();

            connectButton.Enabled = true;
            disconnectButton.Enabled = false;

            lastTimeValue = -1;
        }

        private void AddTempValue(double time, double temp)
        {
            temperatureXList.Add(time);
            temperatureYList.Add(temp);

            if (temperatureChart.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate { UpdateTempChart(); });
            }
        }

        private void UpdateTempChart()
        {
            temperatureChart.Series[tempSeriesName].Points.Clear();

            for (int i = 0; i != temperatureXList.Count; ++i)
            {
                temperatureChart.Series[tempSeriesName].Points.AddXY(temperatureXList[i], temperatureYList[i]);
            }
        }

        private void AddTargetValue(double time, double temp)
        {
            targetXList.Add(time);
            targetYList.Add(temp);

            if (temperatureChart.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate { UpdateTargetChart(); });
            }
        }

        private void UpdateTargetChart()
        {
            temperatureChart.Series[targetSeriesName].Points.Clear();

            for (int i = 0; i != targetXList.Count; ++i)
            {
                temperatureChart.Series[targetSeriesName].Points.AddXY(targetXList[i], targetYList[i]);
            }
        }

        private void AddErrorValue(double time, double val)
        {
            errorXList.Add(time);
            errorYList.Add(val);

            if (errorChart.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate { UpdateErrorChart(); });
            }
        }

        private void UpdateErrorChart()
        {
            errorChart.Series[errorSeriesName].Points.Clear();

            for (int i = 0; i != errorXList.Count; ++i)
            {
                errorChart.Series[errorSeriesName].Points.AddXY(errorXList[i], errorYList[i]);
            }
        }

        private void AddServoValue(double time, double val)
        {
            servoXList.Add(time);
            servoYList.Add(val);

            if (servoChart.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate { UpdateServoChart(); });
            }
        }

        private void UpdateServoChart()
        {
            servoChart.Series[servoSeriesName].Points.Clear();

            for (int i = 0; i != servoXList.Count; ++i)
            {
                servoChart.Series[servoSeriesName].Points.AddXY(servoXList[i], servoYList[i]);
            }
        }

        private void AddHeaterValue(double time, double val)
        {
            heaterXList.Add(time);
            heaterYList.Add(val);

            if (heaterChart.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate { UpdateHeaterChart(); });
            }
        }

        private void UpdateHeaterChart()
        {
            heaterChart.Series[heaterSeriesName].Points.Clear();

            for (int i = 0; i != heaterXList.Count; ++i)
            {
                heaterChart.Series[heaterSeriesName].Points.AddXY(heaterXList[i], heaterYList[i]);
            }
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort = new SerialPort();
                serialPort.BaudRate = 9600;
                serialPort.DataBits = 8;
                serialPort.Handshake = Handshake.None;
                serialPort.Parity = Parity.None;
                serialPort.PortName = "COM" + comNumericUpDown.Value;
                serialPort.StopBits = StopBits.One;
                serialPort.NewLine = "\n";
                serialPort.Open();

                serialConnected = true;
                connectButton.Enabled = false;
                disconnectButton.Enabled = true;

                comLineReader = new BackgroundWorker();
                comLineReader.DoWork += new DoWorkEventHandler(DoComPortReadWork);
                comLineReader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ComPortReadComplete);
                comLineReader.RunWorkerAsync();
            }
            catch (Exception)
            {
                ;
            }
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {
            disconnectComPort();
        }

        private void disconnectComPort()
        {
            try
            {
                serialPort.Close();
            }
            catch (Exception) { }

            serialConnected = false;

            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
        }

        private void DoComPortReadWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                this.serialPortLine = this.serialPort.ReadLine();
            }
            catch (InvalidOperationException)
            {
                disconnectComPort();
            }
            catch (TimeoutException)
            {
                ;
            }
            catch (Exception)
            {
                e.Result = new Boolean();
                e.Result = false;
            }
        }

        private void ComPortReadComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((null != e.Result) && ((Boolean)e.Result).Equals(false))
            {
                disconnectComPort();
                return;
            }

            string[] values = this.serialPortLine.Split(';');

            // The values are formated as:
            //temperature;time;heater duty;servo pos;target temp
            double temp = 0;
            double time = 0;
            double heater = 0;
            double servo = 0;
            double targetTemp = 0;

            if (5 == values.Length)
            {
                bool parseOk = false;

                try
                {
                    temp = Double.Parse(values[0], CultureInfo.InvariantCulture);
                    time = Double.Parse(values[1], CultureInfo.InvariantCulture);
                    heater = Double.Parse(values[2], CultureInfo.InvariantCulture);
                    servo = Double.Parse(values[3], CultureInfo.InvariantCulture);
                    targetTemp = Double.Parse(values[4], CultureInfo.InvariantCulture);

                    parseOk = true;
                }
                catch (Exception)
                {
                    parseOk = false;
                }

                if (parseOk)
                {
                    if (lastTimeValue > time)
                    {
                        temperatureXList.Clear();
                        temperatureYList.Clear();
                        heaterXList.Clear();
                        heaterYList.Clear();
                        servoXList.Clear();
                        servoYList.Clear();
                        errorXList.Clear();
                        errorYList.Clear();
                        targetXList.Clear();
                        targetYList.Clear();
                    }

                    AddTempValue(time, temp);
                    AddHeaterValue(time, heater);
                    AddServoValue(time, servo);
                    AddErrorValue(time, temp - targetTemp);
                    AddTargetValue(time, targetTemp);

                    lastTimeValue = time;
                }
            }

            if (serialConnected)
            {
                comLineReader.RunWorkerAsync();
            }
        }
    }
}
