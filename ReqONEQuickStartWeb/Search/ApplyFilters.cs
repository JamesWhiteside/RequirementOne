using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using RequirementOne.Core.Json;
using RequirementOne.Core.Helpers;
using umbraco.NodeFactory;
using RequirementOne.SoapApiWrapper.RequirementOneApi;
using System.Collections.Specialized;
using System.Diagnostics;

namespace RequirementOne.Web.services.modules.specification
{
    /// <summary>
    /// Summary description for Specification
    /// </summary>
    public class Specification : IHttpHandler, IReadOnlySessionState
    {
        private NameValueCollection allValues;
        private Enums.CollectionView collectionView;

        private const bool enableDebugPrint = false;

        private const string DeleteRequirementNodeId = "deleterequirementnodeid";
        private const string RequirementNodeId = "requirementnodeid";
        private const string DeleteTreeNodeId = "deletetreenodeid";
        private const string TreeNodeId = "treenodeid";

        private const string QUERY_TOC = "query[toc]";
        private const string QUERY_SEARCH = "query[search]";
		private const string QUERY_CREATED_BY = "query[createdby]";
		private const string QUERY_UPDATED_BY = "query[updatedby]";

        private HttpContext context;
        private string requirementLink = Settings.GetDefaultPage(Enums.DefaultPage.Requirement);
        public List<CustomFieldDefinition> CustomFields;

