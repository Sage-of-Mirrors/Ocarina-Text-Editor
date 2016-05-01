using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;

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

        #region Command Callbacks
        public ICommand OnRequestOpenFile
        {
            get { return new RelayCommand(x => Open(), x => true); }
        }
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

        private void AddFilter()
        {
            ViewSource.Filter -= new FilterEventHandler(Filter);
            ViewSource.Filter += new FilterEventHandler(Filter);
        }

        public ViewModel()
        {
            ViewSource = new CollectionViewSource();
        }

        private void Open()
        {
            OpenFileDialog openFile = new OpenFileDialog();

            openFile.Filter = "N64 ROMs (*.n64)|*.n64|All files|*";
            
            if (openFile.ShowDialog() == true)
            {
                Importer file = new Importer(openFile.FileName);
                MessageList = file.GetMessageList();
                ViewSource.Source = MessageList;
                SelectedMessage = MessageList[0];

                WindowTitle = string.Format("{0} - Ocarina of Time Text Editor", openFile.FileName);
            }
        }

        private void Filter(object sender, FilterEventArgs e)
        {
            // see Notes on Filter Methods:
            var src = e.Item as Message;

            if (src == null)
                e.Accepted = false;

            else if (SearchFilter.Contains("msgid"))
            {
                string[] parsed = SearchFilter.Split(':');

                if (parsed.Count() >= 2)
                {
                    // Oh boy, parsing
                    try
                    {
                        if (parsed[1] != "" && Convert.ToInt32(src.MessageID) != Convert.ToInt32(parsed[1]))
                            e.Accepted = false;
                    }
                    // Something fucked up. Let's catch the exception
                    catch (OverflowException ex)
                    {
                        SearchFilter = string.Format("msgid:{0}", int.MaxValue);
                    }
                    catch (FormatException ex)
                    {
                        SearchFilter = string.Format("msgid:{0}", parsed[1].Remove(parsed[1].Length - 1));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Exception {0}!"), ex.ToString());
                    }
                }
            }

            else if (src.TextData != null && !src.TextData.Contains(SearchFilter))// here is FirstName a Property in my YourCollectionItem
                e.Accepted = false;
        }
    }
}
