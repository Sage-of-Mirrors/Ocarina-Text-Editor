using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;
using OcarinaTextEditor.Enums;
using System.IO;

namespace OcarinaTextEditor
{
    public class ViewModel : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged overhead
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region public List<Message> MessageList
        public ObservableCollection<Message> MessageList
        {
            get { return m_messageList; }
            set
            {
                if (value != m_messageList)
                {
                    m_messageList = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private ObservableCollection<Message> m_messageList;
        #endregion

        #region public Message SelectedMessage
        public Message SelectedMessage
        {
            get { return m_selectedMessage; }
            set
            {
                if (value != m_selectedMessage)
                {
                    m_selectedMessage = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Message m_selectedMessage;
        #endregion

        #region public string WindowTitle
        public string WindowTitle
        {
            get { return m_windowTitle; }
            set
            {
                if (value != m_windowTitle)
                {
                    m_windowTitle = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string m_windowTitle = "Ocarina of Time Text Editor";
        #endregion

        #region public CollectionViewSource ViewSource
        public CollectionViewSource ViewSource
        {
            get { return m_viewSource; }
            set
            {
                if (value != m_viewSource)
                {
                    m_viewSource = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private CollectionViewSource m_viewSource;
        #endregion

        #region public string SearchFilter
        public string SearchFilter
        {
            get { return m_searchFilter; }

            set
            {
                m_searchFilter = value;

                if (!string.IsNullOrEmpty(SearchFilter))
                    AddFilter();

                ViewSource.View.Refresh();

                NotifyPropertyChanged("SearchFilter");
            }
        }
        private string m_searchFilter;
        #endregion

        private Dictionary<ControlCode, string> m_controlCodes;

        public int TextboxPosition;

        private MemoryStream m_inputFile;

        #region Command Callbacks
        public ICommand OnRequestOpenFile
        {
            get { return new RelayCommand(x => Open(), x => true); }
        }
        public ICommand OnRequestSaveFileROM
        {
            get { return new RelayCommand(x => SaveToRom(), x => MessageList != null); }
        }
        public ICommand OnRequestSaveFileFiles
        {
            get { return new RelayCommand(x => SaveToFiles(), x => MessageList != null); }
        }
        public ICommand OnRequestSaveFilePatch
        {
            get { return new RelayCommand(x => SaveToPatch(), x => MessageList != null); }
        }
        public ICommand OnRequestAddMessage
        {
            get { return new RelayCommand(x => AddMessage(), x => MessageList != null); }
        }
        public ICommand OnRequestRemoveMessage
        {
            get { return new RelayCommand(x => RemoveMessage(), x => MessageList != null); }
        }
        public ICommand OnRequestAddControl
        {
            get { return new RelayCommand(x => InsertControlCode((string)x), x => SelectedMessage != null); }
        }
        #endregion

        public ViewModel()
        {
            ViewSource = new CollectionViewSource();

            m_controlCodes = PopulateCodeDictionary();
        }

        #region Input/Output
        private void Open()
        {
            OpenFileDialog openFile = new OpenFileDialog();

            openFile.Filter = "N64 ROMs (*.n64, *.z64)|*.n64;*.z64|All files|*";

            if (openFile.ShowDialog() == true)
            {
                Importer file = new Importer(openFile.FileName, m_controlCodes);
                MessageList = file.GetMessageList();
                ViewSource.Source = MessageList;
                SelectedMessage = MessageList[0];

                WindowTitle = string.Format("{0} - Ocarina of Time Text Editor", openFile.FileName);
            }
        }

        private void SaveToRom()
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "N64 ROMs (*.n64, *.z64)|*.n64;*.z64|All files|*";

            if (saveFile.ShowDialog() == true)
            {
                Exporter export = new Exporter(m_messageList, saveFile.FileName, Enums.ExportType.ROM, m_controlCodes);
            }
        }

        private void SaveToFiles()
        {
            var ofd = new CommonOpenFileDialog();
            ofd.Title = "Choose Directory";
            ofd.IsFolderPicker = true;
            ofd.AddToMostRecentlyUsedList = false;
            ofd.AllowNonFileSystemItems = false;
            ofd.EnsureFileExists = true;
            ofd.EnsurePathExists = true;
            ofd.EnsureReadOnly = false;
            ofd.EnsureValidNames = true;
            ofd.Multiselect = false;
            ofd.ShowPlacesList = true;

            if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Exporter export = new Exporter(m_messageList, ofd.FileName, Enums.ExportType.File, m_controlCodes);
            }
        }

        private void SaveToPatch()
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Playstation Patch Format files (*.ppf)|*.ppf|All files|*";

            if (saveFile.ShowDialog() == true)
            {
                Exporter export = new Exporter(m_messageList, saveFile.FileName, Enums.ExportType.Patch, m_controlCodes);
            }
        }
        #endregion

        #region Adding and Removing Messages
        private void AddMessage()
        {
            Message newMes = new Message();
            newMes.MessageID = GetHighestID();
            MessageList.Insert(MessageList.Count - 1, newMes);
            ViewSource.View.Refresh();
        }

        private void RemoveMessage()
        {
            int selectedIndex = MessageList.IndexOf(SelectedMessage);
            MessageList.Remove(SelectedMessage);

            if (MessageList.Count == 0)
                MessageList.Add(new Message());

            if (selectedIndex == 0)
                SelectedMessage = MessageList[0];
            else
                SelectedMessage = MessageList[selectedIndex - 1];
        }

        private short GetHighestID()
        {
            short highest = short.MinValue;

            foreach (Message mes in MessageList)
            {
                if (highest < mes.MessageID)
                    highest = mes.MessageID;
            }

            return (short)(highest + 1);
        }
        #endregion

        #region Search Filtering
        private void Filter(object sender, FilterEventArgs e)
        {
            short findId;

            // see Notes on Filter Methods:
            var src = e.Item as Message;
            
            if (src == null)
                e.Accepted = false;
            
            //test if textbox message doesn't match our filter
            if (src.TextData != null && !src.TextData.Contains(SearchFilter))
                e.Accepted = false;

            //test if filter matches a textbox id
            if (SearchFilter.StartsWith("0x")
                && short.TryParse(SearchFilter.Substring(2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out findId))
            {
                if (src.MessageID == findId)
                    e.Accepted = true;
            }
        }

        private void AddFilter()
        {
            ViewSource.Filter -= new FilterEventHandler(Filter);
            ViewSource.Filter += new FilterEventHandler(Filter);
        }
        #endregion

        private void InsertControlCode(string code)
        {
            SelectedMessage.TextData = SelectedMessage.TextData.Insert(TextboxPosition,string.Format("<{0}>", code));
        }

        private Dictionary<ControlCode, string> PopulateCodeDictionary()
        {
            Dictionary<ControlCode, string> output = new Dictionary<ControlCode, string>();

            foreach (ControlCode code in Enum.GetValues(typeof(ControlCode)))
            {
                output.Add(code, code.ToString().Replace("_", " "));
            }

            return output;
        }
    }
}