        public void ProcessRequest(HttpContext context)
        {
            this.context = context;

            context.Response.ContentType = "text/plain";
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            Json json = null;
            var callback = General.Get(Keys.Callback);

            try
            {
                allValues = General.GetValues(
                    General.GetPostDefault.Create(Keys.ProjectId),
                    General.GetPostDefault.Create(Keys.SpecificationId),
                    General.GetPostDefault.Create(Keys.CollectionView),
                    General.GetPostDefault.Create(RequirementNodeId),
                    General.GetPostDefault.Create(TreeNodeId),
                    General.GetPostDefault.Create(DeleteRequirementNodeId),
                    General.GetPostDefault.Create(DeleteTreeNodeId),
                    General.GetPostDefault.Create(Keys.CurrentPage, "1"),
                    General.GetPostDefault.Create(Keys.PerPage, "5"),
                    General.GetPostDefault.Create(Keys.SortOn, "toc"),
                    General.GetPostDefault.Create(Keys.SortOrder, Enums.SortingDirection.Ascending.ToString()),
                    General.GetPostDefault.Create(QUERY_TOC, ""),
                    General.GetPostDefault.Create(QUERY_SEARCH, ""),
					General.GetPostDefault.Create(QUERY_CREATED_BY, ""),
					General.GetPostDefault.Create(QUERY_UPDATED_BY, ""));
                var requiredKeys = new List<string>() { Keys.ProjectId, Keys.SpecificationId, Keys.CollectionView, RequirementNodeId, TreeNodeId, DeleteRequirementNodeId, DeleteTreeNodeId };
                collectionView = Enums.GetEnum<Enums.CollectionView>(allValues[Keys.CollectionView]);

                json = new Json(callback, allValues, Ids.NodeId, requiredKeys.ToArray());

                if (json.IsReadyForData)
                {
                    switch (collectionView)
                    {
                        case Enums.CollectionView.List:
                            json.SetResult(ExecuteList());
                            break;
                        case Enums.CollectionView.Table:
                            json.SetResult(ExecuteTable());
                            break;
                        case Enums.CollectionView.Tree:
                            json.SetResult(ExecuteTree());
                            break;
                        case Enums.CollectionView.Gantt:
                            json.SetResult(ExecuteGantt());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false);
                json = new Json(callback, ex);
            }

            context.Response.Write(json.Response);
        }

        #region List
        private object ExecuteList()
        {
            List<RequirementDetails> requirements;
            TreeNode topNode;
            int minRecordIndex = 0;
            int maxRecordIndex = 0;
            int totalRequirements = 0;
            bool canUserReadWrite = false;
            Guid specId = new Guid(allValues[Keys.SpecificationId]);

            using (var client = new ReqOneApiClient())
            {
                CustomFields = client.SpecificationsRequirementCustomFieldsGet(
                    specId, General.AccessToken).Where(c => !c.MultiLine).ToList();
                int[] customDefinitionIds = CustomFields.Select(d => d.DefinitionID).ToArray();
                RequirementSearchArguments args = new RequirementSearchArguments();
                var specArgs = new SpecificationSearchArguments() { SpecificationID = specId };
                args.Specifications = new SpecificationSearchArguments[] { specArgs };

                if (!string.IsNullOrEmpty(allValues[QUERY_SEARCH]))
                    args.FreeText = allValues[QUERY_SEARCH];

				if (!string.IsNullOrEmpty(allValues[QUERY_CREATED_BY]))
					args.CreatedBy = new Guid[] { Guid.Parse(allValues[QUERY_CREATED_BY]) };

				if (!string.IsNullOrEmpty(allValues[QUERY_UPDATED_BY]))
					args.UpdatedBy = new Guid[] { Guid.Parse(allValues[QUERY_UPDATED_BY]) };

                if (!string.IsNullOrEmpty(allValues[QUERY_TOC]))
                {
                    if (allValues[QUERY_TOC].ToLower() != "all")
                    {
                        int qtoc = 0;
                        if (int.TryParse(allValues[QUERY_TOC], out qtoc))
                        {
                            specArgs.TableOfContentsNodeID = qtoc;
                            specArgs.Recursive = true;
                        }
                    }
                }

                args.DataToGet = new RequirementDataFilter();
                args.DataToGet.AttachmentCount = true;
                args.DataToGet.NoteCount = true;
                args.DataToGet.LinkCount = true;
                args.DataToGet.Timestamps = true;
                args.DataToGet.CustomFieldsIDs = customDefinitionIds;
                int currentPage = int.Parse(allValues[Keys.CurrentPage]);
                int perPage = int.Parse(allValues[Keys.PerPage]);
                minRecordIndex = (currentPage - 1) * perPage;
                maxRecordIndex = minRecordIndex + perPage;
                // Get all requirements, so we can merge them with the TOC for correct paging.
                // (TOC nodes count as paged items)
                args.SortFields = new RequirementSortCriterion[] { new RequirementSortCriterion() { Field = RequirementSearchFields.TableOfContents, Order = AscDesc.Ascending } };

                // apply filters
                if (!string.IsNullOrEmpty(context.Request.Form["query[created]"]))
                    args.MinCreatedDate = General.Dates.CalculateFilterDate(context.Request.Form["query[created]"]);
                if (!string.IsNullOrEmpty(context.Request.Form["query[search]"]))
                    args.FreeText = context.Request.Form["query[search]"];
                if (!string.IsNullOrEmpty(context.Request.Form["query[updated]"]))
                    args.MinUpdatedDate = General.Dates.CalculateFilterDate(context.Request.Form["query[updated]"]);

                // apply custom values filters
                var customValues = new Dictionary<string, string>();
                foreach (var formKey in context.Request.Form.AllKeys)
                {
                    if (formKey.StartsWith("query[customfield_"))
                    {
                        //syntax: query[customfield_237]
                        int dataId = -1;
                        if (int.TryParse(context.Request.Form[formKey], out dataId))
                        {
                            customValues.Add(formKey.Split('_')[1].Replace("]", ""), dataId.ToString());
                        }
                    }
                }
                if (customValues.Count > 0)
                    args.Specifications[0].CustomValues = customValues;

                var data = client.SpecificationsRequirementSearch(args, General.AccessToken);
                totalRequirements = data.TotalResults;
                requirements = data.Requirements.ToList();
                
                topNode = client.SpecificationsTableOfContentsGet(specId, General.AccessToken);
                AddLevelStrings(topNode, new Stack<int>());
                // Filter tree nodes.
                if (specArgs.TableOfContentsNodeID > 0)
                    topNode = FindTreeNode(topNode, specArgs.TableOfContentsNodeID);

                canUserReadWrite = client.SecurityPermissionCheckForObject(specId.ToString(), RequirementOneObjectTypes.Specification, Permissions.ReadWrite, General.AccessToken);
            }

            // create node list
            int counter = 1;
            Topic topic = BuildTopics(topNode, requirements, ref counter);

            int newRequirementDialogId = General.GetDialog("/root/Pages/MenuPage[@nodeName='Specification']/Page[@nodeName='Overview']/Page[@nodeName='Specification']", "New Requirement").Id;

            var collection = new
            {
				recordstotal = totalRequirements,
                itemstotal = totalRequirements + GetTotalNodeCount(topNode),  // needed for paging
                sorton = "fieldname",
                view = this.collectionView.GetDescription(),
                list = GetTopicsOutput(topic, newRequirementDialogId, minRecordIndex, maxRecordIndex, canUserReadWrite).ToArray()
            };

            var col = new { collection = collection };

            return col;
        }

        private void AddLevelStrings(TreeNode topNode, Stack<int> levelNumbers)
        {
            string levelString = "";

            if (levelNumbers.Count > 0)
            {
                levelString = levelNumbers
                    .Reverse()
                    .Select(n => n.ToString())
                    .Aggregate((n1, n2) => n1 + "." + n2);
                topNode.Name = levelString + " " + topNode.Name;
                
                //Debug.Print(topNode.Name);
            }

            if (topNode.Children.Count() > 0)
            {
                bool isFirstChild = true;
                int childSortOrder = 1;

                foreach (var child in topNode.Children)
                {
                    if (!isFirstChild && levelNumbers.Count > 0)
                        levelNumbers.Pop();

                    levelNumbers.Push(childSortOrder);

                    AddLevelStrings(child, levelNumbers);

                    isFirstChild = false;
                    childSortOrder++;
                }

                if (levelNumbers.Count > 0)
                    levelNumbers.Pop();
            }
        }

        private TreeNode FindTreeNode(TreeNode topNode, int nodeIdToFind)
        {
            if (topNode.Tree_ID == nodeIdToFind)
                return topNode;

            foreach (var child in topNode.Children)
            {
                var nodeFound = FindTreeNode(child, nodeIdToFind);

                if (nodeFound != null)
                    return nodeFound;
            }

            return null;
        }

        private int GetTotalNodeCount(TreeNode topNode)
        {
            int total = 0;
            GetTotalNodeCount(topNode, ref total);
            return total;
        }

        private void GetTotalNodeCount(TreeNode topNode, ref int total)
        {
            total++;

            foreach (TreeNode child in topNode.Children)
            {
                GetTotalNodeCount(child, ref total);
            }
        }

        private List<object> GetTopicsOutput(Topic topic, int newRequirementDialogId,
            int minRecordIndex, int maxRecordIndex, bool canUserReadWrite)
        {
            List<object> topicOutput = new List<object>();

            if (topic != null)
            {
                if (topic.Counter > maxRecordIndex)
                    return topicOutput;
               
                List<object> reqOutput = GetRequirementOutput(topic, minRecordIndex,
                    maxRecordIndex, canUserReadWrite);
                bool view =
                    // need the topic for viewing the reqs
                    reqOutput.Count > 0 ||
                    // the topic itself should be visible
                    (topic.Counter >= minRecordIndex && topic.Counter <= maxRecordIndex);

                if (view)
                {
                    var item = new
                    {
                        id = topic.Node.Tree_ID,
                        dialog = allValues[TreeNodeId],
                        deletedialog = allValues[DeleteTreeNodeId],
                        newrequirementdialog = newRequirementDialogId,
                        specid = allValues[Keys.SpecificationId],
                        heading = string.IsNullOrEmpty(topic.Node.Name) ? "" : topic.Node.Name,
                        description = (string.IsNullOrEmpty(topic.Node.Details) ? "" : 
                            General.FormatAsHtml(topic.Node.Details)),
                        items = reqOutput.ToArray(),
                        canuserreadwrite = canUserReadWrite ? "true" : ""
                    };
                    topicOutput.Add(item);
                }

                // recursive loop in topics
                foreach (var child in topic.Topics)
                {
                    AddTopicOutput(child, newRequirementDialogId, minRecordIndex, maxRecordIndex, 
                        ref topicOutput, canUserReadWrite);
                }
            }

            return topicOutput;
        }

        void DebugPrint(string format, params object[] parameters)
        {
            if (enableDebugPrint)
                Debug.Print(format, parameters);
        }

        /// <summary>
        /// Recursively build topic output (from a Topic tree to a list of objects).
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="newRequirementDialogId"></param>
        /// <param name="topicOutput"></param>
        private void AddTopicOutput(Topic topic, int newRequirementDialogId,
            int minRecordIndex, int maxRecordIndex,
            ref List<object> topicOutput, bool canUserReadWrite)
        {
            if (topic != null)
            {
                if (topic.Counter > maxRecordIndex)
                    return;

                List<object> reqOutput = GetRequirementOutput(topic, minRecordIndex, 
                    maxRecordIndex, canUserReadWrite);
                bool view =
                    // need the topic for viewing the reqs
                    reqOutput.Count > 0 ||
                    // the topic itself should be visible
                    (topic.Counter >= minRecordIndex && topic.Counter <= maxRecordIndex);

                if (view)
                {
                    var item = new
                    {
                        dialog = allValues[TreeNodeId],
                        id = topic.Node.Tree_ID,
                        deletedialog = allValues[DeleteTreeNodeId],
                        newrequirementdialog = newRequirementDialogId,
                        specid = allValues[Keys.SpecificationId],
                        heading = (string.IsNullOrEmpty(topic.Node.Name) ? "" : topic.Node.Name),
                        description = (string.IsNullOrEmpty(topic.Node.Details) ? "" : General.FormatAsHtml(topic.Node.Details)),
                        items = reqOutput.ToArray(),
                        canuserreadwrite = canUserReadWrite ? "true" : ""
                    };

                    topicOutput.Add(item);
                }

                // recursive loop in topics
                foreach (var child in topic.Topics)
                {
                    AddTopicOutput(child, newRequirementDialogId, minRecordIndex, maxRecordIndex, ref topicOutput, canUserReadWrite);
                }
            }
        }

        private List<object> GetRequirementOutput(Topic topic, int minRecordIndex, int maxRecordIndex, bool canUserReadWrite)
        {
            List<object> requirementOutput = new List<object>();

            if (topic.Requirements != null)
            {
                for (int i = 0; i < topic.Requirements.Count; i++)
                {
                    if (topic.Requirements[i].Counter < minRecordIndex)
                        continue;   // Haven't got to the first index; keep looking.
                    if (topic.Requirements[i].Counter > maxRecordIndex)
                        break;      // No need to look farther; exit.

                    var thisReq = topic.Requirements[i];
                    var prevReq = i > 0 ? topic.Requirements[i - 1] : null;
                    var nextReq = i + 1 >= topic.Requirements.Count ? null : topic.Requirements[i + 1];
                    QueryStringHelper qs = new QueryStringHelper(true);
                    qs.Add(Keys.ProjectId, allValues[Keys.ProjectId]);
                    qs.Add(Keys.SpecificationId, thisReq.Requirement.SpecificationID.ToString());
                    qs.Add(Keys.RequirementId, thisReq.Requirement.RequirementID.ToString());

                    var item = new
                    {
                        dialog = allValues[RequirementNodeId],
                        deletedialog = allValues[DeleteRequirementNodeId],
                        id = thisReq.Requirement.RequirementID.ToString(),
                        nextid = nextReq == null ? "" : nextReq.Requirement.SortOrder.ToString(),
                        previd = prevReq == null ? "" : prevReq.Requirement.SortOrder.ToString(),
                        treeid = thisReq.TreeNodeId,
                        link = requirementLink + qs.QueryString,
                        name = (string.IsNullOrEmpty(thisReq.Requirement.Name) ? "" : thisReq.Requirement.Name),
                        description = (string.IsNullOrEmpty(thisReq.Requirement.Details) ? "" : General.FormatAsHtml(thisReq.Requirement.Details)),
                        properties = GetPropertiesOutput(thisReq.Requirement).ToArray(),
                        icons = GetIconsOutput(thisReq.Requirement).ToArray(),
                        canuserreadwrite = canUserReadWrite ? "true" : ""
                    };

                    requirementOutput.Add(item);
                }
            }

            return requirementOutput;            
        }
        private List<object> GetPropertiesOutput(RequirementDetails req)
        {
            List<object> props = new List<object>();

            if (!string.IsNullOrEmpty(req.CustomIdentifier))
                props.Add(new { caption = "id", value = req.CustomIdentifier });

            props.Add(new { caption = General.Translate("created").ToString(), value = General.Dates.FormatHTMLDateTime(req.Created) });
            var createdByName = req.CreatedByName;
            if (!string.IsNullOrEmpty(createdByName))
                props.Add(new { caption = General.Translate("created by").ToString(), value = req.CreatedByName }); 
            
            props.Add(new { caption = General.Translate("updated").ToString(), value = General.Dates.FormatHTMLDateTime(req.Updated) });
            var updatedByName = req.UpdatedByName;
            if (!string.IsNullOrEmpty(updatedByName))
                props.Add(new { caption = General.Translate("updated by").ToString(), value = req.UpdatedByName });

            // custom fields
            if (CustomFields != null)
            {
                foreach (var cf in req.CustomFieldDataAsStrings)
                {
                    var found = CustomFields.Find(c => cf.Key == c.DefinitionID);
                    if (found != null)
                    {
                        var name = found.Name;
                        var value = cf.Value;
                        props.Add(new { caption = General.Translate(name).ToString(), value = (string.IsNullOrEmpty(value) ? "-" : value) });
                    }
                }
            }

            return props;            
        }
        private List<object> GetIconsOutput(RequirementDetails req)
        {
            List<object> icons = new List<object>();

            if (req.NoteCount != 0)
                icons.Add(new { icon = "note", value = req.NoteCount.ToString() });

            if (req.AttachmentCount != 0)
                icons.Add(new { icon = "attachment", value = req.AttachmentCount.ToString() });

            if (req.LinkCount != 0)
                icons.Add(new { icon = "link", value = req.LinkCount.ToString() });

            return icons;            
        }

        private Topic BuildTopics(TreeNode node, List<RequirementDetails> requirements, ref int counter)
        {
            if (node == null)
                return null;

            Topic topic = new Topic(node, counter++);

            // Get the requirements in the current tree node.
            var reqs = requirements.FindAll(r => r.TreeID == node.Tree_ID);
            
            foreach (var req in reqs)
            {
                var countedReq = new CountedRequirementDetails(counter++, req, req.TreeID);

                topic.Requirements.Add(countedReq);
            }
            

            if (node.Children != null)
            {
                for (int i = 0; i < node.Children.Length; i++)
                {
                    AddTopic(node.Children[i], requirements, topic, i + 1, ref counter);
                }
            }

            return topic;
        }

        /// <summary>
        /// Create topics recursively.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="requirements"></param>
        /// <param name="parent"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        private Topic AddTopic(TreeNode node, List<RequirementDetails> requirements, Topic parent, int sortOrder, ref int counter)
        {
            if (node == null)
                return null;

            Topic topic = new Topic(node, counter++, parent);

            var reqs = requirements.FindAll(r => r.TreeID == node.Tree_ID);

            foreach (var req in reqs)
            {
                var counted = new CountedRequirementDetails(counter++, req, req.TreeID);

                topic.Requirements.Add(counted);
            }

            topic.SortOrder = sortOrder;

            if (node.Children != null)
            {
                for (int i = 0; i < node.Children.Length; i++)
                {
                    AddTopic(node.Children[i], requirements, topic, i + 1, ref counter);
                }
            }

            parent.Topics.Add(topic);

            return topic;
        } 

        public class CountedRequirementDetails
        {
            public int Counter { get; set; }
            public RequirementDetails Requirement { get; set; }
            public int TreeNodeId { get; set; }

            public CountedRequirementDetails(int counter, RequirementDetails req, int treeNodeId)
            {
                this.Counter = counter;
                this.Requirement = req;
                this.TreeNodeId = treeNodeId;
            }
        }
        private class Topic
        {
            public TreeNode Node { get; private set; }
            public Topic Parent { get; set; }
            public List<Topic> Topics { get; set; }
            public List<CountedRequirementDetails> Requirements { get; set; }
            public int SortOrder { get; set; }
            public int Counter { get; set; }

            public Topic(TreeNode node, int counter)
                : this(node, counter, null)
            {                
            }
            public Topic(TreeNode node, int counter, Topic parent)
            {
                this.Node = node;
                this.Parent = parent;
                this.Topics = new List<Topic>();
                this.Requirements = new List<CountedRequirementDetails>();
                this.Counter = counter;
            }

            //public string LevelString
            //{
            //    get
            //    {
            //        return "";

            //        List<int> numbers = new List<int>();
            //        var current = this;
            //        do
            //        {
            //            numbers.Add(current.SortOrder);
            //            current = current.Parent;
            //        }
            //        while (current != null);
                    
            //        numbers.Reverse();

            //        if (numbers.Count > 0)
            //            numbers.RemoveAt(0);

            //        var result = string.Empty;

            //        foreach (var i in numbers)
            //        {
            //            result += i.ToString() + ".";
            //        }

            //        return result.Trim('.');
            //    }
            //}

            public override string ToString()
            {
                if (this.Node != null)
                    return this.Node.Name;
                else
                    return "";
            }
        }
        #endregion

        #region Table
        private object ExecuteTable()
        {
			Guid specId = new Guid(allValues[Keys.SpecificationId]);
            List<RequirementDetails> requirements;
            int totalCount = 0;
			bool canUserReadWrite = false;

            using (var client = new ReqOneApiClient())
            {
				CustomFields = client.SpecificationsRequirementCustomFieldsGet(specId, General.AccessToken).Where(c => !c.MultiLine).ToList();
                int[] customDefinitionIds = CustomFields.Select(d => d.DefinitionID).ToArray();
                RequirementSearchArguments args = new RequirementSearchArguments();
                args.Specifications = new SpecificationSearchArguments[] { new SpecificationSearchArguments() { SpecificationID = new Guid(allValues[Keys.SpecificationId]) } };
                args.DataToGet = new RequirementDataFilter();
                args.DataToGet.AttachmentCount = false;
                args.DataToGet.NoteCount = false;
                args.DataToGet.LinkCount = false;
                args.DataToGet.Timestamps = false;
                args.DataToGet.CustomFieldsIDs = customDefinitionIds;
                int currentPage = int.Parse(allValues[Keys.CurrentPage]);
                int perPage = int.Parse(allValues[Keys.PerPage]);
                args.MinRowIndex = (currentPage - 1) * perPage;
                args.MaxRowIndex = args.MinRowIndex + perPage;

                // apply sorting
                Enums.SortingDirection sortDir = Enums.SortingDirection.Ascending;
                if (!string.IsNullOrEmpty(allValues[Keys.SortOrder]))
                {
                    sortDir = Enums.GetEnum<Enums.SortingDirection>(allValues[Keys.SortOrder]);
                }
                var sortOn = "toc";
                if (!string.IsNullOrEmpty(allValues[Keys.SortOn]))
                {
                    sortOn = allValues[Keys.SortOn].ToLower();
                }

                RequirementSortCriterion sort = new RequirementSortCriterion();
                sort.Field = RequirementSearchFields.TableOfContents;
                switch (sortOn)
                {
                    case "toc": sort.Field = RequirementSearchFields.TableOfContents; break;
                    case "id": sort.Field = RequirementSearchFields.CustomID; break;
                    case "name": sort.Field = RequirementSearchFields.Name; break;
                }
                if (sort.Field == RequirementSearchFields.TableOfContents)
                {
                    sort.Order = AscDesc.Ascending;
                }
                else
                {
                    switch (sortDir)
                    {
                        case Enums.SortingDirection.Ascending: sort.Order = AscDesc.Ascending; break;
                        case Enums.SortingDirection.Descending: sort.Order = AscDesc.Descending; break;
                    }
                }
                args.SortFields = new RequirementSortCriterion[] { sort };

                // apply toc filter
                if (!string.IsNullOrEmpty(allValues[QUERY_TOC]))
                {
                    if (allValues[QUERY_TOC].ToLower() != "all")
                    {
                        int qtoc = 0;
                        if (int.TryParse(allValues[QUERY_TOC], out qtoc))
                        {
                            if (args.Specifications.Count() > 0)
                            {
                                args.Specifications[0].TableOfContentsNodeID = qtoc;
                                args.Specifications[0].Recursive = true;
                            }
                        }
                    }
                }

                // apply other filters
                if (!string.IsNullOrEmpty(context.Request.Form["query[created]"]))
                    args.MinCreatedDate = General.Dates.CalculateFilterDate(context.Request.Form["query[created]"]);
                if (!string.IsNullOrEmpty(context.Request.Form[QUERY_SEARCH]))
                    args.FreeText = context.Request.Form[QUERY_SEARCH];
                if (!string.IsNullOrEmpty(context.Request.Form["query[updated]"]))
                    args.MinUpdatedDate = General.Dates.CalculateFilterDate(context.Request.Form["query[updated]"]);

                // apply custom values filters
                var customValues = new Dictionary<string, string>();
                foreach (var formKey in context.Request.Form.AllKeys)
                {
                    if (formKey.StartsWith("query[customfield_"))
                    {
                        //syntax: query[customfield_237]
                        int dataId = -1;
                        if (int.TryParse(context.Request.Form[formKey], out dataId))
                        {
                            customValues.Add(formKey.Split('_')[1].Replace("]", ""), dataId.ToString());
                        }
                    }
                }
                if (customValues.Count > 0)
                    args.Specifications[0].CustomValues = customValues;

                var data = client.SpecificationsRequirementSearch(args, General.AccessToken);
                totalCount = data.TotalResults;
                requirements = data.Requirements.ToList();
				canUserReadWrite = client.SecurityPermissionCheckForObject(specId.ToString(), RequirementOneObjectTypes.Specification, Permissions.ReadWrite, General.AccessToken);
            }

            var collection = new
            {
                sorton = allValues[Keys.SortOn],
                sortorder = allValues[Keys.SortOrder],
                itemstotal = totalCount,
                currentpage = int.Parse(allValues[Keys.CurrentPage]),
                header = new[]
                            {
                                new { name = "toc", caption = General.Translate("TOC section").ToString(), sortable = true },
                                new { name = "id", caption = General.Translate("Id").ToString(), sortable = true },
                                new { name = "name", caption = General.Translate("Name").ToString(), sortable = true },
                                new { name = "details", caption = General.Translate("Details").ToString(), sortable = false },
								//new { name = "attributes", caption = General.Translate("Attributes").ToString(), sortable = false }
                            },
				list = GetTableOutput(requirements, canUserReadWrite).ToArray()
            };

            var obj = new { collection };

            return obj;
        }

        private List<object> GetTableOutput(List<RequirementDetails> requirements, bool canUserReadWrite)
        {
            var list = new List<object>();
            string nullString = null;
            var dialogId = allValues[RequirementNodeId];
            var deleteDialogId = allValues[DeleteRequirementNodeId];

            var pathReq = Settings.GetDefaultPage(Enums.DefaultPage.Requirement);

            foreach (var req in requirements)
            {
                QueryStringHelper qs = new QueryStringHelper(true);
                qs.Add(Keys.ProjectId, allValues[Keys.ProjectId]);
                qs.Add(Keys.SpecificationId, req.SpecificationID.ToString());
                qs.Add(Keys.RequirementId, req.RequirementID.ToString());

                var reqLink = pathReq + qs.QueryString;

                var item = new
                {
                    id = req.RequirementID.ToString(),
                    dialog = dialogId,
                    deletedialog = deleteDialogId,
                    row = new[]
                    {
                        new { caption = (string.IsNullOrEmpty(req.TreeName) ? "-" : req.TreeName.Trim()), link = nullString },
                        new { caption = (string.IsNullOrEmpty(req.CustomIdentifier) ? "-" : req.CustomIdentifier.Trim()), link = nullString },
                        new { caption = (string.IsNullOrEmpty(req.Name) ? "-" : req.Name.Trim()), link = reqLink },
                        new { caption = (string.IsNullOrEmpty(req.Details) ? "-" : req.Details.Trim()), link = nullString },
						//new { caption = (string.IsNullOrEmpty("custom fields") ? "-" : GetCustomFieldsText(req).Trim()), link = nullString }
                    },
					canuserreadwrite = canUserReadWrite ? "true" : ""
                };

                list.Add(item);
            }

            return list;
        }
        private string GetCustomFieldsText(RequirementDetails req)
        {
            List<RequirementCustomFieldData> cData = new List<RequirementCustomFieldData>();
            using (var client = new ReqOneApiClient())
            {
                cData = client.SpecificationsRequirementCustomDataGet(req.RequirementID, General.AccessToken).ToList();
            }

            List<string> data = new List<string>();
            if (cData != null)
                cData.ForEach(x => data.Add(x.Name + ": " + x.Value));

            return string.Join("<br>", data.ToArray());
        } 
        #endregion

        public List<CustomFieldDefinition> GetCustomFieldDefinitions(Guid specId)
        {
            List<CustomFieldDefinition> defs = new List<CustomFieldDefinition>();
            using (var client = new ReqOneApiClient())
            {
                defs = client.SpecificationsRequirementCustomFieldsGet(specId, General.AccessToken)
                    .Where(d => d.Type == CustomFieldType.ListOfValues).ToList();
            }
            return defs;
        }

        private bool ExecuteTree()
        {
            throw new NotImplementedException();
        }

        private bool ExecuteGantt()
        {
            throw new NotImplementedException();
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