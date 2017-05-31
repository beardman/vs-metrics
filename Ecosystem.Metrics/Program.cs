using Ecosystem.Metrics.Domain;
using Ecosystem.Metrics.Helpers;
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
        private const string SUCCESS_STATUS = "succeeded";

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
                    Console.WriteLine($"Retriving builds for definition {definition.Name} of project {vstsProject.Name}");
                    definition.Builds = await GetBuildHistory(vstsProject.Id, definition.Id);
                }

                foreach (var definition in vstsProject.ReleaseDefinitions)
                {
                    Console.WriteLine($"Retriving releases for definition {definition.Name} of project {vstsProject.Name}");
                    definition.Releases = await GetReleaseHistory(vstsProject.Id, definition.Id);
                }

                //foreach (var repo in vstsProject.Repositories)
                //{
                //    repo.Commits = await GetGitCommitHistory(vstsProject.Id, repo.Id);
                //}

                #region Failure rate

                var groupResult = vstsProject.BuildDefinitions[0].Builds
                    .GroupBy(b => b.QueueTime.Date
                            , b => new { Id = b.Id, BuilderNumber = b.BuildNumber, StartTime = b.StartTime, FinishTime = b.FinishTime, Result = b.Result, Status = b.Status }
                            , (key, b) => new FailureRate() { QueueTime = key, FailedCount = b.Count(build => build.Result != SUCCESS_STATUS), SucceededCount = b.Count(build => build.Result == SUCCESS_STATUS), TotalBuilds = b.Count() });

                #endregion

                #region Failure Recovery Time

                // 1) Get all failures and successes separately
                var failures = vstsProject.BuildDefinitions[0].Builds
                                    .Where(b => b.Result != SUCCESS_STATUS)
                                    .Select(b => new FailureRecoveryTime()
                                    {
                                        FailedBuild = b,
                                        SuccessBuild = null
                                    }
                                    )
                                    .OrderBy(b => b.FailedBuild.QueueTime)
                                    .ToList();

                var successes = vstsProject.BuildDefinitions[0].Builds
                                    .Where(b => b.Result == SUCCESS_STATUS)
                                    .OrderBy(b => b.QueueTime)
                                    .ToList();

                // 2) Find next successful build
                foreach (var failure in failures)
                {
                    var nextSuccess = successes.Where(s => s.QueueTime > failure.FailedBuild.QueueTime)
                                               .OrderBy(s => s.QueueTime)
                                               .FirstOrDefault();

                    if (nextSuccess == null)
                    {
                        break;
                    }

                    failure.SuccessBuild = nextSuccess;

                    //Console.WriteLine($"Fail:\tBuild Number: {failure.FailedBuild.BuildNumber}\tTime: {failure.FailedBuild.QueueTime.ToString("yyyy-MM-dd HH:mm:ss")}");
                    //Console.WriteLine($"Corresponding Success:\tBuild Number: {failure.SuccessBuild.BuildNumber}\tTime: {failure.SuccessBuild.QueueTime.ToString("yyyy-MM-dd HH:mm:ss")}\n");
                }

                // 3) Remove failures with same success build
                var buildsBySuccess = failures.GroupBy(f => f.SuccessBuild.Id, f => f, (key, f) => new { SuccessBuildId = key, Failures = f.ToList() });

                List<FailureRecoveryTime> failureRateList = new List<FailureRecoveryTime>();

                foreach (var item in buildsBySuccess)
                {
                    FailureRecoveryTime firstBuildFail = item.Failures.OrderBy(f => f.FailedBuild.QueueTime).First();

                    failureRateList.Add(firstBuildFail);

                    Console.WriteLine($"Failed build {firstBuildFail.FailedBuild.BuildNumber} at {firstBuildFail.FailedBuild.QueueTime.ToString("yyyy-MM-dd HH:mm:ss")} was resolved" +
                                      $" by build {firstBuildFail.SuccessBuild.BuildNumber} which finished at {firstBuildFail.SuccessBuild.FinishTime.ToString("yyyy-MM-dd HH:mm:ss")}\n");
                }

                #endregion

                #region Lead Time

                var leadTimes = vstsProject.BuildDefinitions[0].Builds
                                    .OrderBy(b => b.QueueTime)
                                    .Where(b => b.Result == SUCCESS_STATUS)
                                    .Select(b => new LeadTime()
                                    {
                                        BuildId = b.Id,
                                        BuildNumber = b.BuildNumber,
                                        BuildDuration = ComputeDateDifferenceInSeconds(b.QueueTime, b.FinishTime),
                                        BuildDate = b.QueueTime
                                    }
                                    );

                foreach (var lead in leadTimes)
                {
                    Console.WriteLine($"Lead Time for Build: {lead.BuildNumber}(Id: #{lead.BuildId}) on {lead.BuildDate.ToString("yyyy-MM-dd HH:mm:ss")} was: {lead.BuildDuration}");
                }

                #endregion

                #region Interval

                //var intervals = vstsProject.BuildDefinitions[0].Builds.OrderBy(b => b.QueueTime).Select(b => new Interval() { FirstBuildId = b.Id, FirstBuildNumber = b.BuildNumber, FirstBuildDate = b.QueueTime });

                //foreach (var item in intervals)
                //{
                //    var nextBuild = vstsProject.BuildDefinitions[0].Builds.OrderBy(b => b.QueueTime).Where(b => b.QueueTime > item.FirstBuildDate).FirstOrDefault();

                //    if (nextBuild == null)
                //    {
                //        break;
                //    }

                //    item.NextBuildId = nextBuild.Id;
                //    item.NextBuildNumber = nextBuild.BuildNumber;
                //    item.NextBuildDate = nextBuild.QueueTime;
                //    item.ElapsedTime = ComputeDateDifferenceInSeconds(item.FirstBuildDate, item.NextBuildDate);

                //    Console.WriteLine()
                //}

                #endregion

                Console.WriteLine("Doing stuff");
            }

            //CrunchData();

            Console.WriteLine($"Process finished at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            Console.WriteLine("Process finished. Press enter key to terminate...");
            Console.ReadLine();
        }

        private double ComputeDateDifferenceInSeconds(DateTime start, DateTime end)
        {
            TimeSpan ts = end.Subtract(start);
            return ts.TotalSeconds;
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
