using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using ReqOneApiReference.ReqOneApi;
using ReqOneUI;

namespace RequirementONEQuickStartWeb
{
    public partial class R1EditData : System.Web.UI.Page
    {
        readonly int MAX_RESULTS = int.Parse(ConfigurationManager.AppSettings["MaxSearchResults"]);
        const string ALL = "all";
        Specification currData = new Specification(); 
        ReqOneApiClient _api = new ReqOneApiClient();

        protected void Page_Load(object sender, EventArgs e)
        {
            CheckLogin();

            if (!IsPostBack)
                LoadData(); 
        }

        void CheckLogin()
        {
            if (SessionContext.User == null)
            {
                Response.Redirect(ConfigurationManager.AppSettings["ReqOneLoginUrl"] +
                    "?client_id=" + ConfigurationManager.AppSettings["ReqOneAppKey"] +
                    "&redirect_uri=" + HttpUtility.UrlEncode(
                        AuthUtil.GetReqOneOAuthRedirectUri()));
            }
        }

        void LoadData()
        {
            LoadProjects();
            LoadSpecifications();
            LoadReviews();
            tbText.Focus();
        }

        private void LoadProjects()
        {
            var projects = _api.ProjectGetAll(AuthUtil.AuthToken);
            ddlProjects.Items.Clear();
            ddlProjects.Items.AddRange(projects.Select(p => new ListItem(
                p.Name, p.ProjectID.ToString())).ToArray());

            var reqOneProject = ddlProjects.Items.FindByValue(
                "e2967e32-2dad-4573-8964-28535adcd547");
            if (reqOneProject != null)
                reqOneProject.Selected = true;
        }

        protected void ddlProjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSpecifications();
            LoadReviews();
        }

        private void LoadReviews()
        {
            var reviews = _api.IssueGroupGetAll(new Guid(ddlProjects.SelectedValue),
                AuthUtil.AuthToken);
            ddlReviews.Items.Clear();
            if (reviews.Any())
                ddlReviews.Items.Add(new ListItem("Include all", ALL));
            ddlReviews.Items.AddRange(reviews.Select(r => new ListItem(
                r.Name, r.ID.ToString())).ToArray());
        }

        private void LoadSpecifications()
        {
            var specs = _api.SpecificationsGetAll(new Guid(ddlProjects.SelectedValue),
                AuthUtil.AuthToken);
            ddlSpecifications.Items.Clear();
            if (specs.Any())
                ddlSpecifications.Items.Add(new ListItem("Include all", ALL));
            ddlSpecifications.Items.AddRange(specs.Select(s => new ListItem(
                s.Name, s.SpecificationID.ToString())).ToArray());

            var functionalSpecsItem = ddlSpecifications.Items.FindByValue(
                "5a958c22-5f2b-40f6-9814-b0e7503b58d0");
            if (functionalSpecsItem != null)
                functionalSpecsItem.Selected = true;
        }

        class Statistics
        {
            public int TotalRecords { get; set; }
            public TimeSpan ApiCallDuration { get; set; }
            public TimeSpan TotalProcessingDuration { get; set; }
        }

        protected void btnSearchRequirements_Click(object sender, EventArgs e)
        {
            if (tbText.Text.Trim() == "" || ddlSpecifications.SelectedItem == null ||
                ddlProjects.SelectedItem == null)
            {
                return;
            }

            Statistics stats;
            var reqs = SearchRequirements(tbText.Text, out stats);

            var output = reqs.Select(r => new
            {
                CustomId = r.CustomIdentifier,
                Name = r.Name,
                Link = string.Format("https://ui.requirementone.com/specification/overview/specification/requirement/?projectid={0}&specificationid={1}&requirementid={2}",
                    r.ProjectID,
                    r.SpecificationID,
                    r.RequirementID),
                Details = PrepareForHtml(r.Details)
            });

            lvSearchResults.DataSource = output;
            lvSearchResults.DataBind();

            ShowStatistics(stats);
        }

        protected void btnSearchIssues_Click(object sender, EventArgs e)
        {
            if (tbText.Text.Trim() == "" || ddlReviews.SelectedItem == null ||
                ddlProjects.SelectedItem == null)
            {
                return;
            }

            Statistics stats;
            var issues = SearchIssues(tbText.Text, out stats);

            var output = issues.Select(i => new
            {
                CustomId = i.CustomID,
                Name = i.Name,
                Link = string.Format("https://ui.requirementone.com/issues/overview/review/issue/?projectid={0}&reviewid={1}&issueid={2}",
                    i.ProjectID,
                    i.GroupID,
                    i.ID),
                Details = PrepareForHtml(i.Details)
            });

            lvSearchResults.DataSource = output;
            lvSearchResults.DataBind();

            ShowStatistics(stats);
        }

