using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Collections.Specialized;
using RequirementOne.Core.Json;
using RequirementOne.Core.Helpers;
using RequirementOne.SoapApiWrapper.RequirementOneApi;
using umbraco.NodeFactory;
using RequirementOne.Core.Helpers.Builders;

 
namespace RequirementOne.Web.services.modules.specification
{
    /// <summary>
    /// Summary description for SpecificationTableOfContents
    /// </summary>
    public class SpecificationTableOfContents : IHttpHandler, IReadOnlySessionState
    {
        private NameValueCollection allValues;
		private bool canAdmin = false;

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
                    General.GetPostDefault.Create(Keys.NodeId),
                    General.GetPostDefault.Create(Keys.ProjectId),
                    General.GetPostDefault.Create(Keys.SpecificationId));

                // init
                json = null;
                List<string> keysToValidate = new List<string>() { Keys.NodeId, Keys.ProjectId, Keys.SpecificationId };

                json = new Json(callback, allValues, Ids.NodeId, keysToValidate.ToArray());

                // result
                if (json.IsReadyForData)
                {
                        TreeNode topnode = null;
					Guid specId = new Guid(allValues[Keys.SpecificationId]);

                        using (var client = new ReqOneApiClient())
                        {
                            topnode = client.SpecificationsTableOfContentsGet(specId, General.AccessToken);
							canAdmin = client.SecurityPermissionCheckForObject(specId.ToString(), RequirementOneObjectTypes.Specification, Permissions.ReadWriteAdmin, General.AccessToken);
                        }

                        var propertyList = new List<Node>();
                        var elementList = new List<Node>();
                        var data = General.GetPropertyAndElementLists(int.Parse(allValues[Keys.NodeId]));

                        var innerList = new List<object>();

                        string taskPath = Settings.GetDefaultPage(Enums.DefaultPage.Task);
                        var collection = new
                        {
                            sorton = "",
                            sortorder = Enums.SortingDirection.Descending.GetDescription(),
                            header = new[]
                            {
                                new { name = "name", caption = General.Translate("name").ToString(), sortable = false },
                                new { name = "details", caption = General.Translate("details").ToString(), sortable = false }
                            },
                            list = GetTableOfContents(topnode).ToArray()
                        };

                        var obj = new { collection };

                        json.SetResult(obj);
                }
            }
            catch (Exception ex)
            {
                json = new Json(callback, ex);
            }

            context.Response.Write(json.Response);
        }

        private List<object> GetTableOfContents(TreeNode topNode)
        {
            var list = new List<object>();

            var dialogId = General.GetDialog(int.Parse(allValues[Keys.NodeId]), "Edit Topic").Id;
            var deleteDialogId = General.GetDialog(int.Parse(allValues[Keys.NodeId]), "Delete Topic").Id;
            var newDialogId = General.GetDialog(int.Parse(allValues[Keys.NodeId]), "New Topic").Id;

            if (topNode != null)
            {
                list.Add(GetTopicOutput(topNode, dialogId, deleteDialogId, newDialogId, null, null));

                if (topNode.Children != null)
                {
                    if (topNode.Children.Length > 0)
                    {
                        for (int i = 0; i < topNode.Children.Length; i++)
                        {
                            var prevNode = i == 0 ? null : topNode.Children[i - 1];
                            var nextNode = i == topNode.Children.Length - 1 ? null : topNode.Children[i + 1];

                            AddTopic(topNode.Children[i], dialogId, deleteDialogId, newDialogId, prevNode, nextNode, ref list);
                        }
                    }
                }
            }

            return list;
        }

        private void AddTopic(TreeNode node, int dialogId, int deleteDialogId, int newDialogId, TreeNode prevNode, TreeNode nextNode, ref List<object> list)
        {
            if (node != null)
            {
                list.Add(GetTopicOutput(node, dialogId, deleteDialogId, newDialogId, prevNode, nextNode));

                if (node.Children != null)
                {
                    if (node.Children.Length > 0)
                    {
                        for (int i = 0; i < node.Children.Length; i++)
                        {
                            var pNode = i == 0 ? null : node.Children[i - 1];
                            var nNode = i == node.Children.Length - 1 ? null : node.Children[i + 1];

                            AddTopic(node.Children[i], dialogId, deleteDialogId, newDialogId, pNode, nNode, ref list);
                        }
                    }
                }
            }
        }

        private object GetTopicOutput(TreeNode node, int dialogId, int deleteDialogId, int createDialogId, TreeNode prevNode, TreeNode nextNode)
        {
            string nullString = null;

            var item = new
            {
                id = node.Tree_ID.ToString(),
                dialog = dialogId,
                dialogcreateaschild = createDialogId,
				canadmin = canAdmin ? "1" : "0",
                nextid = (nextNode == null ? nullString : nextNode.Tree_ID.ToString()),
                previd = (prevNode == null ? nullString : prevNode.Tree_ID.ToString()),
                hasmoveup = prevNode != null,
                hasmovedown = nextNode != null,
                parent = (node.ParentId.HasValue ? node.ParentId.Value.ToString() : nullString),
                isparent = (node.Children == null ? false : (node.Children.Length == 0 ? false : true)),
                deletedialog = deleteDialogId,
                param1 = node.Tree_ID.ToString(),
                param2 = (prevNode == null ? nullString : prevNode.Tree_ID.ToString()),
                param3 = (nextNode == null ? nullString : nextNode.Tree_ID.ToString()),
                row = new[]
                    {
                        new { caption = (string.IsNullOrEmpty(node.Name) ? "-" : (node.Name == "_root" ? General.Translate("Table of contents").ToString() : node.Name)), link = nullString },
                        new { caption = (string.IsNullOrEmpty(node.Details) ? "" : node.Details), link = nullString }
                    }
            };
            return item;
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