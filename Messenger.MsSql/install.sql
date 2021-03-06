IF NOT EXISTS (SELECT SCHEMA_ID FROM sys.schemas WHERE [name] = 'Messenger')
BEGIN
    PRINT 'Version1'
    EXEC ('CREATE SCHEMA [Messenger]') /* AUTHORIZATION owner_name */
END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Messenger' AND  TABLE_NAME = 'Queues'))
BEGIN
    PRINT 'Version2'
    CREATE TABLE Messenger.Queues
        (
        QueueId int NOT NULL IDENTITY (1, 1),
        Name nvarchar(255) NOT NULL,
        Priority int NOT NULL,
        Type smallint NOT NULL,
        CreateDate datetime2(7) NOT NULL,
        ProcessingStarted datetime2(7) NULL,
        ProcessorId uniqueidentifier NULL,
        IsFailed bit NOT NULL
        )  ON [PRIMARY]


    ALTER TABLE Messenger.Queues ADD CONSTRAINT
        PK_Messenger_Queues PRIMARY KEY NONCLUSTERED 
        (
            QueueId
        ) ON [PRIMARY]



    CREATE TABLE Messenger.Messages
        (
        MessageId int NOT NULL IDENTITY (1, 1),
        QueueId int NOT NULL,
        ContentType nvarchar(255) NOT NULL,
        [Content] nvarchar(MAX) NOT NULL,
        CreateDate datetime2(7) NOT NULL,
        RetryCount int NOT NULL
        )  ON [PRIMARY]


    ALTER TABLE Messenger.Messages ADD CONSTRAINT
        PK_Messenger_Messages PRIMARY KEY NONCLUSTERED 
        (
            MessageId
        ) ON [PRIMARY]



    ALTER TABLE Messenger.Messages ADD CONSTRAINT
        FK_Messenger_Messages_Messenger_Queues FOREIGN KEY
        (
            QueueId
        ) REFERENCES Messenger.Queues
        (
            QueueId
        ) ON UPDATE  NO ACTION 
         ON DELETE  NO ACTION 
    


    CREATE TABLE Messenger.Errors
        (
            ErrorId int NOT NULL IDENTITY(1,1),
            QueueId int NOT NULL,
            Content nvarchar(MAX) NOT NULL,
            CreateDate datetime2(7) NOT NULL
        )


    ALTER TABLE Messenger.Errors ADD CONSTRAINT
        PK_Messenger_Errors PRIMARY KEY NONCLUSTERED 
        (
            ErrorId
        ) ON [PRIMARY]



    ALTER TABLE Messenger.Errors ADD CONSTRAINT
        FK_Messenger_Errors_Messenger_Queues FOREIGN KEY
        (
            QueueId
        ) REFERENCES Messenger.Queues
        (
            QueueId
        ) ON UPDATE  NO ACTION 
         ON DELETE  NO ACTION 
    


    CREATE UNIQUE CLUSTERED INDEX Ix_Messenger_Queues_Name ON [Messenger].[Queues]
    (
        [Name] ASC
    ) WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]


    CREATE NONCLUSTERED INDEX [Ix_Messenger_Queues_ProcessingStarted] ON [Messenger].[Queues] 
    (
        [ProcessingStarted]
    ) INCLUDE ([QueueId], [Priority], [IsFailed])
    WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]


    CREATE CLUSTERED INDEX [Ix_Messenger_Messages_MessageId] ON [Messenger].[Messages] 
    (
        [MessageId],
        [QueueId],
        [CreateDate]
    ) WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Messenger' AND  TABLE_NAME = 'FailedMessages'))
BEGIN
    PRINT 'Version3'

    ALTER TABLE [Messenger].[Messages]
        ADD [RecoveryType] int NULL

    ALTER TABLE [Messenger].[Messages]
        ADD [LastErrorInfo] nvarchar(max) NULL

    CREATE TABLE [Messenger].[FailedMessages]
        (
            [FailedMessageId] int NOT NULL IDENTITY(1,1),
            [QueueName] nvarchar(255) NOT NULL,
            [ContentType] nvarchar(255) NOT NULL,
            [Content] nvarchar(MAX) NOT NULL,
            [CreateDate] datetime2(7) NOT NULL,
            [LastErrorInfo] nvarchar(max) NULL
        )

    ALTER TABLE [Messenger].[FailedMessages] ADD CONSTRAINT
        PK_Messenger_FailedMessages PRIMARY KEY CLUSTERED 
        (
            [FailedMessageId]
        ) ON [PRIMARY]

