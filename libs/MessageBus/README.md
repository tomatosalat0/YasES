# MessageBus
This library is a simple MessageBus implementation in .NET. The implementation uses an InMemory implementation for the message broker itself. The broker can be replaced by implementing a custom `IMessageBroker`. 

## Use Case
This library can be useful if you need a messaging system within your process but don't want to add a dependency to external services like RabbitMQ etc. The library has a very simple interface and should be easy and straight forward to use.

You might consider using this library if you want to migrated for a highly coupled monolith to a more decoupled system but don't want to add a library with a lot of additional dependencies and features. The goal of this library to do only one thing. 

## Installing and using the library
Currently there is no NuGet package available. It is on the roadmap to actually do that. For the moment, downloading the main code of the main branch and adding the project to your solution is the way to go. Its not the best way to do that for sure.

## Getting Started
### Event Firing and Handling
Within this example, we will create a simple event, fire it and react on it.

#### 1: Define your event
By defining an event, you specify which parameters are passed in that event.
```cs
[Topic("Events/MyEvent")]
public class MyEvent : IMessageEvent
{
    public MessageId MessageId { get; } = MessageId.NewId();
}
```
Each event must implement the `IMessageEvent` interface which adds one property: `MessageId`. This id helps us to uniquely identify the message later on.  

Additionally, you must specify the topic name for that event. To do that, you have to add the `Topic` attribute to the class itself and define a string name for that topic. You can name your topic however you want, but each event must have a unique name within your system. 

#### 2. Register an Event Handler
After you have defined your event, you should add a handler which executes when this event happens. You don't need to register an event handler to be able to a fire a event - when firing an event, you don't care if anyone is actually doing something because of it. 

```cs
using IMessageBus bus = new MessageBrokerMessageBus(
    MemoryMessageBrokerBuilder.InProcessBroker(), 
    NoExceptionNotification.Instance
);
bus.RegisterEventDelegate<MyEvent>(e => Console.WriteLine($"Received MyEvent"));
```

#### 3. Firing an event
After you have registered your event handler, you can fire it. 
```cs
using IMessageBus bus = // ...
bus.Register // ...

await bus.FireEvent(new MyEvent());
```
The `await bus.FireEvent` will return as soon as the event has been scheduled but before any handler might get executed. The execution and firing is decoupled. If the event handler fires an exception, you will not notice it when you call `await bus.fireEvent`.

This quick example should help you get started using this library.

## Example Projects
Within the `examples` folder, you'll find a few projects with different examples in it. These examples are a bit longer compared to the simple example above, but should help you see different stuff in action.

## Naming
Just to clear things up, here are the most important name schemas used within the library
* A `Message` is an abstract object containing data. It is considered immutable after creation. 
* A `Handler` is a piece of code which gets executed when a message arrives. 
* An `Event`, `Command`, `Query` or `Rpc` are message types. While they are all messages, the describe the intend. 
* An `EventHandler`, `CommandHandler`, `QueryHandler` or `RpcHandler` are handler implementations for the corresponding message types.
* The `MessageBroker` is the system responsible receiving messages and executing the corresponding handler. The message broker only knows about two types of messages: broadcast messages (defined as events within the scope of the broker) and work messages (defined as commands within the scope of the broker). 
    * Broadcast messages will get forwarded to all currently registered handles. If a handler throws an exception, it will get ignored.
    * Work messages will get forward to the first registered handler which is currently not handling a previous work message. If all registered handlers are busy, the work message will get forwarded to the first handler which gets available. If an exception within a single handler occurs, the message will get scheduled again. 
* The `MessageBus` is the main entry point for interacting with the system. This class introduces the concepts of events, commands, queries and rpc-calls. The message bus uses the message broker internally for the message propagation.  

## Limitations

### Work message re-scheduling
A work message will get re-scheduled if an exception during its processing occurs. This re-scheduling doesn't have a limit at the moment. That implementation is on the roadmap. . You should take care of exception handling within the handler itself to avoid that problem for the time being. 

## Library behavior and implementation considerations
The library itself is designed to be used within concurrent systems. Every method can be considered thread safe unless it is explicitly specified as non-thread-safe within the method documentation. 

### Execution order
When you fire a message, it will get added to an internal queue of scheduled messages. The `Fire` methods will return as soon as the message has been added to that queue. When a `Fire` method returns, you don't know if the handler for that message was already executed, is currently executing your message or will get executed shortly afterwards. 

### Handler
Each message handler will not get executed concurrently. Because of that, a handler is not required to do any locking. If you want to be able to handle multiple queries in parallel for example, you can simply register multiple instances for each query. This allows you to explicitly define how many parallel executions can happen in your system. If you want to allow a single handler instance to execute multiple events in parallel, you can use the extension methods `WithParallelExecution` .

Example:
```cs
public class MyCommand : IMessageCommand
{
    public MessageId MessageId { get; } = MessageId.NewId();
}

public class MyCommandHandler : IAsyncMessageCommandHandler<MyCommand>
{
    public Task HandleAsync(MyCommand command)
    {
        // this method will get executed in parallel if muliple commands are
        // waiting for execution.
    }
}

// ...

bus.RegisterCommandHandler(
    new MyCommandHandler().WithParallelExecution()
);
```

