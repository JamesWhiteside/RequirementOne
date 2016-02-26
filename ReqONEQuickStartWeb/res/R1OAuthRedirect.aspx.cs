using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Configuration;
using System.Web.UI.WebControls;
//using ReqOneUI.ReqOneAPI;
using System.Web.Security;
using ReqOneApiReference.ReqOneApi;


namespace ReqOneUI
{
    public partial class OAuthRedirect : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Request["error"]) == false)
                AspUtil.RedirectToError(Request["Error"]);

            string authCode = Request["code"];
            User reqOneUser = null;
            ReqOneApiClient reqOneApi = new ReqOneApiClient();

            // Request R1 auth token
            AccessAndRefreshTokenInfo tokenInfo = reqOneApi.GenerateAccessToken(ConfigurationManager.AppSettings["ReqOneAppKey"],
                AuthUtil.GetReqOneOAuthRedirectUri(), ConfigurationManager.AppSettings["ReqOneAppSecret"],
                authCode);

            // Pass the token to the AuthUtil
            AuthUtil.ReqOneOAuthTokenInfo = tokenInfo;

            // Get the logged in user and set the authentication cookie for this session
            reqOneUser = reqOneApi.UserGetPrimaryProfile(AuthUtil.AuthToken);
            SessionContext.User = reqOneUser;
            FormsAuthentication.SetAuthCookie(reqOneUser.UserID.ToString(), false);

            Response.Redirect("~/R1EditData.aspx");
        }
    }
}