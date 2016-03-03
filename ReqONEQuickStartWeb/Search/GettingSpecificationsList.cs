using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using RequirementOne.Core.Helpers;
using RequirementOne.Core.Json;
using RequirementOne.SoapApiWrapper.RequirementOneApi;
using umbraco.NodeFactory;
using System.Collections.Specialized;

namespace RequirementOne.Web.services.specification
{
    /// <summary>
    /// Summary description for SpecificationOverview
    /// </summary>
    public class SpecificationOverview : IHttpHandler, IRequiresSessionState
    {
        // defaults
        private static string DEFAULT_SORT = "";

        // values
        private static NameValueCollection allValues;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            Json json = null;
            var callback = General.Get(Keys.Callback);

            try
            {
                // values
                allValues = General.GetValues(
                    General.GetPostDefault.Create(Keys.SortOn, DEFAULT_SORT),
                    General.GetPostDefault.Create(Keys.NodeId),
                    General.GetPostDefault.Create(Keys.ProjectId, RequirementOne.Core.Projects.ProjectManager.GetCurrentProject().ProjectID.ToString()),
                    General.GetPostDefault.Create(Keys.CurrentPage, "1"),
                    General.GetPostDefault.Create(Keys.PerPage, "5")                    
                );

                // init
                json = new Json(callback, allValues, Ids.NodeId, Keys.ProjectId, Keys.NodeId/*, Keys.SortOn*/);

                // result
                if (json.IsReadyForData)
                {
                    Node node = new Node(int.Parse(allValues[Keys.NodeId]));
                    int totalCount = 0;
                    var list = CreateList(node, allValues[Keys.ProjectId], allValues[Keys.SortOn], ref totalCount);

                    json.SetResult(list, totalCount, allValues[Keys.SortOn]);

                }
            }
            catch (Exception ex)
            {
                json = new Json(callback, ex);
            }

            context.Response.Write(json.Response);
        }

        private List<object> CreateList(Node node, string projectId, string sort, ref int totalCount)
        {
            List<object> list = new List<object>();

            List<SpecificationsStatisticsBasic> specifications = null;
            using (ReqOneApiClient client = new ReqOneApiClient())
            {
                int currentPage = int.Parse(allValues[Keys.CurrentPage]);
                int perPage = int.Parse(allValues[Keys.PerPage]);
                var items = client.DashboardSpecificationsStatisticsBasicGet(new Guid(allValues[Keys.ProjectId]), General.AccessToken);
                totalCount = items.Length;
                specifications = items.Skip((currentPage - 1) * perPage).Take(perPage).ToList();
            }

            switch (sort)
            {
                case "records total":
                    specifications.Sort((x, y) => x.RecordsTotal.CompareTo(y.RecordsTotal));
                    break;
                case "records created last 7 days":
                    specifications.Sort((x, y) => x.RecordsCreatedLast7Days.CompareTo(y.RecordsCreatedLast7Days));
                    break;
                case "records updated last 7 days":
                    specifications.Sort((x, y) => x.RecordsUpdatedLast7Days.CompareTo(y.RecordsUpdatedLast7Days));
                    break;
                default:
                    specifications.Sort((x, y) => x.SpecificationName.CompareTo(y.SpecificationName));
                    break;
            }
            
            var specificationLink = Settings.GetDefaultPage(Enums.DefaultPage.Specification);
            var specificationEditLink = Settings.GetDefaultPage(Enums.DefaultPage.ConfigureSpecification);

            foreach (var spec in specifications)
            {
                var qs = new QueryStringHelper(true);
                qs.Add(Keys.ProjectId, allValues[Keys.ProjectId]);
                qs.Add(Keys.SpecificationId, spec.SpecificationID.ToString());

                var item = new
                {
                    name = spec.SpecificationName,
                    icon = "specification",
                    link = specificationLink + qs.QueryString,
                    editlink = specificationEditLink + qs.QueryString,
                    items = General.CreateItems<SpecificationsStatisticsBasic>(node, spec).ToArray()
                };
                list.Add(item);
            }

            return list;
        }
        
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        
    }
}