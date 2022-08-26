using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MSGEdit {
    public enum PataponMessageFormat {
        MSG,
        TXT
    }

    public class MSGtoTXT {
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
        public bool isLoaded {
            get {
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


        public void SaveMSG(string filename) {
            if (!file_loaded) {
                error = "File not loaded!";
                return;
            }
            using (FileStream fstream = File.OpenWrite(filename)) {
                try {
                    UnicodeEncoding encoder = new UnicodeEncoding();

                    byte[] bytes = BitConverter.GetBytes(msg_count);
                    fstream.Write(bytes, 0, bytes.Length);

                    bytes = BitConverter.GetBytes(magic);
                    fstream.Write(bytes, 0, bytes.Length);

                    // Technically, the next section does not need to use the varibale "offsets"

                    // Recalculate offsets and write them to file
                    offsets = new List<int>(msg_count);
                    offsets.Add(8 + (4 * msg_count) + 4); // this form preserves meaning and is equivalent to
                    // offsets.Add((msg_count + 3) * 4);
                    bytes = BitConverter.GetBytes(offsets[0]);
                    fstream.Write(bytes, 0, bytes.Length);
                    for (int i = 1; i < msg_count; ++i) {
                        offsets.Add(offsets[i - 1] + 2 * messages[i - 1].Length); // 2 == sizeof(wchar)
                        bytes = BitConverter.GetBytes(offsets[i]);
                        fstream.Write(bytes, 0, bytes.Length);
                    }

                    // Section separator
                    bytes = BitConverter.GetBytes(0); // here bytes.Length == 4
                    fstream.Write(bytes, 0, bytes.Length);

                    // Write messages
                    for (int i = 0; i < msg_count; ++i) {
                        bytes = encoder.GetBytes(messages[i]);
                        fstream.Write(bytes, 0, bytes.Length);
                    }

                    error = "";
                }
                catch (Exception except) {
                    error = except.Message;
                }
            }
        }
        public void SaveTXT(string filename) {
            if (!file_loaded) {
                error = "File not loaded!";
                return;
            }
            using (FileStream fstream = File.OpenWrite(filename)) {
                try {
                    // Write BOM
                    fstream.WriteByte(0xFF);
                    fstream.WriteByte(0xFE);

                    int t = msg_count - 1; // last index
                    int dig_count = 0;
                    while (t > 0) {
                        t /= 10;
                        ++dig_count;
                    }

                    UnicodeEncoding encoder = new UnicodeEncoding();

                    // Write header
                    byte[] bytes = encoder.GetBytes("SETTINGS:" + magic.ToString() + "," + msg_count.ToString());
                    fstream.Write(bytes, 0, bytes.Length);
                    bytes = encoder.GetBytes("\r\n");
                    fstream.Write(bytes, 0, bytes.Length);

                    // Write messages
                    for (int i = 0; i < msg_count; ++i) {
                        string num = i.ToString();
                        num = new string('0', dig_count - num.Length) + num;

                        num += ':';
                        bytes = encoder.GetBytes(num);
                        fstream.Write(bytes, 0, bytes.Length);

                        bytes = encoder.GetBytes(messages[i]);
                        fstream.Write(bytes, 0, bytes.Length);

                        bytes = encoder.GetBytes("\r\n");
                        fstream.Write(bytes, 0, bytes.Length);
                    }

                    error = "";
                }
                catch (Exception except) {
                    error = except.Message;
                }
            }
        }

        public void LoadMSG(string filename) {
            try {
                byte[] file_contents = File.ReadAllBytes(filename);
                int filesize = file_contents.Length;

                UnicodeEncoding encoder = new UnicodeEncoding();

                msg_count = BitConverter.ToInt32(file_contents, 0);
                magic = BitConverter.ToInt32(file_contents, 4);

                // Technically, the next section does NOT need to use the varibale "offsets"
                int offset;
                offsets = new List<int>(msg_count);
                messages = new List<string>(msg_count);

                for (int i = 0; i < msg_count; ++i) {
                    offset = BitConverter.ToInt32(file_contents, 8 + 4 * i); // same as (i + 2) * 4
                    offsets.Add(offset);
                }

                int length;
                for (int i = 0; i < msg_count - 1; ++i) {
                    length = offsets[i + 1] - offsets[i];
                    messages.Add(encoder.GetString(file_contents, offsets[i], length));
                }
                // Last iteration
                length = filesize - offsets[msg_count - 1];
                messages.Add(encoder.GetString(file_contents, offsets[msg_count - 1], length));

                file_loaded = true;
                error = "";
            }
            catch (Exception except) {
                file_loaded = false;
                error = except.Message;
            }
        }
        public void LoadTXT(string filename) {
            try {
                string[] lines = File.ReadAllLines(filename);
                string txt_header = "SETTINGS";

                string[] words = lines[0].Split(':');
                if (words[0] != txt_header) {
                    file_loaded = false;
                    error = "Wrong file structure";
                    return;
                }

                string[] info = words[1].Split(',');

                magic = Convert.ToInt32(info[0]);
                msg_count = Convert.ToInt32(info[1]);

                int temp = msg_count; // We will use this variable to compute msg_offset
                int N = 0;
                while (temp > 0) {
                    N++;
                    temp /= 10;
                }
                // This is the number of symbols we skip in every line
                int msg_offset = N + 1;

                messages = new List<string>(msg_count);

                for (int i = 0; i < msg_count; ++i) {
                    messages.Add(lines[i + 1].Substring(msg_offset));
                }

                file_loaded = true;
                error = "";
            }
            catch (Exception except) {
                error = except.Message;
                file_loaded = false;
            }
        }
        public PataponMessageFormat LoadAny(string filename) {
            // We first try LoadTXT because it has a SETTINGS check
            LoadTXT(filename);
            if (file_loaded) {
                return PataponMessageFormat.TXT;
            }
            LoadMSG(filename);
            if (!file_loaded) {
                error = $"{filename} is neither TXT file nor MSG file"; // maybe replace the error with more accurate one?
            }
            return PataponMessageFormat.MSG;
        }
        public void MakeNewFile() {
            file_loaded = true;
            messages = new List<string>(10); // Number does not matter
            msg_count = 0;
            magic = 0;
            error = "";
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
        public MSGtoTXT(string filename) {
            LoadAny(filename);
        }
        public MSGtoTXT(int magic) {
            file_loaded = true;
            msg_count = 0;
            messages = new List<string>(10); // Number does not matter
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

                return messages[index].Trim('\0'); // Please note that messages are stored with a trailing zero byte
            }
            set {
                if (index < 0) {
                    error = "Index out of range!";
                    return;
                }
                // If and only if index >= msg_count, the following loop is executed
                for (int i = 0; i < index - msg_count + 1; ++i) {
                    messages.Add("\0");
                }
                // If the loop body has been run at least once, the number of messages needs to be corrected
                if (msg_count <= index) {
                    msg_count += index - msg_count + 1;
                }
                messages[index] = value + "\0"; // Please note that messages are stored with a trailing zero byte
                error = "";
            }
        }

        public string[] Content() {
            if (!file_loaded) {
                error = "File not loaded!";
                return null;
            }
            return messages.ToArray(); // this results in creation of a new list
            // Please note that this array will contain messages with trailing zero-bytes!
        }

        public void Erase(int start_index, int count) {
            if (!file_loaded) {
                error = "File not loaded!";
                return;
            }

            try {
                messages.RemoveRange(start_index, count);
                msg_count -= count;
                error = "";
            }
            catch (Exception except) {
                error = except.Message;
            }
        }
        public void Insert(int index, string message) {
            if (!file_loaded) {
                error = "File not loaded!";
                return;
            }

            try {
                messages.Insert(index, message + "\0"); // Please note that messages are stored with a trailing zero byte
                ++msg_count;
                error = "";
            }
            catch (Exception except) {
                error = except.Message;
            }
        }

        //This method is quite raw and might not produce random enough results.
        //I've heard about Random's disadvantages, but for now we'll just have to go with it, I guess.
        //For some reason, I don't select a seed based on local PC time, but that or something similar may soon be added.
        public void Randomize(int seed) {
            //RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

            Random rand = new Random(seed);
            int n = msg_count;
            while (n > 1) {
                //byte[] box = new byte[1];
                //do {
                //    rand.GetBytes(box);
                //}
                //while (box[0] >= n * (Byte.MaxValue / n));
                //int k = (box[0] % n);

                --n;
                int k = rand.Next(n + 1);
                string temp = messages[k];
                messages[k] = messages[n];
                messages[n] = temp;
            }
            error = "";
        }


        private int try_match_message(string pattern, RegexOptions options, int index) {
            // This is a private method so there will be no special checks made
            string message = messages[index];
            try {
                var match = Regex.Match(message, pattern, options);
                if (!match.Success) {
                    return -1;
                }
                return match.Index;
            }
            catch (Exception except) {
                // Regex exceptions do not get special treatment.
                error = except.Message;
                return -1;
            }
        }

        // This library version does not check if try_match_message throws
        public Tuple<int, int> FindRegex(string pattern, int start, bool forward, bool cycle_search, RegexOptions options = RegexOptions.None) {
            if (!file_loaded) {
                error = "File not loaded!";
                return null;
            }
            if (start >= msg_count || start < 0) {
                error = "Start index out of range!";
                return null;
            }

            // We reset the error
            error = ""; 

            // TO DO: remove repetition
            if (forward) {
                for (int i = start; i < msg_count; ++i) {
                    int res = try_match_message(pattern, options, i);
                    if (res != -1) { // successfull match
                        return new Tuple<int, int>(i, res);
                    }
                }
                // If we haven't found anything, we can now cycle our search
                if (cycle_search) {
                    for (int i = 0; i < start; ++i) {
                        int res = try_match_message(pattern, options, i);
                        if (res != -1) { // Successfull match
                            return new Tuple<int, int>(i, res);
                        }
                    }
                }
            }
            else {
                for (int i = start; i >= 0; --i) {
                    int res = try_match_message(pattern, options, i);
                    if (res != -1) { // Successfull match
                        return new Tuple<int, int>(i, res);
                    }
                }
                // if we haven't found anything, we can now cycle our search
                if (cycle_search) {
                    for (int i = msg_count - 1; i > start; --i) {
                        int res = try_match_message(pattern, options, i);
                        if (res != -1) { // successfull match
                            return new Tuple<int, int>(i, res);
                        }
                    }
                }
            }

            // If the search was unsuccessfull, return null
            return null;
        }

        public List<Tuple<int, int>> FindRegexAll(string pattern, RegexOptions options = RegexOptions.None) {
            // TO DO: make file_loaded check here

            List<Tuple<int, int>> ans = new List<Tuple<int, int>>();
            Tuple<int, int> res;
            int start = 0;
            // TO DO: use a method that does not make checks as the start is guaranteed to be correct
            while (start < msg_count && (res = FindRegex(pattern, start, true, false, options)) != null) {
                start = res.Item1 + 1;
                ans.Add(res);
            }
            return ans;
        }

        public Tuple<int, int> Find(string text, int start, bool case_sensitive, bool whole_words, bool forward, bool cycle_search) {
            // We will use FindRegex so it's superfluous to make any checks here

            // First let's prepare a pattern based on text.
            // Our text first needs to be "surrounded be \Q and \E" so that special symbols would not be misinterpreted.
            string pattern = Regex.Escape(text);

            RegexOptions options = RegexOptions.None;
            if (!case_sensitive) {
                options |= RegexOptions.IgnoreCase;
            }
            // If we search for whole words, the pattern must be surrounded with \b from both sides.
            if (whole_words) {
                pattern = $@"\b{pattern}\b";
            }

            // Now that we are done, let's execute the FindRegex method
            Tuple<int, int> ans = FindRegex(pattern, start, forward, cycle_search, options);

            // maybe some checks here
            return ans;
        }

        public List<Tuple<int, int>> FindAll(string text, bool case_sensitive, bool whole_words) {
            // We will use FindRegexAll so it's superfluous to make any checks here

            // First let's prepare a pattern based on text.
            // Our text first needs to be "surrounded be \Q and \E" so that special symbols would not be misinterpreted.
            string pattern = Regex.Escape(text);

            RegexOptions options = RegexOptions.None;
            if (!case_sensitive) {
                options |= RegexOptions.IgnoreCase;
            }
            // If we search for whole words, the pattern must be surrounded with \b from both sides.
            if (whole_words) {
                pattern = $@"\b{pattern}\b";
            }

            // Now that we are done, let's execute the FindRegexAll method
            List<Tuple<int, int>> ans = FindRegexAll(pattern, options);

            // maybe some checks here
            return ans;
        }
    }
}
