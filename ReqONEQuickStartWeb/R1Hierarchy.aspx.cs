using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using System.Windows.Forms;
using ReqOneApiReference.ReqOneApi;
using ReqOneUI;
using TreeNode = System.Web.UI.WebControls.TreeNode;

namespace RequirementONEQuickStartWeb
{
    public partial class R1Hierarchy : System.Web.UI.Page
    {
        readonly int MAX_RESULTS = int.Parse(ConfigurationManager.AppSettings["MaxSearchResults"]);
        const string ALL = "all";
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
            if (ddlSpecifications.SelectedItem == null || ddlProjects.SelectedItem == null)
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
            if (ddlReviews.SelectedItem == null || ddlProjects.SelectedItem == null)
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

        //SEARCHREQUIREMENTS
        List<RequirementDetails> SearchRequirements(string text, out Statistics stats)
        {
            stats = new Statistics();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            if(treeHierarchy.Nodes.Count > 0) treeHierarchy.Nodes.Clear();
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
            //***************************ADDED IN
            createTreeParentNodes(reqs.ToList());
            //***************************
            stats.TotalRecords = reqs.Count();
            stats.ApiCallDuration = watch.Elapsed;
            watch.Restart();
            
       _exit:
            stats.TotalProcessingDuration += watch.Elapsed;
       
       return reqs.ToList();
        }


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

        //CLEAR
        protected void btnclearResults(object sender, EventArgs e)
        {
            tbText.Text = null;
            lvSearchResults.DataSource = null;
            lvSearchResults.DataBind();
            treeHierarchy.Nodes.Clear();
        }

        /// <summary>
        /// This class is called whenever the selected Index is Changed
        /// It automatically creates a tree table whenever the Index is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void generateResults(object sender, EventArgs e)
        {
            List<RequirementDetails> reqs = new List<RequirementDetails>();

            if (ddlSpecifications.SelectedValue == ALL)
            {
                for (int i = 0; i < ddlSpecifications.Items.Count; i++)
                {
                    if (ddlSpecifications.Items[i].Value != ALL)
                    {
                        reqs.AddRange(_api.SpecificationsRequirementSearch(new RequirementSearchArguments
                        {
                            Projects = new Guid[] { new Guid(ddlProjects.SelectedValue) },
                            Specifications = new SpecificationSearchArguments[] { new SpecificationSearchArguments { SpecificationID = new Guid(ddlSpecifications.Items[i].Value) } },
                        },
                           AuthUtil.AuthToken).Requirements);
                    }
                }
            }
            else
                reqs.AddRange(_api.SpecificationsRequirementSearch(new RequirementSearchArguments
                {
                    Projects = new Guid[] { new Guid(ddlProjects.SelectedValue) },
                    Specifications = new SpecificationSearchArguments[] { new SpecificationSearchArguments { SpecificationID = new Guid(ddlSpecifications.SelectedValue) } },
                }, AuthUtil.AuthToken).Requirements);
            //***************************ADDED IN
            createTreeParentNodes(reqs.ToList());
            //***************************
        }

        /// <summary>
        /// This code should go through the list, every time there's a new
        /// "subsection" it will call bindtree which will add that subsection
        /// Then it will continue on
        /// Depends on sorted list(by ID), all ID's being processed correctly
        /// </summary>
        /// <param name="list"></param>
        protected void createTreeParentNodes(List<RequirementDetails> list) {
            treeHierarchy.Nodes.Clear();
            
            /*Include all code*/
            if (ddlSpecifications.SelectedItem.ToString() == "Include all")
            {
                for (int i = 1; i < ddlSpecifications.Items.Count; i++)
                {

                    CustomTreeNode rootNode = new CustomTreeNode(ddlSpecifications.Items[i].ToString(), ddlSpecifications.Items[i].Value.ToString());
                    CustomTreeNode parentNode = new CustomTreeNode("", "");
                    treeHierarchy.Nodes.Add(rootNode);
                    List<RequirementDetails> miniList = list.Where(k => k.SpecificationName== ddlSpecifications.Items[i].ToString()).ToList();
                    for (int j = 0; j < miniList.Count(); j++)
                    {
                        if (miniList[j].TreeName != parentNode.Text)
                        {
                            parentNode = new CustomTreeNode(miniList[j].TreeName, miniList[j].TreeParentID.ToString());
                            rootNode.ChildNodes.Add(parentNode);
                            bindTree(miniList, parentNode, j);
                        }
                    }   

                }
            }
            /*Include all code*/
            else
            {
                CustomTreeNode rootNode = new CustomTreeNode(ddlSpecifications.SelectedItem.ToString(), list[0].TreeParentID.ToString());
                CustomTreeNode parentNode = new CustomTreeNode("", "");
                treeHierarchy.Nodes.Add(rootNode);
                for (int i = 0; i < list.Count(); i++)
                {
                    if (list[i].TreeName != parentNode.Text)
                    {
                        parentNode = new CustomTreeNode(list[i].TreeName, list[i].TreeParentID.ToString());
                        rootNode.ChildNodes.Add(parentNode);
                        bindTree(list.ToList(), parentNode, i);
                    }
                }
            }
            
        }
        //Need to have code jump out, and return an index for the above method to use
        protected void bindTree(List<RequirementDetails> list, CustomTreeNode parentNode, int depth)
        {
            var nodes = list.Where(x => parentNode == null ? x.TreeParentID != 0 : x.TreeParentID == int.Parse(parentNode.Value));
            foreach (var node in nodes) {
                CustomTreeNode newNode = new CustomTreeNode(node.Name, node.TreeID.ToString());
                newNode.Tag = node;
                if (parentNode == null) { 
                    treeHierarchy.Nodes.Add(newNode);
                }
                else { 
                    parentNode.ChildNodes.Add(newNode);
                    depth++;
                }
                bindTree(list, newNode, depth);
            }
            treeHierarchy.CollapseAll();
        }

        protected void nodeCheckChanged(object sender, CustomTreeNodeEventArgs e)
        {
            //RequirementDetails test = Convert.ChangeType(e.Node.Target, typeof(RequirementDetails));
            if (e.Node.Checked)
            {
                List<RequirementDetails> reqs = new List<RequirementDetails>(); 
                reqs[0] = e.Node.Tag; 
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
                //masterList.Add(e.Node.Value as RequirementDetails);
            }
            else {
                //masterList.Remove(e.Node.Value as RequirementDetails);
            }
        }
    }
}