<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="R1EditData.aspx.cs" Inherits="RequirementONEQuickStartWeb.R1EditData" Title="ReqOne Quick Start Processing" %>

<!DOCTYPE html>
<script src="https://ajax.googleapis.com/ajax/libs/jquery/2.2.0/jquery.min.js"></script>
<script src="https://ajax.googleapis.com/ajax/libs/jqueryui/1.11.4/jquery-ui.min.js"></script>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <!--<link  href="Style.css" rel="stylesheet"/>-->
</head>
<body>
    <style>
        body, textarea, select, input
    {
      font-family: "Segoe UI", "Lucida Grande", "Arial";
      font-size: 13px;
    }
    select
    {
      width: 300px;
    }
    textarea
    {
      width: 300px;
    }
    td
    {
      padding-bottom: 4px;
    }
    div.searchCriteria, div.stats
    {
      float:left;
    }
    div.results
    {
      float:none;
    }

      .auto-style1
      {
          width: 97px;
      }

      .auto-style2
      {
          width: 4px;
      }
      .searchButton
      {
      }
      .searchButton:hover
      {
      }
        .response.write
        {
            font-family:'Times New Roman';
        }
    </style>

    <form id="form1" runat="server">

      <h3>List requirements or issues</h3>

    <div class="searchCriteria">

      <table>
        <tr>
          <td>Search for text</td>
          <td><asp:TextBox ID="tbText" runat="server" TextMode="MultiLine" Rows="3" /></td>
        </tr>
        <tr>
          <td>Project</td>
          <td><asp:DropDownList ID="ddlProjects" runat="server" OnSelectedIndexChanged=
            "ddlProjects_SelectedIndexChanged" AutoPostBack="true" /></td>
        </tr>
        <tr>
          <td>Specification</td>
          <td><asp:DropDownList ID="ddlSpecifications" runat="server" 
              AutoPostBack="true" OnSelectedIndexChanged="ddlSpecifications_IndexChanged" /></td>
          <td><asp:Button ID="btnSearchRequirements" runat="server" Text="Search"
            OnClick="btnSearchRequirements_Click" /></td>
        </tr>
        <tr>
            <td>Sections</td>
          <td><asp:DropDownList ID="ddlSections" runat="server" /></td>
          <td><asp:Button ID="btnSections" runat="server" Text="Apply"
            OnClick="btnFilterSearch_Click" /></td>
        </tr>
          
        <tr>
          <td>Review</td>
          <td><asp:DropDownList ID="ddlReviews" runat="server" /></td>
          <td><asp:Button ID="btnSearchIssues" runat="server" Text="Search"
            OnClick="btnSearchIssues_Click" /></td>
            
        </tr>
      </table>
    </div>
    <div class="stats">
      <asp:Label ID="lblStatistics" runat="server" style="font-style:italic;" />
    </div>

    
    <div><asp:Button ID="LoadButton" runat="server" Text="Download Data"
            OnClick="LoadData_Click" ToolTip="Download records as word Doc" />
    </div>
    <div>
        <asp:FileUpload ID ="UploadFile" runat="server" />
        
        <asp:Button ID="UploadChanges" runat="server" Text="Upload Data" ToolTip="Upload changes to documents" 
        OnClick="btnUpLoadData_Click" Width="135px"/>
        
    </div>
    
    <div></div>
    <div class="results">  
        <br />
        <br />
        <br />
        <br />
      <br />
      <asp:ListView ID="lvSearchResults" runat="server">
        <ItemTemplate>
          <h4>
            <asp:Label ID="Label1" Text='<%# Eval("CustomId") %>' runat="server" /> - 
            <asp:HyperLink NavigateUrl='<%# Eval("Link") %>' Text='<%# Eval("Name") %>'
               runat="server" />             
          </h4>
          <asp:Label Text='<%# Eval("Details") %>' runat="server" /><br /><br /><br />
        </ItemTemplate>
      </asp:ListView>
    </div>
    </form>

</body>
</html>
