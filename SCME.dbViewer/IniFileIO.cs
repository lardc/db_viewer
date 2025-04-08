using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows;

namespace SCME.dbViewer
{
    public class IniFileIO
    {
        string FPath = null;

        [DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [System.Runtime.InteropServices.DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retValue, int size, string filePath);

        private string ApplicationPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private string IniFileName()
        {
            return String.Concat(Application.ResourceAssembly.GetName().Name, ".ini");
        }

        private string IniFileFullAddress()
        {
            string path = this.ApplicationPath();

            return String.Concat(path, IniFileName());
        }

        public IniFileIO()
        {
            //чтобы можно было переопределить настройки программы надо скопировать с сервера файл SCME.dbViewer.ini в корень диска C и сделать в этой локальной копии все желаемые значения параметров
            string iniFileFullAddress = String.Concat(System.Environment.GetEnvironmentVariable("USERPROFILE"), "\\", IniFileName());

            if (File.Exists(iniFileFullAddress))
                this.FPath = iniFileFullAddress;
            else
            {
                iniFileFullAddress = this.IniFileFullAddress();

                if (File.Exists(iniFileFullAddress))
                    this.FPath = iniFileFullAddress;
            }
        }

        public string Read(string section, string key)
        {
            if (this.FPath == null)
            {
                return null;
            }
            else
            {
                StringBuilder retValue = new StringBuilder(255);
                GetPrivateProfileString(section, key, "", retValue, 255, this.FPath);

                return retValue.ToString();
            }
        }

        public bool KeyExists(string section, string key)
        {
            if (this.FPath == null)
            {
                return false;
            }
            else
                return (this.Read(key, section).Length > 0);
        }

        public void Write(string section, string key, string value)
        {
            if (this.FPath != null)
                WritePrivateProfileString(section, key, value, this.FPath);
        }
    }
}
