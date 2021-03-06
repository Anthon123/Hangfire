﻿// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Dapper;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.SqlServer.Entities;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Dashboard;
using System.Diagnostics;

namespace Hangfire.SqlServer
{
    internal class SqlServerMonitoringApi : IMonitoringApi
    {
        private readonly SqlServerStorage _storage;
        private readonly int? _jobListLimit;

        public SqlServerMonitoringApi([NotNull] SqlServerStorage storage, int? jobListLimit)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            _storage = storage;
            _jobListLimit = jobListLimit;
        }
               
        public long JobCountByStateName(Dictionary<string, string> parameters)
        {
            var allowedStates = new string[]{
                FailedState.StateName,
                SucceededState.StateName,
                ScheduledState.StateName,
                ProcessingState.StateName,
                DeletedState.StateName };

            if(!allowedStates.Contains(parameters["stateName"])) throw new Exception("JobCountByStateName() method does not support the jobstate: " + parameters["stateName"]);

            return UseConnection(connection =>
                 GetNumberOfJobsByStateName(connection, parameters));
        }
 
        /* 4 parameter option has only been implemented on SqlServer database version! See code in file: "SqlServerJobQueueMonitoringApi.cs" */
        public long EnqueuedCount(string queue, Dictionary<string,string> countParameters)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue, countParameters);

            return counters.EnqueuedCount ?? 0;
        }

        public long FetchedCount(string queue)
        {
            var queueApi = GetQueueApi(queue);
            var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            return counters.FetchedCount ?? 0;
        }
                
        public JobList<ProcessingJobDto> ProcessingJobs(Pager pager)
        {
            return UseConnection(connection => GetJobs(
                connection,
                pager,
                ProcessingState.StateName,
                (sqlJob, job, stateData) => new ProcessingJobDto
                {
                    Job = job,
                    ServerId = stateData.ContainsKey("ServerId") ? stateData["ServerId"] : stateData["ServerName"],
                    StartedAt = JobHelper.DeserializeDateTime(stateData["StartedAt"]),
                }));
        }

        public JobList<ScheduledJobDto> ScheduledJobs(Pager pager)
        {
            return UseConnection(connection => GetJobs(
                connection,
                pager,
                ScheduledState.StateName,
                (sqlJob, job, stateData) => new ScheduledJobDto
                {
                    Job = job,
                    EnqueueAt = JobHelper.DeserializeDateTime(stateData["EnqueueAt"]),
                    ScheduledAt = JobHelper.DeserializeDateTime(stateData["ScheduledAt"])
                }));
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return UseConnection(connection => 
                GetTimelineStats(connection, "succeeded"));
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return UseConnection(connection => 
                GetTimelineStats(connection, "failed"));
        }

        public IList<ServerDto> Servers()
        {
            return UseConnection<IList<ServerDto>>(connection =>
            {
                var servers = connection.Query<Entities.Server>(
                    string.Format(@"select * from [{0}].Server", _storage.GetSchemaName()))
                    .ToList();

                var result = new List<ServerDto>();

                foreach (var server in servers)
                {
                    var data = JobHelper.FromJson<ServerData>(server.Data);
                    result.Add(new ServerDto
                    {
                        Name = server.Id,
                        Heartbeat = server.LastHeartbeat,
                        Queues = data.Queues,
                        StartedAt = data.StartedAt.HasValue ? data.StartedAt.Value : DateTime.MinValue,
                        WorkersCount = data.WorkerCount
                    });
                }

                return result;
            });
        }

        public JobList<FailedJobDto> FailedJobs(Pager pager)
        {
            return UseConnection(connection => GetJobs(
                connection,
                pager,
                FailedState.StateName,
                (sqlJob, job, stateData) => new FailedJobDto
                {
                    Job = job,
                    Reason = sqlJob.StateReason,
                    ExceptionDetails = stateData["ExceptionDetails"],
                    ExceptionMessage = stateData["ExceptionMessage"],
                    ExceptionType = stateData["ExceptionType"],
                    FailedAt = JobHelper.DeserializeNullableDateTime(stateData["FailedAt"])
                }));
        }
        
        public JobList<SucceededJobDto> SucceededJobs(Pager pager)
        {
            return UseConnection(connection => GetJobs(
                connection,
                pager,
                SucceededState.StateName,
                (sqlJob, job, stateData) => new SucceededJobDto
                {
                    Job = job,
                    Result = stateData.ContainsKey("Result") ? stateData["Result"] : null,
                    TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                        ? (long?)long.Parse(stateData["PerformanceDuration"]) + (long?)long.Parse(stateData["Latency"])
                        : null,
                    SucceededAt = JobHelper.DeserializeNullableDateTime(stateData["SucceededAt"])
                }));
        }

        public JobList<DeletedJobDto> DeletedJobs(Pager pager)
        {
            return UseConnection(connection => GetJobs(
                connection,
                pager,
                DeletedState.StateName,
                (sqlJob, job, stateData) => new DeletedJobDto
                {
                    Job = job,
                    DeletedAt = JobHelper.DeserializeNullableDateTime(stateData["DeletedAt"])
                }));
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            var tuples = _storage.QueueProviders
                .Select(x => x.GetJobQueueMonitoringApi())
                .SelectMany(x => x.GetQueues(), (monitoring, queue) => new { Monitoring = monitoring, Queue = queue })
                .OrderBy(x => x.Queue)
                .ToArray();

            var result = new List<QueueWithTopEnqueuedJobsDto>(tuples.Length);

            foreach (var tuple in tuples)
            {
                var enqueuedJobIds = tuple.Monitoring.GetEnqueuedJobIds(tuple.Queue, 0, 5);
                var counters = tuple.Monitoring.GetEnqueuedAndFetchedCount(tuple.Queue);

                var firstJobs = UseConnection(connection => EnqueuedJobs(connection, enqueuedJobIds));

                result.Add(new QueueWithTopEnqueuedJobsDto
                {
                    Name = tuple.Queue,
                    Length = counters.EnqueuedCount ?? 0,
                    Fetched = counters.FetchedCount,
                    FirstJobs = firstJobs
                });
            }

            return result;
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, Pager pager)
        {
            var queueApi = GetQueueApi(queue);
            var enqueuedJobIds = queueApi.GetEnqueuedJobIds(queue, @pager.FromRecord, pager.RecordsPerPage, pager);

            return UseConnection(connection => EnqueuedJobs(connection, enqueuedJobIds));
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            var queueApi = GetQueueApi(queue);
            var fetchedJobIds = queueApi.GetFetchedJobIds(queue, from, perPage);

            return UseConnection(connection => FetchedJobs(connection, fetchedJobIds));
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            return UseConnection(connection => 
                GetHourlyTimelineStats(connection, "succeeded"));
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return UseConnection(connection => 
                GetHourlyTimelineStats(connection, "failed"));
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            return UseConnection(connection =>
            {

                string sql = string.Format(@"
select * from [{0}].Job where Id = @id
select * from [{0}].JobParameter where JobId = @id
select * from [{0}].State where JobId = @id order by Id desc", _storage.GetSchemaName());

                using (var multi = connection.QueryMultiple(sql, new { id = jobId }))
                {
                    var job = multi.Read<SqlJob>().SingleOrDefault();
                    if (job == null) return null;

                    var parameters = multi.Read<JobParameter>().ToDictionary(x => x.Name, x => x.Value);
                    var history =
                        multi.Read<SqlState>()
                            .ToList()
                            .Select(x => new StateHistoryDto
                            {
                                StateName = x.Name,
                                CreatedAt = x.CreatedAt,
                                Reason = x.Reason,
                                Data = new Dictionary<string, string>(
                                    JobHelper.FromJson<Dictionary<string, string>>(x.Data),
                                    StringComparer.OrdinalIgnoreCase),
                            })
                            .ToList();

                    return new JobDetailsDto
                    {
                        CreatedAt = job.CreatedAt,
                        ExpireAt = job.ExpireAt,
                        Job = DeserializeJob(job.InvocationData, job.Arguments),
                        History = history,
                        Properties = parameters
                    };
                }
            });
        }
        
        public StatisticsDto GetStatistics()
        {
            string sql = string.Format(@"
select count(Id) from [{0}].Job where StateName = N'Enqueued';
select count(Id) from [{0}].Job where StateName = N'Failed';
select count(Id) from [{0}].Job where StateName = N'Processing';
select count(Id) from [{0}].Job where StateName = N'Scheduled';
select count(Id) from [{0}].Server;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{0}].Counter where [Key] = N'stats:succeeded'
    union all
    select [Value] from [{0}].AggregatedCounter where [Key] = N'stats:succeeded'
) as s;
select sum(s.[Value]) from (
    select sum([Value]) as [Value] from [{0}].Counter where [Key] = N'stats:deleted'
    union all
    select [Value] from [{0}].AggregatedCounter where [Key] = N'stats:deleted'
) as s;
select count(*) from [{0}].[Set] where [Key] = N'recurring-jobs';
", _storage.GetSchemaName());

            var statistics = UseConnection(connection =>
            {
                var stats = new StatisticsDto();
                using (var multi = connection.QueryMultiple(sql))
                {
                    stats.Enqueued = multi.Read<int>().Single();
                    stats.Failed = multi.Read<int>().Single();
                    stats.Processing = multi.Read<int>().Single();
                    stats.Scheduled = multi.Read<int>().Single();

                    stats.Servers = multi.Read<int>().Single();

                    stats.Succeeded = multi.Read<long?>().SingleOrDefault() ?? 0;
                    stats.Deleted = multi.Read<long?>().SingleOrDefault() ?? 0;

                    stats.Recurring = multi.Read<int>().Single();
                }
                return stats;
            });

            statistics.Queues = _storage.QueueProviders
                .SelectMany(x => x.GetJobQueueMonitoringApi().GetQueues())
                .Count();

            return statistics;
        }

        private Dictionary<DateTime, long> GetHourlyTimelineStats(
            SqlConnection connection,
            string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH")), x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            SqlConnection connection,
            string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();
            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }

            var keyMaps = dates.ToDictionary(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd")), x => x);

            return GetTimelineStats(connection, keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(SqlConnection connection,
            IDictionary<string, DateTime> keyMaps)
        {
            string sqlQuery = string.Format(@"
select [Key], [Value] as [Count] from [{0}].AggregatedCounter
where [Key] in @keys", _storage.GetSchemaName());

            var valuesMap = connection.Query(
                sqlQuery,
                new { keys = keyMaps.Keys })
                .ToDictionary(x => (string)x.Key, x => (long)x.Count);

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
            }

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        private IPersistentJobQueueMonitoringApi GetQueueApi(string queueName)
        {
            var provider = _storage.QueueProviders.GetProvider(queueName);
            var monitoringApi = provider.GetJobQueueMonitoringApi();

            return monitoringApi;
        }

        private T UseConnection<T>(Func<SqlConnection, T> action)
        {
            return _storage.UseTransaction(action, IsolationLevel.ReadUncommitted);
        }

        private JobList<EnqueuedJobDto> EnqueuedJobs(
            SqlConnection connection,
            IEnumerable<int> jobIds)
        {
            string enqueuedJobsSql = string.Format(@"
select j.*, s.Reason as StateReason, s.Data as StateData 
from [{0}].Job j
left join [{0}].State s on s.Id = j.StateId
where j.Id in @jobIds", _storage.GetSchemaName());

            var jobs = connection.Query<SqlJob>(
                enqueuedJobsSql,
                new { jobIds = jobIds })
                .ToList();

            return DeserializeJobs(
                jobs,
                (sqlJob, job, stateData) => new EnqueuedJobDto
                {
                    Job = job,
                    State = sqlJob.StateName,
                    EnqueuedAt = sqlJob.StateName == EnqueuedState.StateName
                        ? JobHelper.DeserializeNullableDateTime(stateData["EnqueuedAt"])
                        : null
                });
        }

        private long GetNumberOfJobsByStateName(SqlConnection connection, Dictionary<string, string> parameters)
        {
            var stateName = parameters["stateName"];
            var filterString = parameters["filterString"];
            var filterMethodString = parameters["filterMethodString"];

            string[] queryParams = new string[]
            {
                  _storage.GetSchemaName(),
                  string.IsNullOrEmpty(filterString) ? string.Empty : " and Arguments LIKE '%'+@filterString+'%' ",
                  string.IsNullOrEmpty(filterMethodString) ? string.Empty : " and InvocationData LIKE '%\"Method\":\"%'+@filterMethodString+'%\",\"ParameterTypes\"%' " ,
                  !hasDateTimeParams(parameters) ? string.Empty : " and @startDateTime <= CreatedAt and CreatedAt <= @endDateTime "
            };

            var sqlFilterDates = prepareSqlFilterDates(parameters);
            var sqlQuery = _jobListLimit.HasValue
                ? string.Format(@"select count(j.Id) from (select top (@limit) Id from [{0}].Job where StateName = @state {1} {2} {3}) as j", queryParams)
                : string.Format(@"select count(Id) from [{0}].Job where StateName = @state {1} {2} {3} ", queryParams);

            var count = connection.Query<int>(
                 sqlQuery,
                 new { state = stateName, limit = _jobListLimit, filterString = filterString, filterMethodString = filterMethodString, startDateTime = sqlFilterDates[0], endDateTime = sqlFilterDates[1] })
                 .Single();

            return count;
        }
        
        private bool hasDateTimeParams(Dictionary<string,string> parameters)
        {
            if (parameters.Count <= 0) return false;

            var startDate = parameters["startDate"];
            var endDate = parameters["endDate"];
            var startTime = parameters["startTime"];
            var endTime = parameters["endTime"];

            return !(string.IsNullOrEmpty(startDate) || 
                string.IsNullOrEmpty(endDate) || 
                string.IsNullOrEmpty(startTime) || 
                string.IsNullOrEmpty(endTime) );
        }

        private string[] prepareSqlFilterDates(Dictionary<string, string> parameters)
        {
            if ( !hasDateTimeParams(parameters) ) return new string[] { string.Empty, string.Empty };

            var startDate = parameters["startDate"];
            var endDate = parameters["endDate"];
            var startTime = parameters["startTime"];
            var endTime = parameters["endTime"];

            string start, end;
            var startDateData = startDate.Split('-');
            var endDateData = endDate.Split('-');
            var startTimeData = startTime.Split('-');
            var endTimeData = endTime.Split('-');
           
            start = new DateTime(int.Parse(startDateData[2]), int.Parse(startDateData[1]), int.Parse(startDateData[0]), int.Parse(startTimeData[0]), int.Parse(startTimeData[1]), 0).ToString("yyyy-MM-dd HH:mm:ss");
            end = new DateTime(int.Parse(endDateData[2]), int.Parse(endDateData[1]), int.Parse(endDateData[0]), int.Parse(endTimeData[0]), int.Parse(endTimeData[1]), 0).ToString("yyyy-MM-dd HH:mm:ss");

            return new string[]{ start, end };
        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = JobHelper.FromJson<InvocationData>(invocationData);
            data.Arguments = arguments;

            try
            {
                return data.Deserialize();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }
        
        private JobList<TDto> GetJobs<TDto>(
           SqlConnection connection,
           Pager pager,
           string stateName,
           Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            Dictionary<string,string> parameters = new Dictionary<string,string>()
            {
                { "startDate", pager.JobsFilterStartDate },
                { "endDate", pager.JobsFilterEndDate },
                { "startTime", pager.JobsFilterStartTime },
                { "endTime", pager.JobsFilterEndTime }
            };

            string[] queryParams = new string[]
            {
                  _storage.GetSchemaName(),
                  string.IsNullOrEmpty(pager.JobsFilterText) ? string.Empty : " and j.Arguments LIKE '%' + @filterString + '%' ",
                  string.IsNullOrEmpty(pager.JobsFilterMethodText) ? string.Empty : " and J.InvocationData LIKE '%\"Method\":\"%'+@filterMethodString+'%\",\"ParameterTypes\"%' "  ,
                  !hasDateTimeParams(parameters) ? string.Empty : " and @startDate <= j.CreatedAt and j.CreatedAt <= @endDate "
            };

            var sqlFilterDates = prepareSqlFilterDates(parameters);
            string jobsSql = string.Format(@"
select * from (
  select j.*, s.Reason as StateReason, s.Data as StateData, row_number() over (order by j.Id desc) as row_num
  from [{0}].Job j with (forceseek)
  left join [{0}].State s on j.StateId = s.Id
  where j.StateName = @stateName {1} {2} {3} 
) as j where j.row_num between @start and @end
", queryParams);

            var jobs = connection.Query<SqlJob>(
                        jobsSql,
                        new { stateName = stateName,
                            start = @pager.FromRecord + 1,
                            end = @pager.FromRecord + pager.RecordsPerPage,
                            filterString = pager.JobsFilterText,
                            filterMethodString = pager.JobsFilterMethodText,
                            startDate = sqlFilterDates[0],
                            endDate = sqlFilterDates[1] })
                            .ToList();
            
            return DeserializeJobs(jobs, selector);
        }
        
        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job, Dictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                var deserializedData = JobHelper.FromJson<Dictionary<string, string>>(job.StateData);
                var stateData = deserializedData != null
                    ? new Dictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                    : null;

                var dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);

                result.Add(new KeyValuePair<string, TDto>(
                    job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        private JobList<FetchedJobDto> FetchedJobs(
            SqlConnection connection,
            IEnumerable<int> jobIds)
        {
            string fetchedJobsSql = string.Format(@"
select j.*, s.Reason as StateReason, s.Data as StateData 
from [{0}].Job j
left join [{0}].State s on s.Id = j.StateId
where j.Id in @jobIds", _storage.GetSchemaName());

            var jobs = connection.Query<SqlJob>(
                fetchedJobsSql,
                new { jobIds = jobIds })
                .ToList();

            var result = new List<KeyValuePair<string, FetchedJobDto>>(jobs.Count);

            foreach (var job in jobs)
            {
                result.Add(new KeyValuePair<string, FetchedJobDto>(
                    job.Id.ToString(),
                    new FetchedJobDto
                    {
                        Job = DeserializeJob(job.InvocationData, job.Arguments),
                        State = job.StateName,
                    }));
            }

            return new JobList<FetchedJobDto>(result);
        }
    }
}
