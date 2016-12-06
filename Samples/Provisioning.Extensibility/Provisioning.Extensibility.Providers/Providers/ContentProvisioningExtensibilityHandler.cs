using OfficeDevPnP.Core.Framework.Provisioning.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Diagnostics;
using System.Xml.Linq;
using Provisioning.Extensibility.Providers.Helpers;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Extensions;
using Microsoft.SharePoint.Client.WebParts;
using OfficeDevPnP.Core.Utilities;
using System.Text.RegularExpressions;
using OfficeDevPnP.Core.AppModelExtensions;
namespace Provisioning.Extensibility.Providers
{

    public class ContentProvisioningExtensibilityHandler : IProvisioningExtensibilityHandler
    { 
        private readonly string logSource = "Provisioning.Extensibility.Providers.PublishingPageProvisioningExtensibilityHandler";
        private ClientContext clientContext;
        private Web web;
        private string configurationXml;
      

        public string Name
        {
            get
            {
                  return "Contents"; 
            }
        }

        #region IProvisioningExtensibilityHandler Implementation
        public void Provision(ClientContext ctx, ProvisioningTemplate template, ProvisioningTemplateApplyingInformation applyingInformation, TokenParser tokenParser, PnPMonitoredScope scope, string configurationData)
        {
            Log.Info(
                logSource,
                "ProcessRequest. Template: {0}. Config: {1}",
                template.Id,
                configurationData);

            clientContext = ctx;
            web = ctx.Web;
            configurationXml = configurationData;

            List<PublishingPage> pages = GetPublishingPagesListFromConfiguration();

            foreach (var page in pages)
            {
                try
                {
                    SetForceCheckOut(false);

                    PageHelper.AddPublishingPage(page, clientContext, web);

                    SetForceCheckOut(true);
                }
                catch (Exception ex)
                {
                    Log.Error(logSource, "Error adding publishing page: {0}. Exception: {1}", page.FileName, ex.ToString());
                }
            }
        }
        private void ExtractFile(Microsoft.SharePoint.Client.File file, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo, PnPMonitoredScope scope)
        {
            var url = file.ServerRelativeUrl;

            try
            {
                var listItem = file.EnsureProperty(f => f.ListItemAllFields);
                if (listItem != null && listItem.FieldValues != null)
                {
                    if (listItem.FieldValues.ContainsKey("WikiField")  && listItem.FieldValues["WikiField"] != null)
                    {
                        ExtractWikiPage(file, template, scope, listItem);
                    }
                    else
                    {
                        if (web.Context.HasMinimalServerLibraryVersion(Constants.MINIMUMZONEIDREQUIREDSERVERVERSION))
                        {
                            // Not a wikipage
                            template = GetFileContents(template, file, creationInfo, scope);
                           
                        }
                        else
                        {
                            scope.LogWarning("Page content export requires a server version that is newer than the current server. Server version is {0}, minimal required is {1}", web.Context.ServerLibraryVersion, Constants.MINIMUMZONEIDREQUIREDSERVERVERSION);
                        }
                    }
                }
            }
            catch (ServerException ex)
            {
                if (ex.ServerErrorCode != -2146232832)
                {
                    throw;
                }
                else
                {
                    if (web.Context.HasMinimalServerLibraryVersion(Constants.MINIMUMZONEIDREQUIREDSERVERVERSION))
                    {
                        // Page does not belong to a list, extract the file as is
                        template = GetFileContents(template, file, creationInfo, scope);
                        /*if (template.WebSettings == null)
                        {
                            template.WebSettings = new WebSettings();
                        }
                        template.WebSettings.WelcomePage = homepageUrl;*/
                    }
                    else
                    {
                        scope.LogWarning("Page content export requires a server version that is newer than the current server. Server version is {0}, minimal required is {1}", web.Context.ServerLibraryVersion, Constants.MINIMUMZONEIDREQUIREDSERVERVERSION);
                    }
                }
            }
          //  return template;
        }

