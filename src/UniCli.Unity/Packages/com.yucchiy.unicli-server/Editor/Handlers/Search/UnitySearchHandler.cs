using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.Search;

namespace UniCli.Server.Editor.Handlers.Search
{
    [Module("Search")]
    public sealed class UnitySearchHandler : CommandHandler<UnitySearchRequest, UnitySearchResponse>
    {
        public override string CommandName => "Search";
        public override string Description => "Search Unity project using Unity Search API";

        protected override ValueTask<UnitySearchResponse> ExecuteAsync(UnitySearchRequest request, CancellationToken cancellationToken)
        {
            var flags = SearchFlags.Synchronous;
            if (request.includePackages)
                flags |= SearchFlags.Packages;

            var providers = string.IsNullOrEmpty(request.provider) ? null : new[] { request.provider };

            using var context = SearchService.CreateContext(providers, request.query, flags);
            var searchItems = SearchService.GetItems(context);

            var results = new List<SearchResultItem>();
            var maxResults = request.maxResults > 0 ? request.maxResults : 50;

            foreach (var item in searchItems)
            {
                if (results.Count >= maxResults)
                    break;

                results.Add(new SearchResultItem
                {
                    id = item.id ?? "",
                    label = item.GetLabel(context, true) ?? "",
                    description = item.GetDescription(context, true) ?? "",
                    provider = item.provider?.id ?? ""
                });
            }

            return new ValueTask<UnitySearchResponse>(new UnitySearchResponse
            {
                results = results.ToArray(),
                totalCount = searchItems.Count,
                displayedCount = results.Count,
                query = request.query
            });
        }
    }

    [Serializable]
    public class UnitySearchRequest
    {
        public string query = "";
        public string provider = "";
        public int maxResults = 50;
        public bool includePackages;
    }

    [Serializable]
    public class UnitySearchResponse
    {
        public SearchResultItem[] results;
        public int totalCount;
        public int displayedCount;
        public string query;
    }

    [Serializable]
    public class SearchResultItem
    {
        public string id;
        public string label;
        public string description;
        public string provider;
    }
}
