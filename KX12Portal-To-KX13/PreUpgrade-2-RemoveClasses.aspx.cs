using CMS.Base;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.EventLog;
using CMS.Helpers;
using CMS.Membership;
using CMS.SiteProvider;
using CMS.UIControls;
using CMS.WorkflowEngine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TreeNode = CMS.DocumentEngine.TreeNode;

public partial class CMSPages_UpgradeToKX13Operations_PreUpgradeOperations_RemoveClasses : CMSPage
{
    public CMSPages_UpgradeToKX13Operations_PreUpgradeOperations_RemoveClasses()
    {


    }


    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        var pageTypes = DataClassInfoProvider.GetClasses()
            .WhereTrue("ClassIsDocumentType")
            .Where("ClassID not in (Select distinct NodeClassID from View_CMS_Tree_Joined) and ClassID not in (select Sub.ClassInheritsFromClassID from CMS_Class Sub where Sub.ClassInheritsFromClassID is not null and Sub.ClassInheritsFromClassID <> 0)")
            .TypedResult;

        foreach (var pageType in pageTypes)
        {
            // Check to see if any Database references found
            WriteClassEntry(pageType);
        }

        var usedPageTypes = DataClassInfoProvider.GetClasses()
            .WhereTrue("ClassIsDocumentType")
            .Where("ClassID in (Select distinct NodeClassID from View_CMS_Tree_Joined) and ClassID not in (select Sub.ClassInheritsFromClassID from CMS_Class Sub where Sub.ClassInheritsFromClassID is not null and Sub.ClassInheritsFromClassID <> 0)")
            .OrderBy($"(select count(*) from View_CMS_Tree_Joined subQuery where subQuery.NodeCLassID = ClassID)")
            .TypedResult;

