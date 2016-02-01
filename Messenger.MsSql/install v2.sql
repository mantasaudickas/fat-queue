alter table [Messenger].[Queues] drop column [Type]

alter table [Messenger].[Queues] drop column [Priority]

alter table [Messenger].[Queues] drop column [CreateDate]

drop index [Messenger].[Queues].[Ix_Messenger_Queues_ProcessingStarted]

alter table [Messenger].[Queues] drop column [IsFailed]

alter table [Messenger].[Queues] add NextTryTime datetime2(7) null

alter table [Messenger].[Queues] add Retries int null

alter table [Messenger].[Queues] add Error nvarchar(max) null

alter table [Messenger].[Queues] add ProcessName nvarchar(255) null

alter table [Messenger].[Queues] drop column ProcessorId

--
alter table [Messenger].[Messages] drop column [RetryCount]

alter table [Messenger].[Messages] drop column [RecoveryType]

alter table [Messenger].[Messages] drop column [LastErrorInfo]

alter table [Messenger].[Messages] add Context nvarchar(max) null

update [Messenger].[Messages] set Context = ''

alter table [Messenger].[Messages] alter column Context nvarchar(max) not null

drop table [Messenger].[Errors]

delete from [Messenger].[CompletedMessages]

alter table [Messenger].[CompletedMessages] drop column QueueName

alter table [Messenger].[CompletedMessages] add CompletedDate datetime2(7) not null

alter table [Messenger].[CompletedMessages] add Context nvarchar(max) not null

alter table [Messenger].[CompletedMessages] drop column [LastErrorInfo]

drop index [Messenger].[Messages].[Ix_Messenger_Messages_MessageId]

drop index [Messenger].[Messages].[Ix_Messenger_Messages_QueueId]

drop index [Messenger].[Queues].[Ix_Messenger_Queues_Name]

alter table [Messenger].[Messages] DROP CONSTRAINT [FK_Messenger_Messages_Messenger_Queues]

alter table [Messenger].[Queues] DROP CONSTRAINT [PK_Messenger_Queues]

alter table [Messenger].[Queues] ADD CONSTRAINT [PK_Messenger_Queues] PRIMARY KEY CLUSTERED ([QueueId] ASC)

alter table [Messenger].[Messages]  WITH CHECK ADD CONSTRAINT [FK_Messenger_Messages_Messenger_Queues] FOREIGN KEY([QueueId]) REFERENCES [Messenger].[Queues] ([QueueId])

alter table [Messenger].[Messages] CHECK CONSTRAINT [FK_Messenger_Messages_Messenger_Queues]

execute sp_rename N'Messenger.CompletedMessages.CompletedMessagesId', N'CompletedMessageId', 'COLUMN' 


---------------
drop index Messenger.Queues.Ix_Messenger_Queues_Lock

create index Ix_Messenger_Queues_Lock on Messenger.Queues
(
	ProcessingStarted,
	NextTryTime,
	QueueId
)

drop index Messenger.Messages.Ix_Messenger_Messages_Lock

create index Ix_Messenger_Messages_Lock on Messenger.Messages
(
	QueueId
) include (CreateDate)

drop index Messenger.Messages.Ix_Messenger_Messages_Process

create index Ix_Messenger_Messages_Process on Messenger.Messages 
(
	QueueId
) include (MessageId,Content,Context,CreateDate)

----------

delete from Messenger.FailedMessages

alter table Messenger.FailedMessages drop column QueueName

alter table Messenger.FailedMessages drop column LastErrorInfo

alter table Messenger.FailedMessages add Error nvarchar(max) null

alter table Messenger.FailedMessages add Context nvarchar(max) null

alter table Messenger.FailedMessages add FailedDate datetime2(7) not null

drop table Messenger.[Statistics]

-------

create table [Messenger].[Heartbeat]
(
	HeartbeatId int not null identity(1,1),
	ProcessName nvarchar(255) not null,
	LastBeat datetime2(7) not null
)

ALTER TABLE [Messenger].[Heartbeat] ADD CONSTRAINT PK_Messenger_Hearbeat PRIMARY KEY CLUSTERED (HeartbeatId) ON [PRIMARY]