### Message instances
The system will not (de)serialize the received messages by itself at the moment. Instead the same reference of the message is forwarded to the handler. This currently allows you to basically add anything you want to the message body itself. However this is not recommended at all.  The following recommendation should be used for all messages: 
* All your message body properties should be serializable.
* Do not pass references around, stick to simple data types like `string`, `int` or `IReadOnlyDictionary` which are serializable to a JSON. That decreases coupling between system.
* Messages should be immutable and must be completely loaded when broadcasting them. Do not use any `Lazy<T>` property etc.
* If you keep your message immutable, you don't have to care about locks etc.
* While a message can be any object, it is wise to create a dedicated class which only contains the properties you actually need within the event/command/query/...

### Timeouts
Every message types which returns a result has a `CancellationToken` parameter. You should not use `CancellationToken.None` here. If no handler is registered for your message, each of these messages will not return until the provided cancellation token has timed out - which will be never in case of `CancellationToken.None` or `default(CancellationToken)`. You should define a good timeout for each query and define that explicitly within your code. 

### Timeout and handler
If you fire a message through a method which has an `CancellationToken` parameter, the token won't get passed to the handler itself. So the provided token gets cancelled, the message will get passed to the handler and will get processed there. You can not cancel the scheduling or the execution of a message itself, you only specify a cancellation token for the wait process itself. If you need to be able to cancel a message itself, you need to implement that by yourself.

### IDisposable
#### Subscription IDisposed return value
Every `Subscribe` method returns an `IDisposable`. These disposables are used to be able to remove the subscription later on. If you execute the `Dispose` method of the returned object, the handler itself won't get disposed - it will only get unregistered. If you have a handler which can be removed during runtime, read the next chapter for details

#### When a handler needs to be removable from the MessageBus
If you create a handler which you want to be able to remove from the message bus, you should do the following:
* Add and implement the `IDisposable` interface to your handler.
* Add and implement the `ISubscriptionAwareHandler` interface. This interface has one method called `RegisterSubscription`. This one gets called by the MessageBus when you register an instance of that handler. When registering that handler, ignore the returned `IDisposable` instance.
* Inside your handler, add each received `IDisposable` from the `RegisterSubscription` method to a private list
* Inside the actual Dispose method of the handler, dispose all `IDisposable` instances you have within that private list.
* Throw an `ObjectDisposedException` within the `Handle` method to ensure that a received message will get forwarded to a different handler.
* To unregister your handler then, just dispose it.

Example: 
```cs
public class MyCommandHandler : 
    IAsyncMessageCommandHandler<MyCommand>, 
    ISubscriptionAwareHandler, 
    IDisposable
{
    // will contain each disposable received in RegisterSubscription
    private readonly List<IDisposable> _registerdSubscriptions = new List<IDisposable>();
    private bool _disposedValue;

    public async Task HandleAsync(MyCommand command)
    {
        if (_disposedValue) throw new ObjectDisposedException(nameof(MyCommandHandler));

        // your code
    }

    public void RegisterSubscription(IDisposable subscription) 
        => _registerdSubscriptions.Add(subscription);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var p in _registerdSubscriptions)
                    p.Dispose();
                _registerdSubscriptions.Clear();
            }
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

// ...

// Registering
MyCommandHandler handler = new MyCommandHandler();
bus.RegisterCommandHandler(handler);

// ...

// Unregister
handler.Dispose();

```

## Concepts
### Message Types
The `IMessageBus` interfaces separates messages into four categories:
* Events
* Commands
* Queries
* RPC

Technically these four types can be splitted into two categories:
* Events and Commands will not return anything
* Queries and RPC have a return value

The methods were named by the concept behind it and not by the implementation details. This should help you to express your ident better when using the library and if you read code of others using the library as well.

#### Events
Events are a simple notification system. You can have none, one or multiple handlers for a single event simultaneously in your system. When an event is fired, each currently registered event handler will get executed for each fired event. It shouldn't matter if anyone is currently listening to the event you just fired. 

#### Commands
Commands are used to perform state mutation within your system. A command should include every parameter needed within its body. A command is passed to the first currently available command handler. A single command only gets handled by a single handler, even if multiple handlers for the same command type are currently registered within a system. While you won't get any error if no command handler is currently registered for the command you just fired, you should make sure that each command has a handler by yourself.

While there is a method called `FireCommandAndWait`, you should not use that. You should design your system that you don't need to wait for a command to finish. If you actually need to wait for a command to complete, fire an event after completion within the command handler or use rpc-messages.

#### Queries
Queries are used to fetch data from a module within your system. Semantically, a query will not the state of any module. You should stick to this meaning which helps you later on. A query will get executed by the first currently available query handler. A query handler produces a single query result and returns that back to the sender. 

If no handler for that query is registered, the method will return when the provided `CancellationToken` gets raised. If you provided `CancellationToken.None` for that, the method will never return in that case. 

### RPC
An RPC message is a special generic type which doesn't tell the user how the underlying system will behave in terms of state changes etc. While this one can be used instead of commands and queries, it is not something you should use. It will help you if you want to migrate your current synchronous system to a message oriented one, but it is considered a code smell if you want to use something like CQRS.