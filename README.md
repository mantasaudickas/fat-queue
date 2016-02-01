# fat-queue

Simple fire and forget task queueing solution based on database.
Project has to parts:
1. server (for background processing) (class: MsSqlMessengerServer)
2. client (which does task enqueing job) (class: MsSqlMessenger)

Currently supported database is MsSql.
