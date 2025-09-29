# Channel members monitoring Telegram-bot

This solution provides a mechanism for monitoring changes in the composition of entities anywhere. It can be adapted to work with collections of any type and integrated into any infrastructure. 

## Implementation Overview:
This solution is designed as a Telegram bot that:
- Monitors subscribers of a specified Telegram channel
- Detects and notifies about subscription changes (new joins or exits)
- Provides statistical and historical data on subscribers and related events
- Leverages Event Sourcing to maintain a complete audit trail of all changes

Thus, the bot’s core performs two main tasks:

1. Monitors and broadcasts notifications about changes in the subscriber list.
2. Processes user requests.

## Key Features

- Real-time tracking of channel membership changes
- Event-driven architecture with full history preservation
- Subscription analytics and historical event logging

## High-level Architecture
Very high-level architecture of solution is represented below.
![Архитектура](./docs/scheme-main.drawio.svg)

The **Domain** layer is conceptually represented as a mechanism for generating set difference based on changes in entity composition (additions or removals).

**Infrastructure** of the application depends on the Telegram API, provided by [WTelegramClient](https://wiz0u.github.io/WTelegramClient/). The interaction with the API is handled by an infrastructure fetching users background service that periodically requests the subscriber list. 

The database consists of:
- event store,
- snapshots store
- channel subscribers read model,
- users from API read model
- bot users. 

Detailed database model is represented [below](#current-database-model)

Data access is implemented via repositories.

The **Application** layer is responsible for processing domain events, executing pure commands and queries, as well as part of the bot’s core monitoring logic. It is also responsible for snapshot collecting background job.

The **Presentation** layer contains the bot’s core logic and the mechanism for generating user-facing messages and handling bot users actions. Bot API is provided by [Telegram.BotAPI for .NET](https://github.com/Eptagone/Telegram.BotAPI).

### Current database model


```mermaid

classDiagram

	class UserReadModel{
		long Id
		string? NickName
		bool IsBot
		string? FirstName
		string? LastName
		string? Phone
		string? ChannelReference
		string? LastAction
		DateTime? TimeStamp
		DateTime? Created
	}
	
	class EventStorage{
		Guid Id
		int CurrentTimeSequenceNumber
		string AggregateNameProjection
		string EntityType
		string EventType
		string Data
		DateTime TimeStamp
		string EntityIdProjection
		string EntityNameProjection
	}
```

```mermaid
classDiagram
	class AggregateSnapshot{
		Guid Id
		string AggregateName
		long? TotalEntities
		Guid? LastProcessedEventId
		DateTime? LastEventTimeStamp
		int? LastEventSequenceNumberForTimeStamp
		DateTime? TimeStamp
	}
	
	class APIUserReadModel{
		long Id
		string? NickName
		bool IsBot
		string? FirstName
		string? LastName
		string? Phone
		string? ChannelReference
		DateTime? Joined
		DateTime? TimeStamp
		bool IsCurrent
	}

```

```mermaid
classDiagram
	class BotUser{
		required long Id
		string? FirstName
		string? LastName
		string? UserName
		bool? IsForum
		string? Type
		string? Title
		bool IsCurrent
		DateTime TimeStamp
	}
```


## Subscriber Change Processing Flow

The bot's core executes the following sequence when handling channel membership changes:

```mermaid

sequenceDiagram
actor User

participant Presentation as Presentation

participant Application as Application

participant Infrastructure as Infrastructure<br>and DB

participant Domain as Domain

loop Background monitoring execution by period
	activate Presentation
		Presentation ->> Application: ProcessMonitoring<br>ByPeriodAsync()
	deactivate Presentation
	
    activate Application
        Application->>Infrastructure: get current users identities from API snapshot read model
        activate Infrastructure
	        Infrastructure -->> Application: List<long> users snapshot from API
        deactivate Infrastructure

        Application->>Infrastructure: Get current subscribers identities from subscribers read model

        activate Infrastructure
        Infrastructure-->>Application: List<long> current users identities
        deactivate Infrastructure

        Application ->> Domain: Create difference between two sets of identities

        activate Domain
        Domain -->> Application: Joined and left participants identities
        deactivate Domain
       
		Application ->> Infrastructure: Get joined users if exist by identity from API read model
		activate Infrastructure
        Infrastructure-->>Application: List<Channel member> joined users
        deactivate Infrastructure

		Application ->> Infrastructure: Get left users if exist by identity from participants read model
		activate Infrastructure
        Infrastructure-->>Application: List<Channel member> left users
        deactivate Infrastructure

		Application ->> Application: Generate events

        Application ->> Infrastructure: Add events to event store
        activate Infrastructure
        deactivate Infrastructure
              
        Application ->> Infrastructure: Get pending events

        activate Infrastructure 
        Infrastructure -->> Application: pending events
        deactivate Infrastructure

        Application ->> Application: aggregate events by type. Trigger events

        Application ->> Infrastructure: Invoke: update subscribers read model according to events
        activate Infrastructure
        deactivate Infrastructure       

        Application ->> Presentation: Invoke: generate Telegram<br>message by events
		deactivate Application
		
        activate Presentation
        Presentation ->> User: Telegram<br/>notification
        deactivate Presentation
        
end
```

## Fetch users from API background service flow
That's how users fetch is processed

```mermaid

sequenceDiagram

participant Presentation
participant FetchUsersService as Fetch Users<br> Background Service
participant APIReadModel as API Users<br>Read Model
participant TelegramAPI


Presentation ->> FetchUsersService: Start()

activate FetchUsersService
FetchUsersService ->> FetchUsersService: Start background monitoring

loop Fetch users from API periodic execution
	FetchUsersService ->> TelegramAPI: Get all participants from API with safe delay between requests

	activate TelegramAPI

	TelegramAPI -->> FetchUsersService: List of current participants

	deactivate TelegramAPI

	FetchUsersService ->> APIReadModel: Add or update participants at read model
	activate APIReadModel
	deactivate APIReadModel
end

Presentation ->> FetchUsersService: StopAsync()
FetchUsersService ->> FetchUsersService: Stop background monitoring
deactivate FetchUsersService
```

## Background Events Snapshot Creation Flow


```mermaid

sequenceDiagram

box Application
	participant JobManager
	participant SnapshotCollectingJob
end

box Infrastructure
	participant EventsStore
	participant SnapshotsStore
end


JobManager ->> SnapshotCollectingJob: Schedule Snapshot<br>Collection Job
activate SnapshotCollectingJob

loop Fetch users from API periodic execution
	SnapshotCollectingJob ->> EventsStore: Get all events by current period
	EventsStore -->> SnapshotCollectingJob: List of current events
	
	SnapshotCollectingJob ->> SnapshotsStore: Get last snapshot
	SnapshotsStore -->> SnapshotCollectingJob: last snapshot

	SnapshotCollectingJob ->> SnapshotCollectingJob: create new snapshot

	SnapshotCollectingJob ->> SnapshotsStore: add new snapshot
	activate SnapshotsStore
	deactivate SnapshotsStore


JobManager ->> SnapshotCollectingJob: Pause Snapshot<br>Collection Job
end
deactivate SnapshotCollectingJob

```

## Setup and Deployment

This project can be deployed on any VPS/VDS server (or almost any). You need to clone the repository and set the necessary execute and read/write permissions for the folder.

Next, fill in your own data in the _appsettings.json_ file located in the _source/MonitoringBot/Configurations/_ directory (sample data is provided below):

```json
{
  "TelegramBot": {
    "BotToken": "your-bot-token",
    "ChatIdsCollection": [900000000],
    "CheckPeriodSeconds": 60
  },
  "TelegramApi": {
    "ApiId": "00000000",
    "ApiHash": "your-api-hash",
    "PhoneNumber": "+70000000000",
    "ChannelReferenceLink": "link to your public channel without t.me/"
  }
}
```

Fill the TelegramApi section according to the data obtained from the [official guide](https://core.telegram.org/api/obtaining_api_id).

The bot token is obtained when creating a bot via [BotFather](https://telegram.me/botfather).

To launch the bot, run the following command in the _path/to/project/source/MonitoringBot/_ directory:

```bash
dotnet run
```

> **❗ Warning!**  
>On first launch, you'll need to authorize via console by **entering the verification code** sent to the channel owner in Telegram. The session will then be saved to a _.session_ binary file in the output directory, and subsequent launches won't require additional authentication steps.