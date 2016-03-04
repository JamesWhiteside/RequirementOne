// Applying a section from the Table of Contents as a filter and performing a freetext search with the filter applied

// Important!!! Everywhere where a class that belongs to the RequirementOne.SoapApiWrapper.RequirementOneApi namespace 
// is used, that class is defined in the SOAP reference contract. I will not go in any detail about the class 
// fields and signature since those can be seen in the SOAP contract

// this will be the final filtered and searched requirements data
List<RequirementOne.SoapApiWrapper.RequirementOneApi.RequirementDetails> requirements;
// This is the table of contents for the specification
RequirementOne.SoapApiWrapper.RequirementOneApi.TreeNode topNode; 
// The values below are required arguments when performing a search for the requirements.
// These can be altered to control pagination of results
int minRecordIndex = 0;
int maxRecordIndex = 0;
bool canUserReadWrite = false;
Guid specId = new Guid(/*place the specification id for which you are performing the search/filtering*/);
// This will be the total number of returned results once data has been retrieved - includin pagination
int totalRequirements = 0;

using (var client = new RequirementOne.SoapApiWrapper.RequirementOneApi.ReqOneApiClient())
  {
    // Get a list of defined custom fields for the specification
    List<RequirementOne.SoapApiWrapper.RequirementOneApi.CustomFieldDefinition> CustomFields =  client.SpecificationsRequirementCustomFieldsGet(
                    specId, General.AccessToken).Where(c => !c.MultiLine).ToList();
    // Filter just the definition ID of the custom fields, to be used in the search args
    int[] customDefinitionIds = CustomFields.Select(d => d.DefinitionID).ToArray();
    
    // initialize the requirement search arguments
    var args = new RequirementOne.SoapApiWrapper.RequirementOneApi.RequirementSearchArguments();
    // initialize the specification search arguments and assign the specification Id
    var specArgs = new RequirementOne.SoapApiWrapper.RequirementOneApi.SpecificationSearchArguments() { SpecificationID = specId };
    
    // Fill in each applied filter to the requirement search arguments
    // 1. Specification Id (search context)
    args.Specifications = new SpecificationSearchArguments[] { specArgs };
    
    // 2. Performing freetext search
    if (!string.IsNullOrEmpty(/*search string here*/))
      args.FreeText = "/*search string here*/";
      
    // 3. Saving the selected Table of Contents filter
    // We use allValues[QUERY_TOC] to store and pass the Table of Contents section/node ID.
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
      
      // Additional data that has to be sent along with the search and filter args
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
           
      // This is where we make the call to the client to perform the free text search
      var data = client.SpecificationsRequirementSearch(args, General.AccessToken);
      totalRequirements = data.TotalResults;
      requirements = data.Requirements.ToList();
      
      // IMPORTANT!!! This performs the free text search on the requirements, 
      // indifferent of the selected table of contents node id. The filtering to show only
      // requirements results that belong the applied TOC filter is done on the client side
      // - presentation layer.
      
      // Get the TOC contenxt for the specification:
      RequirementOne.SoapApiWrapper.RequirementOneApi.TreeNode topNode = client.SpecificationsTableOfContentsGet(specId, General.AccessToken);
      // This method handles ordering the TOC nodes based on their level
      AddLevelStrings(topNode, new Stack<int>()); 
      // Filter tree nodes.
      if (specArgs.TableOfContentsNodeID > 0)
          // This method keeps only the TOC nodes that are applied as filter, and excludes the rest from the list view
          topNode = FindTreeNode(topNode, specArgs.TableOfContentsNodeID); 
          
  }


