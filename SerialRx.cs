using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.ComponentModel;
using System.Xml;
using System.IO;
using System.Windows.Controls;
using looking_glass_api.DataObjects;
namespace Logger
{
    public class SerialRx : INotifyPropertyChanged
    {
        public delegate void mt(string val);
        public event mt LogText;
        public TextBlock tb { get; set; }
        public List<string> ports { get { return pp; } set { pp = value; OnPropertyChanged("ports"); } }
        List<string> pp = new List<string>();
        public string status { get { return sp; } set { sp = value; OnPropertyChanged("status"); } }
        string sp;
        public string rx { get { return rp; } set { rp = value; OnPropertyChanged("rx"); } }
        string rp;
        public SerialRx()
        {
            ports = System.IO.Ports.SerialPort.GetPortNames().ToList();
        }
        public void Refresh()
        {
            ports = System.IO.Ports.SerialPort.GetPortNames().ToList();
        }
        static SerialPort port { get; set; }
        public void Write(string data)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    port.Write(data);
                }
            }
            catch (Exception ex)
            {
            }
        }
        public void Connect(string port_name)
        {
            try
            {
                port = new SerialPort(port_name, 9600, Parity.None, 8, StopBits.One);
                port.DataReceived += port_DataReceived;
                port.Open();
                status = "Connected to : " + port_name;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public DataSet ds { get; set; }
        int dayOld = -1;
        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(100);
            int toRead = ((SerialPort)sender).BytesToRead;
            byte[] byte_buffer = new byte[toRead];
            port.Read(byte_buffer, 0, toRead);
            var data = System.Text.Encoding.Default.GetString(byte_buffer);
            //LogText(data.ToString());
            //LogText(data);
            Console.WriteLine(data.ToString());
            XmlNode node;
            int dayNow = DateTime.Now.Day;
            if (ds == null)
            {
                ds = new DataSet();
                ds.SetFolderPath(@"c:\datalog");
                ds.AttachMeta("Start Time", DateTime.Now);
                dayOld = dayNow;
            }
            else if (dayNow != dayOld)
            {
                ds.EndWrite();
                ds = new DataSet();
                ds.SetFolderPath(@"c:\datalog");
                ds.AttachMeta("Start Time", DateTime.Now);
                dayOld = dayNow;
            }
            bool any = false;
            //parse the data.
            try
            {
                using (XmlReader reader = XmlReader.Create(new StringReader(data.ToString())))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            //Console.WriteLine(reader.LocalName);
                            if (reader.LocalName == "Data")
                            {
                                if (!any)
                                {
                                    ds.AddParameterDate("Timestamp", DateTime.Now);
                                    any = true;
                                }
                                var val = reader.GetAttribute("Value");
                                //Console.WriteLine(reader.LocalName + ": " + val);
                                ds.AddParameterDouble(reader.LocalName, Convert.ToDouble(val));
                                LogText(val.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { }
            if (any = true && ds != null)
            {
                ds.EndItem();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// notify bindings of property changed.
        /// </summary>
        /// <param name="name"></param>
        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
