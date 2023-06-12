﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Azure.Core;
using MCOM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using Microsoft.SharePoint.Client.Taxonomy;

namespace MCOM.Services
{
    public interface ISharePointService
    {
        ClientContext GetClientContext(string webUrl, string token);
        List GetListById(ClientContext clientContext, Guid listId);
        ListItemCollection GetListItems(ClientContext clientContext, List list, CamlQuery query);
        ListItem GetListItemByUniqueId(ClientContext clientContext, List list, Guid uniqueId);
        ListItem GetListItemById(ClientContext clientContext, List list, int id);
        string GetListItemRetentionLabel(ClientContext clientContext, Guid list, int id);
        bool SetListItemRetentionLabel(ClientContext clientContext, Guid listId, int id, string label);
        bool ValidateItemRetentionLabel(ClientContext siteContext, string listId, string listItemId);
        ResultTable SearchItems(ClientContext clientContext, string queryText);
        ResultTable SearchItems(ClientContext clientContext, string queryText, int maxQuantity, Guid resultSourceId);
        ResultTable SearchItems(ClientContext clientContext, string queryText, string[] properties, int maxQuantity, Guid resultSourceId);
        List<ListItem> GetListAsGenericList(ListItemCollection listItemCollection);
        TaxonomySession GetTaxonomySession(ClientContext clientContext);
        TermStore GetDefaultSiteCollectionTermStore(TaxonomySession taxonomySession);
        IQueryable<TermGroup> GetTermStoreGroups(TermStore termStore, string termGroup);
        IQueryable<TermSet> GetTermSets(TermGroup termGroup, string termSetName);
        TermCollection GetTerms(TermSet termSet, LabelMatchInformation label);
        TermCollection GetAllTerms(TermSet termSet);
        LabelMatchInformation GetLabelMatchInformation(ClientContext clientContext, string termName);
        void Load<T>(ClientContext clientContext, T clientObject, params Expression<Func<T, object>>[] retrievals) where T : ClientObject;
        void ExecuteQuery(ClientContext clientContext);
    }

    public class SharePointService : ISharePointService
    {
        #region Context, Load and Execution
        public virtual ClientContext GetClientContext(string webUrl, string token)
        {
            var clientContext = new ClientContext(webUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + token;
            };

            return clientContext;
        }
        public virtual void Load<T>(ClientContext clientContext, T clientObject, params Expression<Func<T, object>>[] retrievals) where T : ClientObject
        {
            clientContext.Load(clientObject, retrievals);
        }

        public virtual void ExecuteQuery(ClientContext clientContext)
        {
            clientContext.ExecuteQuery();
        }
        #endregion

        #region Site, List and Items        

        public virtual List GetListById(ClientContext clientContext, Guid listId)
        {
            return clientContext.Web.Lists.GetById(listId);
        }

        public virtual ListItemCollection GetListItems(ClientContext clientContext, List list, CamlQuery query)
        {
            return list.GetItems(query);
        }

        public virtual ListItem GetListItemByUniqueId(ClientContext clientContext, List list, Guid uniqueId)
        {
            return list.GetItemByUniqueId(uniqueId);
        }

        public virtual ListItem GetListItemById(ClientContext clientContext, List list, int id)
        {
            return list.GetItemById(id);
        }

        public virtual string GetListItemRetentionLabel(ClientContext clientContext, Guid listId, int id)
        {
            List list = clientContext.Web.Lists.GetById(listId);
            clientContext.Load(list);
            clientContext.ExecuteQuery();

            ListItem item = list.GetItemById(id);
            clientContext.Load(item, i => i.ComplianceInfo);
            clientContext.ExecuteQuery();

            return item.ComplianceInfo.ComplianceTag;
        }

        public virtual bool SetListItemRetentionLabel(ClientContext clientContext, Guid listId, int id, string label)
        {
            bool result = false;
            try
            {
                List list = clientContext.Web.Lists.GetById(listId);
                clientContext.Load(list);
                clientContext.ExecuteQuery();

                ListItem item = list.GetItemById(id);
                clientContext.Load(item, i => i.ComplianceInfo);
                clientContext.ExecuteQuery();

                item.SetComplianceTag(label, false, false, false, false, false);
                item.SystemUpdate();
                clientContext.ExecuteQuery();

                result = true;
            }
            catch (Exception)
            {
                return result;
            }

            return result;
        }

        public virtual bool ValidateItemRetentionLabel(ClientContext siteContext, string listId, string listItemId)
        {
            bool completed = true;
            try
            {
                var retentionLabel = GetListItemRetentionLabel(siteContext, new Guid(listId), Int32.Parse(listItemId));
                if (retentionLabel.Length > 0)
                {
                    Global.Log.LogInformation($"Retention label found: {retentionLabel}. Proceeding to remove before updating dummy document content");

                    // Remove retention label for dummy document before updating it.
                    var updated = SetListItemRetentionLabel(siteContext, new Guid(listId), Int32.Parse(listItemId), "");

                    Global.Log.LogInformation($"Retention label removed. {updated}");
                }
            }
            catch (Exception ex)
            {
                Global.Log.LogCritical(ex, $"Exception trying to validate retention label. ErrorMessage: {ex.Message}");
                completed = false;
            }
            return completed;
        }

