#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#load "MessageType.fs"
#load "Server.fs"
open MessageType
//open Server
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Collections.Generic
open System.Diagnostics

let totalActors = 10
let actorList=new List<IActorRef>();

let config =  
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = 127.0.0.1
                port = 9001
            }
        }"

//let system = System.create "RemoteActorFactory" config
let system = System.create "system" (Configuration.defaultConfig())
let Client(mailbox:Actor<_>)=
    
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
            |Sample(s)->
                printfn "Sample message"
            |Register(actorRef)->
                printfn "Register this client"
            |TweetMsg(actorRef,tweetMsg)->
                printfn "TweetMessage"
            |Subscribe(actorRef)->
                printfn "subscribe to a specific client"
            |QuerySubs->
                printfn "Query subscribers"
            |QueryTag(tag)->
                printfn "Query tags"
            |QueryMentions(actorRef)->
                printfn "Query Mentions"
            |Logout->
                printfn "logout"

        return! loop()
    }
    loop()

let Simulator(mailbox:Actor<_>)=
    let rec loop()=actor{
        let! msg = mailbox.Receive()
        //match msg with
        //    |Simulate->
        //        printfn "start simulation"
        return! loop()
    }
    loop()


for i=1 to totalActors do
    actorList.Add(spawn system (string i) Client)
    Server.server<!Sample("hello")

Console.ReadKey()|>ignore
    