        private void ShowStatistics(Statistics stats)
        {
            lblStatistics.Text = string.Format("<b>Statistics</b><br/>Total records: {0}<br/> API call duration: {1}<br/>", stats.TotalRecords, stats.ApiCallDuration);
        }

        string PrepareForHtml(string text)
        {
            if (text == null)
                return null;
            else
                return text.Replace("\r\n", "\n").Replace("\n", "<br/>");
        }


        List<RequirementDetails> SearchRequirements(string text, out Statistics stats)
        {
            stats = new Statistics();
            Stopwatch watch = new Stopwatch();
            watch.Start();

            List<RequirementDetails> reqs = new List<RequirementDetails>();

            if (ddlSpecifications.SelectedItem == null)
                goto _exit;            

            if (ddlSpecifications.SelectedValue == ALL)
            {
                for (int i = 0; i < ddlSpecifications.Items.Count; i++)
                {
                    if (ddlSpecifications.Items[i].Value != ALL)
                    {
                        reqs.AddRange(_api.SpecificationsRequirementSearch(new RequirementSearchArguments {
                            Projects = new Guid[] { new Guid(ddlProjects.SelectedValue) },
                            Specifications = new SpecificationSearchArguments[] { new SpecificationSearchArguments {SpecificationID = new Guid(ddlSpecifications.Items[i].Value)} },
                            FreeText = text
                        }   , AuthUtil.AuthToken).Requirements);
                    }
                }
                
            }
            else
                reqs.AddRange(_api.SpecificationsRequirementSearch(new RequirementSearchArguments
                {
                        Projects = new Guid[] { new Guid(ddlProjects.SelectedValue) },
                        Specifications = new SpecificationSearchArguments[] { new SpecificationSearchArguments {SpecificationID = new Guid(ddlSpecifications.SelectedValue)} },
                        FreeText = text
                    }  , AuthUtil.AuthToken).Requirements);

            stats.TotalRecords = reqs.Count();
            stats.ApiCallDuration = watch.Elapsed;
            watch.Restart();

       _exit:
            stats.TotalProcessingDuration += watch.Elapsed;

       return reqs.ToList();
        }

        // ======================================================

        List<Issue> SearchIssues(string text, out Statistics stats)
        {
            stats = new Statistics();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            List<Issue> issues = new List<Issue>();

            if (ddlProjects.SelectedItem == null || ddlReviews.SelectedItem == null)
                goto _exit;

            if (ddlReviews.SelectedValue == ALL)
            {
                for (int i = 0; i < ddlReviews.Items.Count; i++)
                {
                    if (ddlReviews.Items[i].Value != ALL)
                    {
                        issues.AddRange(_api.IssueSearch(new IssueSearchArguments
                        {
                            Projects = new Guid[] { new Guid(ddlProjects.SelectedValue) },
                            Groups = new Guid[] { new Guid(ddlReviews.Items[i].Value) },
                            FreeText = text
                        }, AuthUtil.AuthToken).Issues);
                    }
                }

            }
            else
            {
                issues.AddRange(_api.IssueSearch(new IssueSearchArguments
                {
                    Projects = new Guid[] { new Guid(ddlProjects.SelectedValue) },
                    Groups = new Guid[] { new Guid(ddlReviews.SelectedValue) },
                    FreeText = text
                }, AuthUtil.AuthToken).Issues);
            }
            
            stats.ApiCallDuration = watch.Elapsed;
            stats.TotalRecords = issues.Count;
            watch.Restart();
         
        _exit:
            stats.TotalProcessingDuration += watch.Elapsed;

            return issues;
        }

        
        protected void LoadData_Click(object sender, EventArgs e)
        {
            Guid specificationID = new Guid(ddlSpecifications.SelectedValue);

            var testData = _api.SpecificationsRequirementGetAll(specificationID, AuthUtil.AuthToken);
            if (testData == null)
            {
                return;
            }

            Response.ContentType = "application/msword";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"test" + ".doc\"");

            foreach(var test in testData){
                Response.Write(test.CustomIdentifier);
                Response.Write(test.Name);
                /*Link = string.Format("https://ui.requirementone.com/specification/overview/specification/requirement/?projectid={0}&specificationid={1}&requirementid={2}",
                    test.ProjectID,
                    test.SpecificationID,
                    test.RequirementID);*/
                 Response.Write(PrepareForHtml(test.Details));
            }

            //Response.Flush();
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }

        
        protected void UpLoadData_Click(object sender, EventArgs e) 
        {
            /*
            currData.Details = editText.Text;
            _api.SpecificationsSave(currData, AuthUtil.AuthToken);
            */
             
        }
          
    }
}