END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Messenger' AND  TABLE_NAME = 'CompletedMessages'))
BEGIN
    PRINT 'Version5'

    CREATE TABLE [Messenger].[CompletedMessages]
        (
            [CompletedMessagesId] int NOT NULL IDENTITY(1,1),
            [QueueName] nvarchar(255) NOT NULL,
            [ContentType] nvarchar(255) NOT NULL,
            [Content] nvarchar(MAX) NOT NULL,
            [CreateDate] datetime2(7) NOT NULL
        )

    ALTER TABLE [Messenger].[CompletedMessages] ADD CONSTRAINT
        PK_Messenger_CompleteMessages PRIMARY KEY CLUSTERED 
        (
            [CompletedMessagesId]
        ) ON [PRIMARY]
END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Messenger' AND  TABLE_NAME = 'Heartbeat'))
BEGIN
    PRINT 'Version6'

    drop index [Messenger].[Queues].[Ix_Messenger_Queues_ProcessingStarted]

    alter table [Messenger].[Queues] drop column [Type]

    alter table [Messenger].[Queues] drop column [Priority]

    alter table [Messenger].[Queues] drop column [CreateDate]

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

    drop table [Messenger].[Errors]

    delete from [Messenger].[CompletedMessages]

    alter table [Messenger].[CompletedMessages] drop column QueueName

    alter table [Messenger].[CompletedMessages] add CompletedDate datetime2(7) not null

    alter table [Messenger].[CompletedMessages] add Context nvarchar(max) not null

    if  exists (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Messenger].[Messages]') AND name = N'Ix_Messenger_Messages_MessageId')
    drop index [Messenger].[Messages].[Ix_Messenger_Messages_MessageId]

    if  exists (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Messenger].[Messages]') AND name = N'Ix_Messenger_Messages_QueueId')
    drop index [Messenger].[Messages].[Ix_Messenger_Messages_QueueId]

    if  exists (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Messenger].[Queues]') AND name = N'Ix_Messenger_Queues_Name')
    drop index [Messenger].[Queues].[Ix_Messenger_Queues_Name]

    alter table [Messenger].[Messages] DROP CONSTRAINT [FK_Messenger_Messages_Messenger_Queues]

    alter table [Messenger].[Queues] DROP CONSTRAINT [PK_Messenger_Queues]

    alter table [Messenger].[Queues] ADD CONSTRAINT [PK_Messenger_Queues] PRIMARY KEY CLUSTERED ([QueueId] ASC)

    alter table [Messenger].[Messages]  WITH CHECK ADD CONSTRAINT [FK_Messenger_Messages_Messenger_Queues] FOREIGN KEY([QueueId]) REFERENCES [Messenger].[Queues] ([QueueId])

    alter table [Messenger].[Messages] CHECK CONSTRAINT [FK_Messenger_Messages_Messenger_Queues]

    execute sp_rename N'Messenger.CompletedMessages.CompletedMessagesId', N'CompletedMessageId', 'COLUMN' 


    ---------------
    create index Ix_Messenger_Queues_Lock on Messenger.Queues
    (
        ProcessingStarted,
        NextTryTime,
        QueueId
    )

    create index Ix_Messenger_Messages_Lock on Messenger.Messages
    (
        QueueId
    ) include (CreateDate)

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
    
    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Messenger' AND  TABLE_NAME = 'Statistics')) drop table Messenger.[Statistics]

    -------

    create table [Messenger].[Heartbeat]
    (
        HeartbeatId int not null identity(1,1),
        ProcessName nvarchar(255) not null,
        LastBeat datetime2(7) not null
    )

    ALTER TABLE [Messenger].[Heartbeat] ADD CONSTRAINT PK_Messenger_Hearbeat PRIMARY KEY CLUSTERED (HeartbeatId) ON [PRIMARY]

END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Messenger' AND TABLE_NAME = 'Queues' AND COLUMN_NAME = 'ProcessedAt'))
BEGIN
    PRINT 'Version7'

    ALTER TABLE [Messenger].[Messages] DROP CONSTRAINT [PK_Messenger_Messages]

    ALTER TABLE Messenger.Messages ADD CONSTRAINT
        PK_Messenger_Messages PRIMARY KEY CLUSTERED 
        (
            MessageId
        ) ON [PRIMARY]
        
    ALTER TABLE Messenger.Queues ADD ProcessedAt datetime2(7) NULL
    
