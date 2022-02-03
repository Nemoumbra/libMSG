using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MSGEdit
{
    enum PataponMessageFormat {
        MSG,
        TXT
    }
    class MSGtoTXT
    {
        private int magic;
        private bool file_loaded = false;
        private int msg_count;
        private string error;
        //private bool unpushed_changes;
        public int Magic {
            get {
                if (!file_loaded) {
                    error = "File not loaded!";
                    return 0;
                }
                return magic;
            }
            set {
                magic = value;
            }
        }
        public bool isLoaded
        {
            get
            {
                return file_loaded;
            }
        }
        public int Count {
            get {
                if (!file_loaded) {
                    error = "File not loaded!";
                    return 0;
                }
                return msg_count;
            }
        }
        private List<string> messages;
        private List<int> offsets;

        public void SaveMSG(string filename)
        {
            if (!file_loaded) {
                error = "File not loaded!";
                return;
            }
            try {
                FileStream fstream = File.OpenWrite(filename);
                UnicodeEncoding encoder = new UnicodeEncoding();
                byte[] bytes = BitConverter.GetBytes(msg_count);
                fstream.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(magic);
                fstream.Write(bytes, 0, bytes.Length);
                
                //recalculate offsets and write them to file
                offsets = new List<int>(msg_count);
                offsets.Add(8 + 4 * msg_count + 4);

                bytes = BitConverter.GetBytes(offsets[0]);
                fstream.Write(bytes, 0, bytes.Length);
                for (int i = 1; i < msg_count; i++) {
                    offsets.Add(offsets[i - 1] + 2 * messages[i - 1].Length);
                    bytes = BitConverter.GetBytes(offsets[i]);
                    fstream.Write(bytes, 0, bytes.Length);
                }
                
                bytes = BitConverter.GetBytes(0);
                fstream.Write(bytes, 0, bytes.Length);
                //fstream.Write(bytes, 0, bytes.Length);

                for (int i = 0; i < msg_count; i++) {
                    //if (i == msg_count - 1) {
                    //    Console.WriteLine("");
                    //}
                    bytes = encoder.GetBytes(messages[i]);
                    fstream.Write(bytes, 0, bytes.Length);
                }
                fstream.Close();
            }
            catch (Exception except) {
                error = except.Message;
                file_loaded = false;
            }
        }
        public void SaveTXT(string filename)
        {
            if (!file_loaded) {
                error = "File not loaded!";
                return;
            }
            try {
                FileStream fstream = File.OpenWrite(filename);
                fstream.WriteByte(0xFF);
                fstream.WriteByte(0xFE);

                int t = msg_count - 1;
                int dig_count = 0;
                while (t > 0) {
                    t /= 10;
                    dig_count++;
                }
                UnicodeEncoding encoder = new UnicodeEncoding();
                byte[] bytes = encoder.GetBytes("SETTINGS:" + magic.ToString() + "," + msg_count.ToString());
                fstream.Write(bytes, 0, bytes.Length);
                bytes = encoder.GetBytes("\r\n");
                fstream.Write(bytes, 0, bytes.Length);

                for (int i = 0; i < msg_count; i++) {
                    string num = i.ToString();
                    while (num.Length < dig_count) {
                        num = "0" + num;
                    }
                    num += ':';
                    bytes = encoder.GetBytes(num);
                    fstream.Write(bytes, 0, bytes.Length);
                    bytes = encoder.GetBytes(messages[i]);
                    fstream.Write(bytes, 0, bytes.Length);

                    bytes = encoder.GetBytes("\r\n");
                    fstream.Write(bytes, 0, bytes.Length);
                }
                fstream.Close();
            }
            catch (Exception exept) {
                error = exept.Message;
            }
        }
        public void LoadMSG(string filename)
        {
            try
            {
                byte[] file_contents = File.ReadAllBytes(filename);
                int filesize = file_contents.Length;
                UnicodeEncoding encoder = new UnicodeEncoding();
                msg_count = BitConverter.ToInt32(file_contents, 0);
                magic = BitConverter.ToInt32(file_contents, 4);
                int offset;
                offsets = new List<int>(msg_count);
                messages = new List<string>(msg_count);
                for (int i = 0; i < msg_count; i++)
                {
                    offset = BitConverter.ToInt32(file_contents, 8 + 4 * i);
                    offsets.Add(offset);
                }
                int length;
                for (int i = 0; i < msg_count; i++)
                {
                    if (i == msg_count - 1)
                    {
                        length = filesize - offsets[msg_count - 1];
                    }
                    else
                    {
                        length = offsets[i + 1] - offsets[i];
                    }
                    //messages.Add(BitConverter.ToString(file_contents, offsets[i], length));
                    messages.Add(encoder.GetString(file_contents, offsets[i], length));
                }
                file_loaded = true;
            }
            catch (Exception exept)
            {
                file_loaded = false;
                error = exept.Message;
            }
        }
        public void LoadTXT(string filename)
        {
            try
            {
                string[] lines = File.ReadAllLines(filename);
                string[] words = lines[0].Split(':')[1].Split(',');
                magic = Convert.ToInt32(words[0]);
                int temp = Convert.ToInt32(words[1]);
                msg_count = temp;
                int N = 0;
                while (temp > 0) {
                    N++;
                    temp /= 10;
                }
                //int msg_offset = (N + 1) * 2;
                int msg_offset = N + 1;
                messages = new List<string>(msg_count);
                for (int i = 0; i < msg_count; i++) {
                    messages.Add(lines[i + 1].Substring(msg_offset));
                }
                file_loaded = true;
            }
            catch (Exception exept) {
                error = exept.Message;
                file_loaded = false;
            }
        }

        public MSGtoTXT() {

        }
        public MSGtoTXT(string filename, PataponMessageFormat format) {
            if (format == PataponMessageFormat.MSG) {
                LoadMSG(filename);
            }
            else {
                LoadTXT(filename);
            }
        }
        public MSGtoTXT(int magic) {
            file_loaded = true;
            msg_count = 0;
            messages = new List<string>();
            this.magic = magic;
        }

        public string LastError {
            get {
                return error;
            }
        }
        public string this[int index] {
            get {
                if (!file_loaded) {
                    error = "File was not loaded!";
                    return "";
                }
                if (index > msg_count - 1 || index < 0) {
                    error = "Index out of range!";
                    return "";
                }
                return messages[index].Trim('\0');
            }
            set {
                if (index < 0) {
                    error = "Index out of range!";
                    return;
                }
                for (int i = 0; i < index - msg_count + 1; i++) {
                    messages.Add("\0");
                }
                msg_count += index - msg_count + 1;
                messages[index] = value + "\0";
            }
        }
        //public string At(int index) {
        //
        //}
        public void Erase(int start_index, int count) {
            if (!file_loaded) {
                error = "File not loaded!";
                return;
            }
            try {
                messages.RemoveRange(start_index, count);
                msg_count -= count;
            }
            catch (Exception exept) {
                error = exept.Message;
                return;
            }
        }
        public void Insert(int index, string message) {
            if (!file_loaded)
            {
                error = "File not loaded!";
                return;
            }
            try
            {
                messages.Insert(index, message);
                msg_count++;
            }
            catch (Exception exept)
            {
                error = exept.Message;
                return;
            }
        }
    }
}
