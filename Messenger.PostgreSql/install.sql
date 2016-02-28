CREATE SCHEMA IF NOT EXISTS messenger;

CREATE TABLE Messenger.CompletedMessages(
	CompletedMessageId SERIAL NOT NULL,
	ContentType varchar(255) NOT NULL,
	Content text NOT NULL,
	CreateDate timestamptz NOT NULL,
	CompletedDate timestamptz NOT NULL,
	Context text NOT NULL,
	Identity uuid NULL,
	CONSTRAINT PK_Messenger_CompleteMessages PRIMARY KEY (CompletedMessageId)
);

CREATE TABLE Messenger.FailedMessages(
	FailedMessageId SERIAL NOT NULL,
	ContentType varchar(255) NOT NULL,
	Content text NOT NULL,
	CreateDate timestamptz NOT NULL,
	Error text NULL,
	Context text NULL,
	FailedDate timestamptz NOT NULL,
	Identity uuid NULL,
	CONSTRAINT PK_Messenger_FailedMessages PRIMARY KEY (FailedMessageId)
);

CREATE TABLE Messenger.Heartbeat(
	HeartbeatId SERIAL NOT NULL,
	ProcessName varchar(255) NOT NULL,
	LastBeat timestamptz NOT NULL,
	CONSTRAINT PK_Messenger_Hearbeat PRIMARY KEY (HeartbeatId)
);

CREATE TABLE Messenger.Messages(
	MessageId SERIAL NOT NULL,
	QueueId int4 NOT NULL,
	ContentType varchar(255) NOT NULL,
	Content text NOT NULL,
	StartDate timestamptz NOT NULL,
	Context text NULL,
	Identity uuid NULL,
	CONSTRAINT PK_Messenger_Messages PRIMARY KEY (MessageId)
);

CREATE TABLE Messenger.Queues(
	QueueId SERIAL NOT NULL,
	Name varchar(255) NOT NULL,
	ProcessingStarted timestamptz NULL,
	NextTryTime timestamptz NULL,
	Retries int4 NULL,
	Error text NULL,
	ProcessName varchar(255) NULL,
	ProcessedAt timestamptz NULL,
	CONSTRAINT PK_Messenger_Queues PRIMARY KEY (QueueId)
);

ALTER TABLE Messenger.Messages
ADD CONSTRAINT FK_Messenger_Messages_Messenger_Queues FOREIGN KEY (QueueId)
REFERENCES Messenger.Queues (QueueId);

CREATE INDEX Ix_Messenger_Messages_Lock 
ON Messenger.Messages (QueueId, StartDate);

CREATE INDEX Ix_Messenger_Messages_Process 
ON Messenger.Messages (QueueId, MessageId, StartDate);

CREATE UNIQUE INDEX UC_Messenger_Messages_Identity 
ON Messenger.Messages (Identity) WHERE (Identity IS NOT NULL);

CREATE UNIQUE INDEX UC_Messenger_Heartbeat_Process
ON Messenger.Heartbeat (ProcessName);

CREATE INDEX Ix_Messenger_Queues_Lock 
ON Messenger.Queues (ProcessingStarted, NextTryTime, QueueId);
