using System.ComponentModel;

namespace SCME.UI.PagesUser
{
    public class UserTestPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Types.Profiles.Profile m_Profile;
        public Types.Profiles.Profile Profile
        {
            get { return m_Profile; }
            set
            {
                m_Profile = value;
                NotifyPropertyChanged("Profile");
            }
        }

        private string m_PsdJob;
        public string PsdJob
        {
            get { return m_PsdJob; }
            set
            {
                m_PsdJob = value;
                NotifyPropertyChanged("PsdJob");
            }
        }

        private string m_PsdSerialNumber;
        public string PsdSerialNumber
        {
            get { return m_PsdSerialNumber; }
            set
            {
                m_PsdSerialNumber = value;
                NotifyPropertyChanged("PsdSerialNumber");
            }
        }

        private string m_PseJob;
        public string PseJob
        {
            get { return m_PseJob; }
            set
            {
                m_PseJob = value;
                NotifyPropertyChanged("PseJob");
            }
        }

        private string m_PseNumber;
        public string PseNumber
        {
            get { return m_PseNumber; }
            set
            {
                m_PseNumber = value;
                NotifyPropertyChanged("PseNumber");
            }
        }

        private bool m_SpecialMeasureMode;
        public bool SpecialMeasureMode
        {
            get { return m_SpecialMeasureMode; }
            set
            {
                m_SpecialMeasureMode = value;
                NotifyPropertyChanged("SpecialMeasureMode");
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
