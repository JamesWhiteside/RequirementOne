using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

//using ReqOneUI.ReqOneAPI;
using ReqOneApiReference.ReqOneApi;


namespace ReqOneUI
{
    public static class SessionContext
    {
        public static User User
        {
            get { return HttpContext.Current.Session["__CrtUser"] as User; }
            set { HttpContext.Current.Session["__CrtUser"] = value; }
        }
    }
}