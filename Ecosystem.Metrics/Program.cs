using Ecosystem.Metrics.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Ecosystem.Metrics
{
    public class Program
    {
        private const string ACCOUNT = "teamplay";
        private const string ACCOUNT_INSTANCE = "teamplay.visualstudio.com";                                                                                                                                

        static void Main(string[] args)
        {
            Program metricsApp = new Program();
            metricsApp.GetMetrics().Wait();
        }

        public async Task GetMetrics()
        {
            Console.WriteLine($"Process started at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            List<Project> vstsProjects = await GetProjectList();

            foreach (var vstsProject in vstsProjects)
            {
                vstsProject.BuildDefinitions = await GetBuildDefinitionList(vstsProject.Id);
                vstsProject.ReleaseDefinitions = await GetReleaseDefinitionList(vstsProject.Id);
                vstsProject.Repositories = await GetGitRepositoryList(vstsProject.Id);

                foreach (var definition in vstsProject.BuildDefinitions)
                {
                    definition.Builds = await GetBuildHistory(vstsProject.Id, definition.Id);
                }

                foreach (var definition in vstsProject.ReleaseDefinitions)
                {
                    definition.Releases = await GetReleaseHistory(vstsProject.Id, definition.Id);
                }

                foreach (var repo in vstsProject.Repositories)
                {
                    repo.Commits = await GetGitCommitHistory(vstsProject.Id, repo.Id);
                }
            }

            //CrunchData();

            Console.WriteLine($"Process finished at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            Console.WriteLine("Process finished. Press enter key to terminate...");
            Console.ReadLine();
        }

        private void CrunchData(List<Project> vstsProjects)
        {
            /**
             * For deployment and build:
             * - Failure Rate
             * - Failure Recovery Time
             * - Lead Time
             * - Interval
             * 
             * For commits:
             * - commit interval
             **/
            foreach (var project in vstsProjects)
            {
                foreach (var buildDef in project.BuildDefinitions)
                {
                    int totalBuildsForDefinition = buildDef.Builds.Count;
                    //int totalFailedBuilds = buildDef.Builds.GroupBy(d => d.QueueTime.ToString("yyyy-MM-dd")).ToDictionary();
                }
            }
        }

        private string GetBase64AuthString()
        {
            return Convert.ToBase64String(
                Encoding.ASCII.GetBytes(
                    $"{App.Default.Username}:{App.Default.Token}"));

        }

        private async Task<List<Project>> GetProjectList()
        {
            string url = $"https://{ACCOUNT_INSTANCE}/DefaultCollection/_apis/projects?api-version=2.0";

            return await GetData<Project>(url);
        }

        private async Task<List<BuildDefinition>> GetBuildDefinitionList(string projectId)
        {
            string url = $"https://{ACCOUNT_INSTANCE}/DefaultCollection/{projectId}/_apis/build/definitions?api-version=2.0";

            return await GetData<BuildDefinition>(url);
        }

        private async Task<List<Build>> GetBuildHistory(string projectId, int buildDefinitionId)
        {
            string url = $"https://{ACCOUNT_INSTANCE}/DefaultCollection/{projectId}/_apis/build/builds?api-version=2.0&definitions={buildDefinitionId}";

            return await GetData<Build>(url);
        }

        private async Task<List<ReleaseDefinition>> GetReleaseDefinitionList(string projectId)
        {
            string url = $"https://{ACCOUNT}.vsrm.visualstudio.com/defaultcollection/{projectId}/_apis/release/definitions?api-version=3.0-preview.1";

            List<ReleaseDefinition> result = await GetData<ReleaseDefinition>(url);

            return result;
        }

        private async Task<List<BaseEntity>> GetReleaseIds(string projectId, int definitionId)
        {
            string url = $"https://{ACCOUNT}.vsrm.visualstudio.com/defaultcollection/{projectId}/_apis/release/releases?api-version=3.0-preview.2&definitionId={definitionId}";

            List<BaseEntity> result = await GetData<BaseEntity>(url);

            return result;
        }

        private async Task<List<Release>> GetReleaseHistory(string projectId, int definitionId)
        {
            var ids = await GetReleaseIds(projectId, definitionId);

            List<Release> releases = new List<Release>();

            foreach (var item in ids)
            {
                string url = $"https://{ACCOUNT}.vsrm.visualstudio.com/defaultcollection/{projectId}/_apis/release/releases/{item.Id}?api-version=3.0-preview.2";

                Tuple<string, string> response = await MakeHttpCall(url, string.Empty);

                Release release = JsonConvert.DeserializeObject<Release>(response.Item1);

                releases.Add(release);
            }

            return releases;
        }

        private async Task<List<Repository>> GetGitRepositoryList(string projectId)
        {
            string url = $"https://{ACCOUNT_INSTANCE}/DefaultCollection/{projectId}/_apis/git/repositories?api-version=1.0";

            return await GetData<Repository>(url);
        }

        private async Task<List<Commit>> GetGitCommitHistory(string projectId, string repositoryId)
        {
            string url = $"https://{ACCOUNT_INSTANCE}/DefaultCollection/{projectId}/_apis/git/repositories/{repositoryId}/commits?api-version=1.0&branch=master";

            return await GetPagedData<Commit>(url);
        }

        private string GetContinuationToken(HttpResponseHeaders headers)
        {
            IEnumerable<string> values;
            string token = null;

            if (headers.TryGetValues("x-ms-continuationtoken", out values))
            {
                token = values.FirstOrDefault();
            }

            return token ?? string.Empty;
        }

        private async Task<List<T>> GetData<T>(string baseUrl)
        {

            string continuationToken = string.Empty;
            List<T> entities = new List<T>();

            do
            {
                Tuple<string, string> result = await MakeHttpCall(baseUrl, continuationToken);
                BaseResponse<T> temp = JsonConvert.DeserializeObject<BaseResponse<T>>(result.Item1);

                entities.AddRange(temp.Value);

                continuationToken = result.Item2;
            }
            while (!string.IsNullOrEmpty(continuationToken));

            Console.WriteLine($"Found {entities.Count} items");

            return entities;
        }

        private async Task<List<T>> GetPagedData<T>(string baseUrl)
        {
            int numberOfResults = 100;
            int skip = 0;
            List<T> entities = new List<T>();
            bool done = false;

            do
            {
                string requestUrl = string.Concat(baseUrl, $"&$skip={skip}&$top={numberOfResults}");
                Tuple<string, string> result = await MakeHttpCall(requestUrl);
                BaseResponse<T> temp = JsonConvert.DeserializeObject<BaseResponse<T>>(result.Item1);

                skip += numberOfResults;
                entities.AddRange(temp.Value);
                done = (temp.Count == 0);
            }
            while (!done);

            Console.WriteLine($"Found {entities.Count} items");

            return entities;
        }

        private async Task<Tuple<string, string>> MakeHttpCall(string requestUrl, string continuationToken = null)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GetBase64AuthString());

                    if (!string.IsNullOrEmpty(continuationToken))
                    {
                        requestUrl = string.Concat(requestUrl, $"&continuationtoken={continuationToken}");
                    }

                    using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        string token = GetContinuationToken(response.Headers);

                        return new Tuple<string, string>(responseBody, token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new Tuple<string, string>(string.Empty, string.Empty);
            }
        }
    }
}
