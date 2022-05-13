using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibBot.Models;

namespace LibBot.Schedular;

class JobsSchedular : IHostedService
{
    public IScheduler Scheduler { get; set; }
    private readonly IJobFactory jobFactory;
    private readonly List<JobMetadata> jobMetadatas;
    private readonly ISchedulerFactory schedulerFactory;

    public JobsSchedular(ISchedulerFactory schedulerFactory, List<JobMetadata> jobMetadatas, IJobFactory jobFactory)
    {
        this.jobFactory = jobFactory;
        this.schedulerFactory = schedulerFactory;
        this.jobMetadatas = jobMetadatas;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Scheduler = await schedulerFactory.GetScheduler();
        Scheduler.JobFactory = jobFactory;

        jobMetadatas?.ForEach(jobMetadata =>
        {
            IJobDetail jobDetail = CreateJob(jobMetadata);

            ITrigger trigger = CreateTrigger(jobMetadata);
   
            Scheduler.ScheduleJob(jobDetail, trigger, cancellationToken).GetAwaiter();
  
        });
        await Scheduler.Start(cancellationToken);
    }

    private ITrigger CreateTrigger(JobMetadata jobMetadata)
    {
        return TriggerBuilder.Create()
            .WithIdentity(jobMetadata.JobId.ToString())
            .WithCronSchedule(jobMetadata.CronExpression)
            .WithDescription(jobMetadata.JobName)
            .Build();
    }

    private IJobDetail CreateJob(JobMetadata jobMetadata)
    {
        return JobBuilder.Create(jobMetadata.JobType)
            .WithIdentity(jobMetadata.JobId.ToString())
            .WithDescription(jobMetadata.JobName)
            .Build();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Scheduler.Shutdown();
    }
}
