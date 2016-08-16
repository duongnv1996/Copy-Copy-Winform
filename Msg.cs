using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAD.ClipMon
{
    class Msg
    {
        int _code;
        public int code {
            get { return _code; }
            set { _code = value; }
        }
        String _content;
        public String content {
            get { return _content; }
            set { _content = value; }
        }
        int _id;
        public int id {
            get { return _id; }
            set { _id = value; }
        }
        String _date;
        public String date {
            get { return _date; }
            set { _date = value; }
        }
       



    }
}
