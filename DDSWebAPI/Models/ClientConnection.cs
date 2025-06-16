using System;
using System.ComponentModel;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 用戶端連接資訊類別
    /// </summary>
    public class ClientConnection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;        private string _id;
        private string _ipAddress;
        private string _connectionId;
        private DateTime _connectTime;
        private DateTime _lastActivityTime;
        private string _requestType;

        /// <summary>
        /// 用戶端唯一識別碼
        /// </summary>
        public string Id 
        { 
            get { return _id; }
            set 
            { 
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        /// <summary>
        /// 用戶端 IP 位址
        /// </summary>
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }        }

        /// <summary>
        /// 連線識別碼 (用於持久連線追蹤)
        /// </summary>
        public string ConnectionId
        {
            get { return _connectionId; }
            set
            {
                _connectionId = value;
                OnPropertyChanged(nameof(ConnectionId));
            }
        }

        /// <summary>
        /// 連接時間
        /// </summary>
        public DateTime ConnectTime
        {
            get { return _connectTime; }
            set
            {
                _connectTime = value;
                OnPropertyChanged(nameof(ConnectTime));
            }
        }

        /// <summary>
        /// 最後活動時間
        /// </summary>
        public DateTime LastActivityTime
        {
            get { return _lastActivityTime; }
            set
            {
                _lastActivityTime = value;
                OnPropertyChanged(nameof(LastActivityTime));
            }
        }

        /// <summary>
        /// 請求類型
        /// </summary>
        public string RequestType
        {
            get { return _requestType; }
            set
            {
                _requestType = value;
                OnPropertyChanged(nameof(RequestType));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
