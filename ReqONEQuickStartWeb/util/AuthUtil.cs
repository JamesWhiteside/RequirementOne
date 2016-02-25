using System;
using System.Linq;
using System.Web;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Security;
using ReqOneApiReference.ReqOneApi;




namespace ReqOneUI
{
    public static class AuthUtil
    {
        #region private methods
        private static DateTime ReqOneOAuthTokenExpiry
        {
            set { HttpContext.Current.Session["__utcTokenExpiry"] = value; }
            get { return HttpContext.Current.Session["__utcTokenExpiry"] == null ? DateTime.MinValue : (DateTime)HttpContext.Current.Session["__utcTokenExpiry"]; }
        }

        private static void SetAuthTokenExpiry(int secondsUntilExpiry)
        {
            ReqOneOAuthTokenExpiry = DateTime.UtcNow.AddSeconds(secondsUntilExpiry);
        }
        #endregion private methods

        #region public methods
        /// <summary>
        /// Set the value of this member after a call to reqManApi.GenerateAuthToken or reqManApi.RefreshAuthToken
        /// The value is being kept in the Session state
        /// </summary>
        public static AccessAndRefreshTokenInfo ReqOneOAuthTokenInfo
        {
            set
            {
                HttpContext.Current.Session["__tokenInfo"] = value;
                SetAuthTokenExpiry(value.SecondsUntilExpiry);
            }
            private get { return HttpContext.Current.Session["__tokenInfo"] as AccessAndRefreshTokenInfo; }
        }

        /// <summary>
        /// Use this property any time you need the ReqOne authToken for the current session. The property will automatically handle
        /// expiration.
        /// </summary>
        public static string AuthToken
        {
            get
            {
                int maxMinutesUntilExpiry = 5;
                DateTime tokenExpiry = ReqOneOAuthTokenExpiry;

                if (tokenExpiry == DateTime.MinValue)
                    AspUtil.RedirectToError("User authentication error");

                try
                {
                    if (DateTime.UtcNow.AddMinutes(maxMinutesUntilExpiry) > tokenExpiry)
                    {
                        // No need for locking. By default ASP .Net implements locking on all requests consuming the Session state
                        ReqOneApiClient apiClient = new ReqOneApiClient();
                        AccessTokenInfo authTokenInfo = apiClient.RefreshAccessToken(ReqOneOAuthTokenInfo.RefreshToken);

                        ReqOneOAuthTokenInfo.AccessToken = authTokenInfo.AccessToken;
                        SetAuthTokenExpiry(authTokenInfo.SecondsUntilExpiry);
                    }
                }
                catch
                {
                    // catch logic not needed for the purpose of this sample; should be implemented if this code gets into production
                    throw;
                }

                return ReqOneOAuthTokenInfo.AccessToken;
            }
        }

        public static string GetReqOneOAuthRedirectUri()
        {
            string redirectUri = AspUtil.GetRoot() + "/res/R1OAuthRedirect.aspx";

            return redirectUri;
        }
        #endregion public methods
    }
}