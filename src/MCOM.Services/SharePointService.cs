using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        ResultTable SearchItems(ClientContext clientContext, string queryText);
        ResultTable SearchItems(ClientContext clientContext, string queryText, int maxQuantity);
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
            keywordQuery.SelectProperties.Add("UniqueID");
            keywordQuery.SelectProperties.Add("OriginalPath");
            keywordQuery.TrimDuplicates = false;

            var searchExecutor = new SearchExecutor(clientContext);
            var results = searchExecutor.ExecuteQuery(keywordQuery);

            clientContext.ExecuteQuery();

            var resultTable = results.Value.FirstOrDefault();
            return resultTable;
        }

        public virtual ResultTable SearchItems(ClientContext clientContext, string queryText, int maxQuantity)
        {
            var keywordQuery = new KeywordQuery(clientContext)
            {
                QueryText = queryText
            };

            keywordQuery.SelectProperties.Add("Title");
            keywordQuery.SelectProperties.Add("SPSiteURL");
            keywordQuery.SelectProperties.Add("ListID");
            keywordQuery.SelectProperties.Add("UniqueID");
            keywordQuery.SelectProperties.Add("OriginalPath");
            keywordQuery.TrimDuplicates = false;
            keywordQuery.RowLimit = maxQuantity;

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