        private void ExtractWikiPage(Microsoft.SharePoint.Client.File file, ProvisioningTemplate template, PnPMonitoredScope scope, ListItem listItem)
        {
            scope.LogDebug(String.Format("ExtractWikiPage {0}", file.ServerRelativeUrl));
            var fullUri = GetFullUri(web);
            var page = new Page()
            {
                Layout = WikiPageLayout.Custom,
                Overwrite = true,
                Url = fullUri.PathAndQuery.TokenizeUrl(web.Url),
            };
            var wikiField = listItem.FieldValues["WikiField"];
            var pageContents = wikiField.ToString();
            var regexClientIds = new System.Text.RegularExpressions.Regex(@"id=\""div_(?<ControlId>(\w|\-)+)");
            
            if (regexClientIds.IsMatch(pageContents))
            {
                LimitedWebPartManager limitedWPManager = file.GetLimitedWebPartManager(PersonalizationScope.Shared);
                foreach (System.Text.RegularExpressions.Match webPartMatch in regexClientIds.Matches(pageContents))
                {
                    String serverSideControlId = webPartMatch.Groups["ControlId"].Value;

                    try
                    {
                        String serverSideControlIdToSearchFor = String.Format("g_{0}",
                            serverSideControlId.Replace("-", "_"));
                        //var webParts = limitedWPManager.WebParts.ToList();
                        WebPartDefinition webPart = limitedWPManager.WebParts.GetByControlId(serverSideControlIdToSearchFor);
                        
                        if (webPart != null && webPart.Id != null)
                        {
                            web.Context.Load(webPart,
                                wp => wp.Id,
                                wp => wp.WebPart.Title,
                                wp => wp.WebPart.ZoneIndex
                                );
                            web.Context.ExecuteQueryRetry();

                            var webPartxml = TokenizeWebPartXml(web, web.GetWebPartXml(webPart.Id, file.ServerRelativeUrl));

                            page.WebParts.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.WebPart()
                            {
                                Title = webPart.WebPart.Title,
                                Contents = webPartxml,
                                Order = (uint)webPart.WebPart.ZoneIndex,
                                Row = 1, // By default we will create a onecolumn layout, add the webpart to it, and later replace the wikifield on the page to position the webparts correctly.
                                Column = 1 // By default we will create a onecolumn layout, add the webpart to it, and later replace the wikifield on the page to position the webparts correctly.
                            });

                            pageContents = Regex.Replace(pageContents, serverSideControlId, string.Format("{{webpartid:{0}}}", webPart.WebPart.Title), RegexOptions.IgnoreCase);
                        }
                    }
                    catch (PropertyOrFieldNotInitializedException)
                    {
                        scope.LogWarning("Found a WebPart ID which is not available on the server-side. ID: {0}", serverSideControlId);
                        try
                        {
                            web.Context.ExecuteQueryRetry();
                        }
                        catch
                        {
                            //suppress pending transaction
                            // avoids issues with invalid/corrupted wiki pages
                        }
                    }
                    catch (ServerException)
                    {
                        scope.LogWarning("Found a WebPart ID which is not available on the server-side. ID: {0}", serverSideControlId);
                    }
                }
            }

            page.Fields.Add("WikiField", pageContents);
            template.Pages.Add(page);
            
        }
        // private ProvisioningTemplate GetFileContents(Web web, ProvisioningTemplate template, string welcomePageUrl, ProvisioningTemplateCreationInformation creationInfo, PnPMonitoredScope scope)
        //#

        private ProvisioningTemplate GetFileContents(ProvisioningTemplate template, Microsoft.SharePoint.Client.File file, ProvisioningTemplateCreationInformation creationInfo, PnPMonitoredScope scope)
        {
            var listItem = file.EnsureProperty(p => p.ListItemAllFields);
            var fileUrl = file.ServerRelativeUrl;
            var folderPath = fileUrl.Substring(0, fileUrl.LastIndexOf("/"));

            var homeFile = new OfficeDevPnP.Core.Framework.Provisioning.Model.File()
            {
                Folder = folderPath.TokenizeUrl(web.Url),
                Src = file.ServerRelativeUrl,
                Overwrite = true,
            };

            // Add field values to file
            if (listItem != null && listItem.FieldValues != null)
            {
                homeFile.Properties = listItem.ToProvisioningValues();
            }
            // Add WebParts to file, if it is a page.
            if (System.IO.Path.GetExtension(file.ServerRelativeUrl) == ".aspx")
            {
                var webParts = web.GetWebParts(file.ServerRelativeUrl);

                foreach (var webPart in webParts)
                {
                    var webPartxml = TokenizeWebPartXml(web, web.GetWebPartXml(webPart.Id, file.ServerRelativeUrl));

                    var newWp = new OfficeDevPnP.Core.Framework.Provisioning.Model.WebPart()
                    {
                        Title = webPart.WebPart.Title,
                        Row = (uint)webPart.WebPart.ZoneIndex,
                        Order = (uint)webPart.WebPart.ZoneIndex,
                        Contents = webPartxml
                    };
#if !SP2016
                    // As long as we've no CSOM library that has the ZoneID we can't use the version check as things don't compile...
                    if (web.Context.HasMinimalServerLibraryVersion(Constants.MINIMUMZONEIDREQUIREDSERVERVERSION))
                    {
                        newWp.Zone = webPart.ZoneId;
                    }
#endif
                    homeFile.WebParts.Add(newWp);
                }
            }
            template.Files.Add(homeFile);
            creationInfo.PersistFile(folderPath, file.Name, web, scope);
            return template;
        }

