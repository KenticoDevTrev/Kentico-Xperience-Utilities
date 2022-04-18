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

public partial class CMSPages_UpgradeToKX13Operations_PreUpgradeOperations_ConvertPageTypesForMVC : CMSPage
{
    public CMSPages_UpgradeToKX13Operations_PreUpgradeOperations_ConvertPageTypesForMVC()
    {

    }


    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        var pageBuilderPageTypes = DataClassInfoProvider.GetClasses()
            .Where("ClassName <> 'CMS.Root' and ClassIsDocumentType = 1 and COALESCE(ClassIsContentOnly, 0) = 0 and ClassIsCoupledClass = 1 and ClassUrlPattern is null")
            .OrderBy("ClassDisplayName")
            .TypedResult;

        var pageBuilderContentOnly = DataClassInfoProvider.GetClasses()
            .Where("ClassName <> 'CMS.Root' and ClassIsDocumentType = 1 and ClassIsContentOnly = 1 and ClassIsCoupledClass = 1 and CLassUrlPattern is null")
            .OrderBy("ClassDisplayName")
            .TypedResult;

        // Get any page types that are parents of any URL enabled one.
        var classWithUrlPages = ConnectionHelper.ExecuteQuery(@"select distinct OuterC.* from View_CMS_Tree_Joined outerT
left join CMS_Class outerC on OuterC.CLassID = OuterT.NodeClassID
where coalesce(OuterC.ClassURLPattern, '') = '' and OuterC.ClassName <> 'CMS.root'
and NodeID in (
select innerT.NodeParentID from View_CMS_Tree_Joined innerT
left join CMS_CLass C on C.ClassID = innerT.NodeClassID 
where coalesce(C.ClassURLPattern, '') <> '' and C.ClassName <> 'cms.root')", null, QueryTypeEnum.SQLQuery).Tables[0].Rows.Cast<DataRow>().Select(x => new DataClassInfo(x));

        bool unConvertedTypesFound = (pageBuilderPageTypes.Any() || pageBuilderContentOnly.Any());
        if(unConvertedTypesFound)
        {
            pnlFirstRun.Visible = true;
            pnlSecondRun.Visible = false;
            pnlEnablePageBuilder.Controls.Add(new Literal()
            {
                Text = "<h3>Page Types with Widgets/Content Zones possible</h3><ul>"
            });
            foreach (var pageType in pageBuilderPageTypes)
            {
                // Check to see if any Database references found
                WriteClassEntry(pageType);
            }
            pnlEnablePageBuilder.Controls.Add(new Literal()
            {
                Text = "</ul><hr/><h3>Page Types without Widgets/Content Zones possible</h3><ul>"
            });
            foreach (var pageType in pageBuilderContentOnly)
            {
                // Check to see if any Database references found
                WriteClassEntryContentOnly(pageType);
            }
            pnlEnablePageBuilder.Controls.Add(new Literal()
            {
                Text = "</ul>"
            });
        } else if(classWithUrlPages.Any())
        {
            pnlFirstRun.Visible = false;
            pnlSecondRun.Visible = true;
            

            pnlEnableUrlForParents.Controls.Add(new Literal()
            {
                Text = "<ul>"
            });

            foreach (var pageType in classWithUrlPages)
            {
                // Check to see if any Database references found
                WriteClassEntryForUrl(pageType);
            }

            pnlEnableUrlForParents.Controls.Add(new Literal()
            {
                Text = "</ul>"
            });

        } else
        {
            pnlFirstRun.Visible = false;
            pnlSecondRun.Visible = false;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }



    public void WriteClassEntry(DataClassInfo classObj)
    {
        int totalWidgetContentPages = (int)ConnectionHelper.ExecuteQuery($"select count(*) as itemCount from View_CMS_Tree_Joined where NodeClassID = {classObj.ClassID} and ((len(COALESCE(DocumentContent, '')) > 0 and DocumentContent <> '<content></content>')  or LEN(COALESCE(DocumentWebParts, '')) > 0)", null, QueryTypeEnum.SQLQuery).Tables[0].Rows[0]["itemCount"];
        int totalPages = (int)ConnectionHelper.ExecuteQuery($"select count(*) as itemCount from View_CMS_Tree_Joined where NodeClassID = {classObj.ClassID}", null, QueryTypeEnum.SQLQuery).Tables[0].Rows[0]["itemCount"];

        Panel classEntry = new Panel()
        {
            ID = $"pnlConversion1_{classObj.ClassID}",
            ClientIDMode = ClientIDMode.Static
        };
        classEntry.Controls.Add(new Literal()
        {
            Text = $"<li><strong>[{classObj.ClassName}] {classObj.ClassDisplayName}</strong> [{totalWidgetContentPages}/{totalPages} with Widget Content]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Conversion Mode: "
        });
        classEntry.Controls.Add(GetSelectControl(classObj, totalWidgetContentPages > 0, false));
        classEntry.Controls.Add(new Literal()
        {
            Text = $"<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MVC Template ID: "
        });
        classEntry.Controls.Add(GetDefaultTemplateControl(classObj, totalWidgetContentPages > 0, false));
        classEntry.Controls.Add(new Literal()
        {
            Text = $"</li>"
        });
        pnlEnablePageBuilder.Controls.Add(classEntry);
    }

    public void WriteClassEntryContentOnly(DataClassInfo classObj)
    {
        int totalPages = (int)ConnectionHelper.ExecuteQuery($"select count(*) as itemCount from View_CMS_Tree_Joined where NodeClassID = {classObj.ClassID}", null, QueryTypeEnum.SQLQuery).Tables[0].Rows[0]["itemCount"];

        Panel classEntry = new Panel()
        {
            ID = $"pnlConversion1_{classObj.ClassID}",
            ClientIDMode = ClientIDMode.Static
        };
        classEntry.Controls.Add(new Literal()
        {
            Text = $"<li><strong>[{classObj.ClassName}] {classObj.ClassDisplayName}</strong> [{totalPages} pages]<br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Conversion Mode: "
        });
        classEntry.Controls.Add(GetSelectControl(classObj, false, true));
        classEntry.Controls.Add(new Literal()
        {
            Text = $"</li>"
        });
        pnlEnablePageBuilder.Controls.Add(classEntry);
    }

    public void WriteClassEntryForUrl(DataClassInfo classObj)
    {
        Panel classEntry = new Panel()
        {
            ID = $"pnlConversion2_{classObj.ClassID}",
            ClientIDMode = ClientIDMode.Static
        };
        classEntry.Controls.Add(new Literal()
        {
            Text = $"<li><strong>[{classObj.ClassName}] {classObj.ClassDisplayName}</strong><br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Conversion Mode: "
        });
        classEntry.Controls.Add(GetSelectControl(classObj, false, true));
        classEntry.Controls.Add(new Literal()
        {
            Text = $"</li>"
        });
        pnlEnableUrlForParents.Controls.Add(classEntry);
    }

    private TextBox GetDefaultTemplateControl(DataClassInfo classObj, bool hasWidgetPages, bool contentOnly)
    {
        return new TextBox()
        {
            ID = $"tbxConversion1_{classObj.ClassID}",
            ClientIDMode = ClientIDMode.Static,
            CssClass = "form-control",
            Text = (hasWidgetPages ? $"{classObj.ClassName}_Default" : "")
        };
    }

    private DropDownList GetSelectControl(DataClassInfo classObj, bool hasWidgetPages, bool contentOnly)
    {
        DropDownList dropDownList = new DropDownList()
        {
            ID = $"ddlConversion1_{classObj.ClassID}",
            ClientIDMode = ClientIDMode.Static,
            CssClass = "form-control"
        };
        if (!contentOnly)
        {
            dropDownList.Items.AddRange(new ListItem[] {
            new ListItem("Page Builder Enabled", "pagebuilder")
            {
                Selected = hasWidgetPages
            },
            new ListItem("Url Enabled Only", "urlonly")
            {
                Selected = !hasWidgetPages
            },
            new ListItem("Neither Url nor Page Builder", "neither")
            {
                Selected = false
            }
            });
        } else
        {
            dropDownList.Items.AddRange(new ListItem[] {
            new ListItem("Url Enabled", "urlonly")
            {
                Selected = false
            },
            new ListItem("Neither Url nor Page Builder", "neither")
            {
                Selected = true
            }
            });
        }
        return dropDownList;
    }


    protected void btnAdjustClasses_Click(object sender, EventArgs e)
    {
        // loop through panel
        foreach(var pnl in pnlEnablePageBuilder.Controls.Cast<Control>().Where(x => x is Panel).Select(x => (Panel)x))
        {
            if (pnl.ClientID == null || !pnl.ClientID.StartsWith("pnlConversion1")){
                continue;
            }
            int classID = int.Parse(pnl.ID.Replace("pnlConversion1_", ""));
            DropDownList ddlControl = (DropDownList)pnl.FindControl($"ddlConversion1_{classID}");
            TextBox txtControl = (TextBox)pnl.FindControl($"tbxConversion1_{classID}");
            string query = "";
            string subQuery = "";
            switch(ddlControl.SelectedValue.ToLower())
            {
                case "pagebuilder":
                    query = $" update CMS_Class set ClassURLPattern = '{{% NodeAliasPath %}}', ClassIsContentOnly = 1, CLassIsMenuItemType = 1 where ClassID = {classID}";
                    // Set default page template on documents if specified
                    if(!string.IsNullOrWhiteSpace(txtControl.Text)) {
                        subQuery = "update CMS_Document set DocumentPageTemplateConfiguration = '{\"identifier\":\"" + txtControl.Text.Trim() + "\",\"properties\":{}}' where DocumentPageTemplateConfiguration is null and DocumentNodeID in (Select NodeID from CMS_Tree where NodeClassID = " + classID.ToString() + ")";
                    }
                    break;
                case "urlonly":
                    query = $" update CMS_Class set ClassURLPattern = '{{% NodeAliasPath %}}', ClassIsContentOnly = 1, ClassIsMenuItemType = 0 where ClassID = {classID}";
                    subQuery = $"update CMS_Document set DocumentContent = null, DocumentWebParts = null where (LEN(COALESCE(DocumentContent, ''))+LEN(COALESCE(DocumentWebParts, '')) > 0) and DocumentNodeID in (Select nodeid from CMS_Tree where NodeClassID = {classID})";
                    break;
                case "neither":
                    query = $" update CMS_Class set ClassURLPattern = '', ClassIsContentOnly = 1, ClassIsMenuItemType = 0 where ClassID = {classID}";
                    subQuery = $"update CMS_Document set DocumentContent = null, DocumentWebParts = null where (LEN(COALESCE(DocumentContent, ''))+LEN(COALESCE(DocumentWebParts, '')) > 0) and DocumentNodeID in (Select nodeid from CMS_Tree where NodeClassID = {classID})";
                    break;
            }
            ConnectionHelper.ExecuteNonQuery(query, null, QueryTypeEnum.SQLQuery);
            if(!string.IsNullOrWhiteSpace(subQuery))
            {
                ConnectionHelper.ExecuteNonQuery(query, null, QueryTypeEnum.SQLQuery);
            }
        }

        CacheHelper.ClearCache();
        SystemHelper.RestartApplication();
        URLHelper.RefreshCurrentPage();
    }

    protected void btnEnableUrlFeature_Click(object sender, EventArgs e)
    {
        // loop through panel
        foreach (var pnl in pnlEnableUrlForParents.Controls.Cast<Control>().Where(x => x is Panel).Select(x => (Panel)x))
        {
            if (pnl.ClientID == null || !pnl.ClientID.StartsWith("pnlConversion2"))
            {
                continue;
            }
            int classID = int.Parse(pnl.ID.Replace("pnlConversion2_", ""));
            DropDownList ddlControl = (DropDownList)pnl.FindControl($"ddlConversion1_{classID}");
            var classObj = DataClassInfoProvider.GetDataClassInfo(classID);

            string query = "";
            string subQuery = "";

            switch (ddlControl.SelectedValue.ToLower())
            {
                case "urlonly":
                    if (classObj.ClassIsCoupledClass)
                    {
                        query = $" update CMS_Class set ClassURLPattern = '{{% NodeAliasPath %}}', ClassIsContentOnly = 1, ClassIsMenuItemType = 0 where ClassID = {classID}";
                        subQuery = $"update CMS_Document set DocumentContent = null, DocumentWebParts = null where (LEN(COALESCE(DocumentContent, ''))+LEN(COALESCE(DocumentWebParts, '')) > 0) and DocumentNodeID in (Select nodeid from CMS_Tree where NodeClassID = {classID})";
                    } else
                    {
                        TurnContainerToCouple(classObj);
                    }
                    break;
            }
            if(!string.IsNullOrWhiteSpace(query)) { 
                ConnectionHelper.ExecuteNonQuery(query, null, QueryTypeEnum.SQLQuery);
            }
            if (!string.IsNullOrWhiteSpace(subQuery))
            {
                ConnectionHelper.ExecuteNonQuery(query, null, QueryTypeEnum.SQLQuery);
            }
        }

        CacheHelper.ClearCache();
        SystemHelper.RestartApplication();
        URLHelper.RefreshCurrentPage();
    }

    private void TurnContainerToCouple(DataClassInfo classObj)
    {
        string query = @"-- DO NOT MODIFY BELOW
declare @ClassName nvarchar(200);
declare @TableName nvarchar(200);
declare @FormIDFieldGuid nvarchar(50);
declare @FormNameFieldGuid nvarchar(50);
declare @FormIDSearchFieldGuid nvarchar(50);
declare @FormNameSearchFieldGuid nvarchar(50);
set @ClassName = @Namespace+'.'+@Name
set @TableName = @Namespace+'_'+@Name
set @FormIDFieldGuid = LOWER(Convert(nvarchar(50), NewID()));
set @FormNameFieldGuid = LOWER(Convert(nvarchar(50), NewID()));
set @FormIDSearchFieldGuid = LOWER(Convert(nvarchar(50), NewID()));
set @FormNameSearchFieldGuid = LOWER(Convert(nvarchar(50), NewID()));

-- Update Class
update CMS_Class set
ClassIsDocumentType = 1,
ClassIsCoupledClass = 1,
ClassXmlSchema = '<?xml version=""1.0"" encoding=""utf-8""?>
<xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
  <xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true"">
    <xs:complexType>
      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
        <xs:element name=""'+@TableName+'"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""'+@Name+'ID"" msdata:ReadOnly=""true"" msdata:AutoIncrement=""true"" type=""xs:int"" />
              <xs:element name=""Name"">
                <xs:simpleType>
                  <xs:restriction base=""xs:string"">
                    <xs:maxLength value=""200"" />
                  </xs:restriction>
                </xs:simpleType>
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
    <xs:unique name=""Constraint1"" msdata:PrimaryKey=""true"">
      <xs:selector xpath="".//'+@TableName+'"" />
      <xs:field xpath=""Folder2ID"" />
    </xs:unique>
  </xs:element>
</xs:schema>',
ClassFormDefinition = '<form version=""2""><field column=""'+@Name+'ID"" columntype=""integer"" guid=""'+@FormIDFieldGuid+'"" isPK=""true"" publicfield=""false""><properties><fieldcaption>'+@Name+'ID</fieldcaption></properties><settings><controlname>labelcontrol</controlname></settings></field><field column=""Name"" columnsize=""200"" columntype=""text"" guid=""'+@FormNameFieldGuid+'"" publicfield=""false"" visible=""true""><properties><fieldcaption>Name</fieldcaption></properties><settings><AutoCompleteEnableCaching>False</AutoCompleteEnableCaching><AutoCompleteFirstRowSelected>False</AutoCompleteFirstRowSelected><AutoCompleteShowOnlyCurrentWordInCompletionListItem>False</AutoCompleteShowOnlyCurrentWordInCompletionListItem><controlname>TextBoxControl</controlname><FilterMode>False</FilterMode><Trim>False</Trim></settings></field></form>',
ClassNodeNameSource = 'Name',
ClassTableName = @TableName,
ClassShowTemplateSelection = null,
ClassIsMenuItemType = null,
ClassSearchTitleColumn = 'DocumentName',
ClassSearchContentColumn='DocumentContent',
ClassSearchCreationDateColumn = 'DocumentCreatedWhen', 
ClassSearchSettings = '<search><item azurecontent=""True"" azureretrievable=""False"" azuresearchable=""True"" content=""True"" id=""'+@FormNameSearchFieldGuid+'"" name=""Name"" searchable=""False"" tokenized=""True"" /><item azurecontent=""False"" azureretrievable=""True"" azuresearchable=""False"" content=""False"" id=""'+@FormIDSearchFieldGuid+'"" name=""'+@Name+'ID"" searchable=""True"" tokenized=""False"" /></search>',
ClassInheritsFromClassID = 0,
ClassSearchEnabled = 1,
ClassIsContentOnly = 1,
ClassURLPattern = case when @EnsureUrlPattern = 1 then '{% NodeAliasPath %}' else ClassURLPattern end
where ClassName = @ClassName

-- Create table
declare @CreateTable nvarchar(max);
set @CreateTable = '
CREATE TABLE [dbo].['+@TableName+'](
	['+@Name+'ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_'+@TableName+'] PRIMARY KEY CLUSTERED 
(
	['+@Name+'ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
ALTER TABLE [dbo].['+@TableName+'] ADD  CONSTRAINT [DEFAULT_'+@TableName+'_Name]  DEFAULT (N'''') FOR [Name]'
exec(@CreateTable);

-- Populate joining table data, as well as generate the default url path entry based on nodealiaspath
declare @BindingAndVersionSQL nvarchar(max);

set @BindingAndVersionSQL = '
declare @ClassName nvarchar(200);
declare @TableName nvarchar(200);
declare @documentid int;
declare @documentname nvarchar(200);
declare @documentculture nvarchar(10);
declare @NodeID int;
declare @SiteID int;
declare @newrowid int;
set @ClassName = '''+@Namespace+'.'+@Name+'''
set @TableName = '''+@Namespace+'_'+@Name+'''
declare contenttable_cursor cursor for
 select * from (
  select
  COALESCE(D.DocumentID, NoCultureD.DocumentID) as DocumentID,
COALESCE(D.DocumentName, NoCultureD.DocumentName) as DocumentName,
 C.CultureCode,
NodeID, 
NodeSiteID
  from  CMS_Site S
    left join CMS_SiteCulture SC on SC.SiteID = S.SiteID
	left join CMS_Culture C on C.CultureID = SC.CultureID
	left join CMS_Tree on NodeSiteID = S.SiteID
	left join CMS_Class on ClassID = NodeClassID
	left outer join CMS_Document D on D.DocumentNodeID = NodeID and D.DocumentCulture = C.CultureCode
	left outer join CMS_Document NoCultureD on NoCultureD.DocumentNodeID = NodeID
	where ClassName = @ClassName
	and D.DocumentName is not null and NoCultureD.DocumentName is not null
	) cultureAcross
	group by DocumentID, DocumentName, CultureCode, NodeID, NodeSiteID
	order by DocumentID
open contenttable_cursor
fetch next from contenttable_cursor into @documentid, @documentname, @documentculture, @NodeID, @SiteID
WHILE @@FETCH_STATUS = 0  BEGIN
	-- insert into binding table --
	INSERT INTO [dbo].['+@TableName+'] ([Name]) VALUES (@documentname)
	
	-- Update document --
	set @newrowid = SCOPE_IDENTITY();
	update CMS_Document set DocumentForeignKeyValue = @newrowid where DocumentID = @documentid
	
	-- update also history --
	update CMS_VersionHistory set NodeXML = replace(NodeXML, ''<DocumentID>''+CONVERT(nvarchar(10), @documentid)+''</DocumentID>'', ''<DocumentID>''+CONVERT(nvarchar(10), @documentid)+''</DocumentID><DocumentForeignKeyValue>''+CONVERT(nvarchar(10), @newrowid)+''</DocumentForeignKeyValue>'') where DocumentID = @documentid
	
	FETCH NEXT FROM contenttable_cursor into @documentid, @documentname, @documentculture, @NodeID, @SiteID
END
Close contenttable_cursor
DEALLOCATE contenttable_cursor'
exec(@BindingAndVersionSQL)";

        // This will create the coupled table, initiate the default document of each one, and then enable the url feature
        ConnectionHelper.ExecuteNonQuery(query, new QueryDataParameters()
        {
            {"@Namespace", classObj.ClassName.Split('.')[0] },
            {"@Name", classObj.ClassName.Replace(classObj.ClassName.Split('.')[0], "").Trim('.') },
            {"@EnsureUrlPattern", true },
        }, QueryTypeEnum.SQLQuery);
            
    }
}
