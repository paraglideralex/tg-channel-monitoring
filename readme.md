# Channel members monitoring telegram-bot

## Subscribers change processing

```mermaid
sequenceDiagram
	actor User

	box Presentation 
    
    participant MessagesSendingService as Messages sending<br>service
    end

	box Application
	   participant SubscribersMonitoringService as Subscribers<br>MonitoringService
    participant EventsMonitoringProcessor as Events monitoring<br>processor
    participant SubscribersChangeProcessor as Subscribers change<br>processor
    end
    
	box Infrastructure and DB
    participant TgApi as Telegram<br>API
    participant UsersReadModel as Users<br>read model
    participant EventsStorage as Events<br>Storage
    end

	box Domain
	participant ChangeDetector as Entities Change<br>Detector
	end
  

    activate SubscribersMonitoringService

	    SubscribersMonitoringService->>TgApi: FetchUsersBackgroundService.GetSnapshot()
	    activate TgApi
		TgApi -->> SubscribersMonitoringService: List<ChannelMember> users snapshot from API
		deactivate TgApi
	
	    SubscribersMonitoringService->>UsersReadModel: GetAllCurrentSubscribersQuery.ExecuteAsync()
	    activate UsersReadModel
	    UsersReadModel-->>SubscribersMonitoringService: List<ChannelMember> current users
	    deactivate UsersReadModel
	
		SubscribersMonitoringService ->> ChangeDetector: ProduceEvents(apiUsers, databaseUsers, channelReference) generates subscribe/unsubscribe events
		activate ChangeDetector
		ChangeDetector -->> SubscribersMonitoringService: IReadOnlyCollection<EntitiesChangedDomainEventBase<TEntity>> events
		deactivate ChangeDetector
	
		SubscribersMonitoringService ->> EventsStorage: AddEventsCommand.ExecuteAsync(events)
	deactivate SubscribersMonitoringService
		activate EventsStorage
		deactivate EventsStorage
		EventsMonitoringProcessor ->> EventsStorage: GetEventsInPeriodQueryExecution.GetEventsByPeriodAsync(events)
		activate EventsStorage

		activate EventsMonitoringProcessor
		EventsStorage -->> EventsMonitoringProcessor: events
		deactivate EventsStorage
	
		EventsMonitoringProcessor ->> SubscribersChangeProcessor: aggregated<br/>by type events
		activate SubscribersChangeProcessor
		SubscribersChangeProcessor ->> UsersReadModel: AddOrUpdateSubscribersCommand.ExecuteAsync<br/>(EntitiesCollectionChangedEventArgs<ChannelMember> aggregatedEvent)
		activate UsersReadModel
		deactivate UsersReadModel
		deactivate SubscribersChangeProcessor
	
		EventsMonitoringProcessor ->> MessagesSendingService: aggregated<br/>by type events
		deactivate EventsMonitoringProcessor
		activate MessagesSendingService
		MessagesSendingService ->> User: Telegram<br/>notification
		deactivate MessagesSendingService

		
	

```