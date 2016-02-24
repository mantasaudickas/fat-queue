begin transaction
set transaction isolation level read uncommitted
select q.QueueId, q.Name, q.ProcessedAt, q.ProcessingStarted, q.ProcessName, q.Retries, q.NextTryTime, q.Error
, sum(case when m.MessageId is null then 0 else 1 end) as MessageCount
, (select top 1 StartDate from Messenger.Messages mm where mm.QueueId = q.QueueId order by StartDate asc) as StartDate
from Messenger.Queues q
left join Messenger.Messages m on m.QueueId = q.QueueId
group by q.QueueId, q.Name, q.ProcessedAt, q.ProcessingStarted, q.ProcessName, q.Retries, q.NextTryTime, q.Error
order by q.ProcessedAt desc

--select * from Messenger.Heartbeat

select 'Failed' as State, count(*) as MessageCount from Messenger.FailedMessages
union
select 'Completed', count(*) from Messenger.CompletedMessages
union
select 'Ready', count(*) from Messenger.Messages

rollback

/*
truncate table Messenger.Messages
truncate table Messenger.FailedMessages
truncate table Messenger.CompletedMessages

update Messenger.Queues
set ProcessingStarted = null,
ProcessName = null,
Retries = null,
Error = null,
NextTryTime = null

select * from Messenger.FailedMessages order by CreateDate asc




delete from Messenger.Messages
delete from Messenger.FailedMessages
delete from Messenger.CompletedMessages
delete from Messenger.Queues
delete from Messenger.Heartbeat


select  * from Messenger.Messages order by StartDate asc

select  top 20 * from Messenger.Messages
where QueueId = 31
order by CreateDate desc
*/

/*
select * from Messenger.Queues
select * from Messenger.FailedMessages
select * from Messenger.CompletedMessages order by CreateDate
select * from Messenger.Messages

select * from Messenger.CompletedMessages
where CreateDate > CompletedDate


begin tran
insert into Messenger.Messages (QueueId, ContentType, Content, CreateDate, Context)
select 1, ContentType, Content, CreateDate, Context
from Messenger.FailedMessages

delete from Messenger.FailedMessages
commit
*/


-- dbcc sqlperf (logspace)
