using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using looking_glass_api.DataObjects;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Newtonsoft.Json;
namespace Logger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class UcSetting
        {
            //[JsonIgnore]
            private string name;
            public string Name
            {
                get { return name; }
                set { name = value; }
            }
            //[JsonIgnore]
            private int val = 0;
            public int Value
            {
                get { return val; }
                set { val = value; }
            }
            //[JsonIgnore]
            private int max = 0;
            public int Max
            {
                get { return max; }
                set { max = value; }
            }
            //[JsonIgnore]
            private int min = 0;
            public int Min
            {
                get { return min; }
                set { min = value; }
            }
            public void Tx()
            {
                Console.WriteLine("Name:" + Name + " Value:" + Value);
               
            }
        }
        public SerialRx srx { get { return sp; } set { sp = value; } }
        public  SerialRx sp;
        private ObservableCollection<UcSetting> items = new ObservableCollection<UcSetting>();
        
        public  void Write(string data){
            srx.Write(data);
        }
        
        public ObservableCollection<UcSetting> itemsList
        {
            get { return items; }
            set { items = value; }
        }
        public string SettingsFile = "settings";
        public void GetSettings()
        {
            if (File.Exists(SettingsFile))
            {
                using (FileStream filein = new FileStream(SettingsFile, FileMode.Open))
                {
                    TextReader tr = new StreamReader(filein);
                    string jsonString = tr.ReadToEnd();
                    itemsList =  JsonConvert.DeserializeObject<ObservableCollection<UcSetting>>(jsonString);
                }
            }
            else if(itemsList.Count==0)
            {
                itemsList.Add(new UcSetting() { Name = "T0:", Max = 1023 });
                itemsList.Add(new UcSetting() { Name = "Pre(0):", Max = 1023 });
                itemsList.Add(new UcSetting() { Name = "M:", Max = 255 });
                itemsList.Add(new UcSetting() { Name = "C:", Max = 255 });
            }
        }
        public MainWindow()
        {
            GetSettings();
            InitializeComponent();
            srx = new SerialRx();
            srx.LogText += srx_LogText;
            coms.ItemsSource = srx.ports;
            connectedTo.Text = srx.status;
            logButton.Click += logButton_Click;
            Closing += MainWindow_Closing;
        }
        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            using (FileStream fileout = new FileStream(SettingsFile, FileMode.Create))
            {
                TextWriter tx = new StreamWriter(fileout);
                string aettingsJson = JsonConvert.SerializeObject(itemsList);
                tx.Write(aettingsJson);
                tx.Close();
            }
            if (srx.ds != null)
            {
                srx.ds.EndWrite();
            }
        }
        double gatef = 40;
        double countval;
        void srx_LogText(string val)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                log.Add(DateTime.Now + " : " + val);
                rx.ItemsSource = log;
                //if(Double.TryParse(gateFreq.Text.ToString(),out gatef ) ){
                //}
                if (Double.TryParse(val, out countval))
                {
                    currentCount.Text = (countval * gatef).ToString();
                }
                datetCount.Text = DateTime.Now.ToString();
                try
                {
                    ListBoxAutomationPeer svAutomation = (ListBoxAutomationPeer)ScrollViewerAutomationPeer.CreatePeerForElement(rx);
                    IScrollProvider scrollInterface = (IScrollProvider)svAutomation.GetPattern(PatternInterface.Scroll);
                    System.Windows.Automation.ScrollAmount scrollVertical = System.Windows.Automation.ScrollAmount.LargeIncrement;
                    System.Windows.Automation.ScrollAmount scrollHorizontal = System.Windows.Automation.ScrollAmount.NoAmount;
                    //If the vertical scroller is not available, the operation cannot be performed, which will raise an exception. 
                    if (scrollInterface.VerticallyScrollable)
                        scrollInterface.Scroll(scrollHorizontal, scrollVertical);
                }
                catch (Exception ex)
                {
                }
                try
                {
                    using (FileStream fileIn = new FileStream(@"c:\dataLog\log1.csv", FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileIn))
                        {
                            if (overFlow.Count > 0)
                            {
                                overFlow.ForEach(o => sw.Write(o.First + " : " + o.Second));
                                overFlow.Clear();
                            }
                            sw.Write(DateTime.Now + " : " + val);
                        }
                    }
                }
                catch (Exception ex)
                {
                    overFlow.Add(new Pair<DateTime, string>(DateTime.Now, val));
                }
            }));
        }
        List<Pair<DateTime, string>> overFlow = new List<Pair<DateTime, string>>();
        ObservableCollection<string> log = new ObservableCollection<string>();
        void logButton_Click(object sender, RoutedEventArgs e)
        {
            srx.Connect(coms.SelectedItem.ToString());
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var setting = (sender as Button).DataContext as UcSetting;
            setting.Tx();
        }
        private void Apply_All(object sender, RoutedEventArgs e)
        {
            itemsList.ToList().ForEach(s =>
            {
                s.Tx(); System.Threading.Thread.Sleep(100);
                Write("{"+s.Name+":"+s.Value+"}");
            });
        }

        private void SetTime(object sender, RoutedEventArgs e)
        {
            var time = "{Time:" + DateTime.Now.ToString("yyyyMMddHHmmss") + "}";
            Write(time);
        }
    }
}