        private static Uri GetFullUri(Web web)
        {
            var rootFolder = web.EnsureProperty(p => p.RootFolder);
            var homepageUrl = rootFolder.WelcomePage;
            if (string.IsNullOrEmpty(homepageUrl))
            {
                homepageUrl = "Default.aspx";
            }

            var fullUri = new Uri(UrlUtility.Combine(web.Url, homepageUrl));
            return fullUri;
        }

        private string TokenizeWebPartXml(Web web, string xml)
        {
            var lists = web.Lists;
            web.Context.Load(web, w => w.ServerRelativeUrl, w => w.Id);
            web.Context.Load(lists, ls => ls.Include(l => l.Id, l => l.Title));
            web.Context.ExecuteQueryRetry();

            foreach (var list in lists)
            {
                xml = Regex.Replace(xml, list.Id.ToString(), string.Format("{{listid:{0}}}", list.Title), RegexOptions.IgnoreCase);
            }
            xml = Regex.Replace(xml, web.Id.ToString(), "{siteid}", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, "(\"" + web.ServerRelativeUrl + ")(?!&)", "\"{site}", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, "'" + web.ServerRelativeUrl, "'{site}", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, ">" + web.ServerRelativeUrl, ">{site}", RegexOptions.IgnoreCase);
            return xml;
        }

        private IEnumerable<Microsoft.SharePoint.Client.File> GetFiles(Microsoft.SharePoint.Client.Folder folder)
        {
            var files = new List<Microsoft.SharePoint.Client.File>();
            folder.EnsureProperties(f => f.Folders, f=> f.ServerRelativeUrl, f => f.ServerRelativePath, f => f.ListItemAllFields);
            if (folder.Folders.Any())
            {
                foreach (var subfolder in folder.Folders)
                {
                    files.AddRange(GetFiles(subfolder));
                }
            }
            var folderFiles = folder.EnsureProperty(f => f.Files);
            files.AddRange(folderFiles);
            return files;
        }

        public ProvisioningTemplate Extract(ClientContext ctx, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInformation, PnPMonitoredScope scope, string configurationData)
        {

            web = ctx.Web;
            clientContext = ctx;
            ctx.Load(web);
            ctx.ExecuteQueryRetry();
            ExtractLists(template, scope);

            ExtractLibraries(template, creationInformation, scope);
            
            return template;
        }

        private void ExtractLists(ProvisioningTemplate template, PnPMonitoredScope scope)
        {
            scope.LogDebug("ContentProvisioningExtensibilityHandler.ExtractLists");

            var lists = template.Lists.Where(p => SupportedLists.Contains(p.TemplateType));
            foreach (var list in lists)
            {
                try
                {
                    scope.LogDebug(String.Format("ContentProvisioningExtensibilityHandler.ExtractLists Extracting List {0}", list.Url));
                    var spList = web.GetListByUrl(list.Url);
                    var spListItems = spList.GetItems(CamlQuery.CreateAllItemsQuery());
                    clientContext.Load(spListItems);
                    clientContext.ExecuteQueryRetry();
                    if (spListItems.AreItemsAvailable)
                        foreach (var item in spListItems)
                        {
                            list.DataRows.Add(new DataRow(item.ToProvisioningValues()));
                        }
                }
                catch (Exception e)
                {
                    scope.LogError(e, String.Format("Exception exporting list {0}", list.Title));
                    try
                    {
                        clientContext.ExecuteQueryRetry();
                    }
                    catch
                    {//suppress follow up exception
                    }
                }
            }
        }