        foreach (var pageType in usedPageTypes)
        {
            // Check to see if any Database references found
            WriteClassEntryForUsed(pageType);
        }

    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }



    public void WriteClassEntry(DataClassInfo classObj)
    {
        var tableFieldOnlyQueryLookups = new List<Tuple<string, string, bool>>
        {
            new Tuple<string, string, bool>("CMS_Class", "ClassFormDefinition", false),
            new Tuple<string, string, bool>("CMS_AlternativeForm", "FormDefinition", false),
            new Tuple<string, string, bool>("CMS_Document", "DocumentContent", true),
            new Tuple<string, string, bool>("CMS_Document", "DocumentWebParts", true),
            new Tuple<string, string, bool>("CMS_EmailTemplate", "EmailTemplateText", false),
            new Tuple<string, string, bool>("CMS_EmailTemplate", "EmailTemplatePlainText", false),
            new Tuple<string, string, bool>("CMS_SearchIndex", "IndexQueryKey", false),
            new Tuple<string, string, bool>("CMS_SearchIndex", "IndexCustomAnalyzerClassName", false),
            new Tuple<string, string, bool>("CMS_SettingsKey", "KeyFormControlSettings", false),
            new Tuple<string, string, bool>("CMS_SettingsKey", "KeyValue", false),
            new Tuple<string, string, bool>("CMS_UIElement", "ElementProperties", false),
            new Tuple<string, string, bool>("CMS_UIElement", "ElementAccessCondition", false),
            new Tuple<string, string, bool>("CMS_UIElement", "ElementVisibilityCondition", false),
            new Tuple<string, string, bool>("CMS_WebPart", "WebPartProperties", true),
            new Tuple<string, string, bool>("CMS_Widget", "WidgetProperties", true),
            new Tuple<string, string, bool>("Newsletter_EmailTemplate", "TemplateCode", false),
            new Tuple<string, string, bool>("Newsletter_EmailWidget", "EmailWidgetProperties", false)
        };

        List<string> referencesFound = new List<string>();
        // Loop through queries and look for references
        string entryText = CacheHelper.Cache(cs =>
        {
            if (cs.Cached)
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(new string[]
                {
                    $"CMS.Class|all",
                    $"CMS.AlternativeForm|all",
                    $"CMS.Document|all",
                    $"CMS.EmailTemplate|all",
                    $"CMS.SearchIndex|all",
                    $"CMS.SettingsKey|all",
                    $"CMS.UIElement|all",
                    $"CMS.WebPart|all",
                    $"CMS.Widget|all",
                    $"Newsletter.EmailTemplate|all",
                    $"Newsletter.EmailWidget|all"
                });
            }
            var entry = $"[{classObj.ClassName}] {classObj.ClassDisplayName} [";

            foreach (var lookup in tableFieldOnlyQueryLookups.Where(x => !x.Item3))
            {
                int refs = References(classObj.ClassName, lookup.Item1, lookup.Item2);
                if (refs > 0)
                {
                    entry += $"{lookup.Item1}.{lookup.Item2}x{refs}, ";
                }
            }
            foreach (var lookup in tableFieldOnlyQueryLookups.Where(x => x.Item3))
            {
                bool itemsFound = false;
                foreach (var queryName in QueryInfoProvider.GetQueries().WhereEquals("ClassID", classObj.ClassID).TypedResult)
                {
                    var lookupItem = $"{classObj.ClassName}.{queryName}";
                    int refs = References(lookupItem, lookup.Item1, lookup.Item2);

                    if (refs > 0 && itemsFound)
                    {
                        itemsFound = true;
                        entry += $"{lookup.Item1}.{lookup.Item2}-{lookupItem}x{refs}";
                    }
                    else if (refs > 0)
                    {
                        entry += $"|{lookupItem}x{refs}";
                    }
                }
                if (itemsFound)
                {
                    entry += ", ";
                }
            }
            if (entry[entry.Length - 1] == '[')
            {
                entry = entry.Substring(0, entry.Length - 1);
            }
            else
            {
                entry = entry.Substring(0, entry.Length - 2) + "]";
            }
            return entry;
        }, new CacheSettings(60, "refLookupForClasses", classObj.ClassName));
        cbxClasses.Items.Add(new ListItem(entryText, classObj.ClassID.ToString()));
    }

    public void WriteClassEntryForUsed(DataClassInfo classObj)
    {
        int totalDocs = Convert.ToInt32(ConnectionHelper.ExecuteQuery($"select count(*) as totalCount from VIew_CMS_Tree_Joined where NodeClassID = {classObj.ClassID}", null, QueryTypeEnum.SQLQuery).Tables[0].Rows[0]["totalCount"]);
        var entry = $"[{classObj.ClassName}] {classObj.ClassDisplayName} [{totalDocs} Documents]";

        cbxUsedClasses.Items.Add(new ListItem(entry, classObj.ClassID.ToString()));
    }

    public int References(string lookup, string table, string column)
    {
        return ValidationHelper.GetInteger(ConnectionHelper.ExecuteQuery($"Select Count(*) from {table} where {column} like '%{SqlHelper.EscapeQuotes(lookup)}%'", null, QueryTypeEnum.SQLQuery).Tables[0].Rows[0][0], 0);
    }


    protected void btnDeleteClasses_Click(object sender, EventArgs e)
    {
        var selected = cbxClasses.Items.Cast<ListItem>().Where(x => x.Selected);
        var classIds = selected.Select(x => ValidationHelper.GetInteger(x.Value, 0));
        foreach (var classObj in DataClassInfoProvider.GetClasses().WhereIn("ClassID", classIds.ToArray()).TypedResult)
        {
            classObj.Delete();
        }
        // remove entries
        
        foreach (var selectedItem in selected)
        {
            try { 
            cbxClasses.Items.Remove(selectedItem);
            } catch { }
        }
        ltrResult.Text = $"{selected.Count()} Classes Removed.";
    }

    protected void btnDeletePagesAndClasses_Click(object sender, EventArgs e)
    {
        var selected = cbxUsedClasses.Items.Cast<ListItem>().Where(x => x.Selected);
        var classIds = selected.Select(x => ValidationHelper.GetInteger(x.Value, 0));
        // Delete all pages

        foreach (var classObj in DataClassInfoProvider.GetClasses().WhereIn("ClassID", classIds.ToArray()).TypedResult)
        {
            // Delete pages first
            foreach (var page in DocumentHelper.GetDocuments().WhereEquals("NodeClassID", classObj.ClassID).Published(false).LatestVersion().AllCultures().TypedResult)
            {
                try
                {
                    page.Destroy();   
                }
                catch { }
            }
            classObj.Delete();
        }
        // remove entries
        try
        {
            foreach (var selectedItem in selected)
            {
                cbxUsedClasses.Items.Remove(selectedItem);
            }
        } catch { }
        ltrResult.Text = $"{selected.Count()} Classes Removed.";
    }
}