        public virtual List<ListItem> GetListAsGenericList(ListItemCollection listItemCollection)
        {
            return listItemCollection.ToList();
        }
        #endregion

        #region Search

        public virtual ResultTable SearchItems(ClientContext clientContext, string queryText)
        {
            var keywordQuery = new KeywordQuery(clientContext)
            {
                QueryText = queryText
            };

            keywordQuery.SelectProperties.Add("Title");
            keywordQuery.SelectProperties.Add("SPSiteURL");
            keywordQuery.SelectProperties.Add("ListID");
            keywordQuery.SelectProperties.Add("ListItemId");
            keywordQuery.SelectProperties.Add("UniqueID");
            keywordQuery.SelectProperties.Add("OriginalPath");
            keywordQuery.TrimDuplicates = false;

            var searchExecutor = new SearchExecutor(clientContext);
            var results = searchExecutor.ExecuteQuery(keywordQuery);

            clientContext.ExecuteQuery();

            var resultTable = results.Value.FirstOrDefault();
            return resultTable;
        }

        public virtual ResultTable SearchItems(ClientContext clientContext, string queryText, int maxQuantity, Guid resultSourceId)
        {
            var keywordQuery = new KeywordQuery(clientContext)
            {
                QueryText = queryText
            };

            keywordQuery.SelectProperties.Add("Title");
            keywordQuery.SelectProperties.Add("SPSiteURL");
            keywordQuery.SelectProperties.Add("SiteId");
            keywordQuery.SelectProperties.Add("WebId");
            keywordQuery.SelectProperties.Add("ListID");
            keywordQuery.SelectProperties.Add("ListItemId");
            keywordQuery.SelectProperties.Add("UniqueID");
            keywordQuery.SelectProperties.Add("OriginalPath");
            keywordQuery.SelectProperties.Add("PhysicalRecord");
            keywordQuery.SelectProperties.Add("PhysicalRecordStatus");
            keywordQuery.SelectProperties.Add("FileExtension");
            keywordQuery.TrimDuplicates = false;
            keywordQuery.RowLimit = maxQuantity;
            keywordQuery.SourceId = resultSourceId;

            var searchExecutor = new SearchExecutor(clientContext);

            var results = searchExecutor.ExecuteQuery(keywordQuery);

            clientContext.ExecuteQuery();

            var resultTable = results.Value.FirstOrDefault();
            return resultTable;
        }

        public virtual ResultTable SearchItems(ClientContext clientContext, string queryText, string[] properties, int maxQuantity, Guid resultSourceId)
        {
            var keywordQuery = new KeywordQuery(clientContext)
            {
                QueryText = queryText
            };

            foreach (string prop in properties)
            {
                keywordQuery.SelectProperties.Add(prop);
            }

            keywordQuery.TrimDuplicates = false;
            keywordQuery.RowLimit = maxQuantity;
            keywordQuery.SourceId = resultSourceId;

            var searchExecutor = new SearchExecutor(clientContext);

            var results = searchExecutor.ExecuteQuery(keywordQuery);

            clientContext.ExecuteQuery();

            var resultTable = results.Value.FirstOrDefault();
            return resultTable;
        }

        #endregion

        #region Taxonomy
        public virtual TaxonomySession GetTaxonomySession(ClientContext clientContext)
        {
            return TaxonomySession.GetTaxonomySession(clientContext);
        }

        public virtual TermStore GetDefaultSiteCollectionTermStore(TaxonomySession taxonomySession)
        {
            return taxonomySession.GetDefaultSiteCollectionTermStore();
        }

        public virtual IQueryable<TermGroup> GetTermStoreGroups(TermStore termStore, string termGroupName)
        {
            return termStore.Groups.Where(g => g.Name.Equals(termGroupName));
        }

        public virtual IQueryable<TermSet> GetTermSets(TermGroup termGroup, string termSetName)
        {
            return termGroup.TermSets.Where(g => g.Name.Equals(termSetName));
        }

        public virtual TermCollection GetTerms(TermSet termSet, LabelMatchInformation label)
        {
            return termSet.GetTerms(label);
        }

        public virtual TermCollection GetAllTerms(TermSet termSet)
        {
            return termSet.GetAllTerms();
        }

        public virtual LabelMatchInformation GetLabelMatchInformation(ClientContext clientContext, string termName)
        {
            return new LabelMatchInformation(clientContext)
            {
                TermLabel = termName,
                TrimUnavailable = false,
            };
        }
        #endregion       
    }
}
