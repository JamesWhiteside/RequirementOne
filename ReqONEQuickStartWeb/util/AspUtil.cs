using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Configuration;

namespace ReqOneUI
{
    public class AspUtil
    {
        #region public methods
        public static string GetRoot()
        {
            HttpRequest req = HttpContext.Current.Request;
            string result = String.Concat(req.Url.Scheme, "://", req.Url.Authority,
                req.ApplicationPath.TrimEnd('/'));

            return result;
        }

        public static void RedirectToError(string message)
        {
            HttpContext.Current.Response.Redirect("~/res/Error.aspx?error=" + message);
        }

        #endregion public methods
    }
}