        private void ExtractLibraries(ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInformation, PnPMonitoredScope scope)
        {
            scope.LogInfo("ContentProvisioniningExtensibilityHandler.ExtractLibraries");
            var libraries = template.Lists.Where(p => SupportedLibraryTemplateTypes.Contains(p.TemplateType) 
            && !p.Url.Contains("FiveP") && !p.Url.Contains("Style") && !p.Url.Contains("Icons")
            );
            foreach (var library in libraries)
            {
                scope.LogInfo(String.Format("ContentProvisioniningExtensibilityHandler.ExtractLibraries {0}", library.Url));

                var splibrary = web.GetListByUrl(library.Url);
                var libraryFolder = splibrary.RootFolder;
                splibrary.EnsureProperty(p => p.RootFolder);
                var sourceFiles = GetFiles(libraryFolder);
                clientContext.ExecuteQueryRetry();
                var formsPath = String.Format("{0}/Forms", libraryFolder.ServerRelativeUrl);
                var mPath = String.Format("{0}/_m/", libraryFolder.ServerRelativeUrl);
                var wPath = String.Format("{0}/_w/", libraryFolder.ServerRelativeUrl);
                var tPath = String.Format("{0}/_t/", libraryFolder.ServerRelativeUrl);
                sourceFiles = sourceFiles.Where(p =>    !p.ServerRelativeUrl.StartsWith(formsPath) &&
                                                        !p.ServerRelativeUrl.StartsWith(mPath) &&
                                                        !p.ServerRelativeUrl.StartsWith(wPath) &&
                                                        !p.ServerRelativeUrl.StartsWith(tPath)
                                                );

                foreach (var sourceFile in sourceFiles)
                {
                    ExtractFile(sourceFile, template, creationInformation, scope);
                }
            }
        }

        private static List<int> SupportedLists
        {
            get
            {
                return new List<int>(new[] {
                    (int)ListTemplateType.CustomGrid,
                    (int)ListTemplateType.GenericList,
                    (int)ListTemplateType.Links}
                );
            }
        }

        private static List<int> SupportedLibraryTemplateTypes
        {
            get
            {
                return new List<int>(new[]{
                    (int)ListTemplateType.DocumentLibrary,
                    (int)ListTemplateType.PictureLibrary,
                    (int)ListTemplateType.DataConnectionLibrary,
                    (int)ListTemplateType.WebPageLibrary,
                    850, //# OOTB Pages Library 

                });
            }
        }

        public IEnumerable<TokenDefinition> GetTokens(ClientContext ctx, ProvisioningTemplate template, string configurationData)
        {
            return new List<TokenDefinition>();
        }
        #endregion

        private void SetForceCheckOut(bool disable)
        {
            List pages = web.Lists.GetByTitle("Pages");

            pages.ForceCheckout = disable;
            pages.Update();

            clientContext.ExecuteQuery();
        }
        
        private List<PublishingPage> GetPublishingPagesListFromConfiguration()
        {
            List<PublishingPage> pages = new List<PublishingPage>();

            XNamespace ns = "http://schemas.somecompany.com/PublishingPageProvisioningExtensibilityHandlerConfiguration";
            XDocument doc = XDocument.Parse(configurationXml);

            foreach (var p in doc.Root.Descendants(ns + "Page"))
            {
                PublishingPage page = new PublishingPage
                {
                    Title = p.Attribute("Title").Value,
                    Layout = p.Attribute("Layout").Value,
                    Overwrite = bool.Parse(p.Attribute("Overwrite").Value),
                    FileName = p.Attribute("FileName").Value,
                    Publish = bool.Parse(p.Attribute("Publish").Value)
                };

                if (p.Attribute("WelcomePage") != null)
                {
                    page.WelcomePage = bool.Parse(p.Attribute("WelcomePage").Value);
                }

                var pageContentNode = p.Descendants(ns + "PublishingPageContent").FirstOrDefault();
                if (pageContentNode != null)
                {
                    page.PublishingPageContent = pageContentNode.Attribute("Value").Value;
                }

                foreach (var wp in p.Descendants(ns + "WebPart"))
                {
                    PublishingPageWebPart publishingPageWebPart = new PublishingPageWebPart();

                    if (wp.Attribute("DefaultViewDisplayName") != null)
                    {
                        publishingPageWebPart.DefaultViewDisplayName = wp.Attribute("DefaultViewDisplayName").Value;
                    }

                    publishingPageWebPart.Order = uint.Parse(wp.Attribute("Order").Value);
                    publishingPageWebPart.Title = wp.Attribute("Title").Value;
                    publishingPageWebPart.Zone = wp.Attribute("Zone").Value;

                    string webpartContensts = wp.Element(ns + "Contents").Value;
                    publishingPageWebPart.Contents = webpartContensts.Trim(new[] { '\n', ' ' });

                    page.WebParts.Add(publishingPageWebPart);
                }

                Dictionary<string, string> properties = new Dictionary<string, string>();
                foreach (var property in p.Descendants(ns + "Property"))
                {
                    properties.Add(
                        property.Attribute("Name").Value,
                        property.Attribute("Value").Value);
                }
                page.Properties = properties;

                pages.Add(page);
            }

            return pages;
        }

      
       
    }
}
