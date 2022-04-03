# YasES
YasES stands for "Yet another simple Event Store". It is a simple library for saving and reading events when doing Event Sourcing. 

The library is very much work in progress. There is no stable API yet.

## Examples
There are two simple examples projects inside `examples` folder which demonstrate the basics for this library.

* [The Simple Example](./examples/YasES.Examples.Simple/Program.cs) just shows how to interact with the storage (store events, read back events).
* [The Person List Example](./examples/YasES.Examples.PersonList/) shows an example implementation on how define commands, basic rule validations for commands, events and projections. 