END
GO

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Messenger' AND TABLE_NAME = 'Messages' AND COLUMN_NAME = 'StartDate'))
BEGIN
    PRINT 'Version8'

    EXEC sp_rename 'Messenger.Messages.CreateDate', 'StartDate', 'COLUMN';
    
    ALTER TABLE Messenger.Messages ADD [Identity] uniqueidentifier NULL;
    
    ALTER TABLE Messenger.FailedMessages ADD [Identity] uniqueidentifier NULL;
    
    ALTER TABLE Messenger.CompletedMessages ADD [Identity] uniqueidentifier NULL;
END
GO

IF (NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Messenger].[Messages]') AND name = N'UC_Messenger_Messages_Identity'))
BEGIN
    PRINT 'Version9'

    CREATE UNIQUE NONCLUSTERED INDEX UC_Messenger_Messages_Identity ON [Messenger].[Messages] 
    (
        [Identity]
    ) WHERE [Identity] IS NOT NULL
    ON [PRIMARY]
END
GO

IF (SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Messenger' AND TABLE_NAME = 'Messages' AND COLUMN_NAME = 'Identity') = 'YES'
BEGIN
    PRINT 'Version10'

    DROP INDEX [UC_Messenger_Messages_Identity] ON [Messenger].[Messages];

    ALTER TABLE Messenger.Messages ADD CONSTRAINT DF_Messenger_Messages_Identity DEFAULT NEWID() FOR [Identity];
    UPDATE Messenger.Messages SET [Identity] = NEWID() WHERE [Identity] is NULL;
    ALTER TABLE Messenger.Messages ALTER COLUMN [Identity] uniqueidentifier NOT NULL;
        
    ALTER TABLE Messenger.CompletedMessages ADD CONSTRAINT DF_Messenger_CompletedMessages_Identity DEFAULT NEWID() FOR [Identity];
    UPDATE Messenger.CompletedMessages SET [Identity] = NEWID() WHERE [Identity] is NULL;
    ALTER TABLE Messenger.CompletedMessages ALTER COLUMN [Identity] uniqueidentifier NOT NULL;

    ALTER TABLE Messenger.FailedMessages ADD CONSTRAINT DF_Messenger_FailedMessages_Identity DEFAULT NEWID() FOR [Identity];
    UPDATE Messenger.FailedMessages SET [Identity] = NEWID() WHERE [Identity] is NULL;
    ALTER TABLE Messenger.FailedMessages ALTER COLUMN [Identity] uniqueidentifier NOT NULL;

    CREATE UNIQUE NONCLUSTERED INDEX UC_Messenger_Messages_Identity ON [Messenger].[Messages] 
    (
        [Identity]
    )
    ON [PRIMARY] 
END

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Messenger' AND TABLE_NAME = 'Messages' AND COLUMN_NAME = 'ContextFactory'))
BEGIN
    PRINT 'Version11'
    
    ALTER TABLE Messenger.Messages ADD [ContextFactory] nvarchar(max) NULL;
    ALTER TABLE Messenger.CompletedMessages ADD [ContextFactory] nvarchar(max) NULL;
    ALTER TABLE Messenger.FailedMessages ADD [ContextFactory] nvarchar(max) NULL;
END

IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Messenger' AND TABLE_NAME = 'Messages' AND COLUMN_NAME = 'Error'))
BEGIN
    PRINT 'Version12'
    
    ALTER TABLE Messenger.Messages ADD [Error] nvarchar(max) NULL;
    ALTER TABLE Messenger.FailedMessages ADD [QueueId] int NULL;
    
    ALTER TABLE Messenger.FailedMessages ADD CONSTRAINT
        FK_Messenger_FailedMessages_Messenger_Queues FOREIGN KEY
        (
            QueueId
        ) REFERENCES Messenger.Queues
        (
            QueueId
        ) ON UPDATE  NO ACTION 
         ON DELETE  NO ACTION 
    
END

IF exists(SELECT * FROM sys.indexes WHERE name='IX_StartDate' AND object_id = OBJECT_ID('[Messenger].[Messages]'))
    DROP INDEX IX_StartDate ON [Messenger].[Messages]

CREATE NONCLUSTERED INDEX IX_StartDate ON [Messenger].[Messages]
(
    [StartDate] ASC, 
    [QueueId]
)

IF exists(SELECT * FROM sys.indexes WHERE name='Ix_Messenger_Queues_Lock' AND object_id = OBJECT_ID('[Messenger].[Queues]'))
    DROP INDEX [Ix_Messenger_Queues_Lock] ON [Messenger].[Queues]

CREATE NONCLUSTERED INDEX [Ix_Messenger_Queues_Lock] ON [Messenger].[Queues]
(
    [ProcessingStarted] ASC,
    [NextTryTime] ASC
)

