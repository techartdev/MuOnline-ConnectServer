using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConnectServer
{
    /// <summary>
    /// Interaction logic for GameServerItem.xaml
    /// </summary>
    public partial class GameServerItem : UserControl, INotifyPropertyChanged
    {
        #region Private Properties
        private string _serverName = "";
        private int _percent = 0;
        private short _userCount = 0;
        private bool _isAlive = false;
        #endregion

        #region Public Properties
        public string ServerName
        {
            get
            {
                return _serverName;
            }
            set
            {
                _serverName = value;
                OnPropertyChanged("ServerName");
            }
        }

        public int ServerCode { get; set; }

        public IPAddress IPAddress { get; set; }

        public int Port { get; set; }

        public bool Show { get; set; }

        public bool IsAlive
        {
            get
            {
                return _isAlive;
            }
            set
            {
                _isAlive = value;
                OnPropertyChanged("IsAlive");
            }
        }

        public int Percent
        {
            get
            {
                return _percent;
            }
            set
            {
                _percent = value;
                OnPropertyChanged("Percent");
            }
        }

        public short UserCount
        {
            get
            {
                return _userCount;
            }
            set
            {
                _userCount = value;
                UsersInfo = _userCount + "/" + MaxUserCount;
                OnPropertyChanged("UserCount");
                OnPropertyChanged("UsersInfo");
            }
        }

        public short AccountCount { get; set; }

        public short PCbangCount { get; set; }

        public short MaxUserCount { get; set; }

        public string UsersInfo { get; set; }

        public bool IsHidden { get; set; }
        #endregion

        public GameServerItem()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
