module Server

open System
open Akka.Actor
open Akka.FSharp
open System.Collections.Generic

//let config =  
//    Configuration.parse
//        @"akka {
//            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
//        }"

//let system = ActorSystem.Create("ActorFactory", config) 
// Have all clients ip addresses to share the work
(*let clients = ["192.168.0.36";"127.0.0.1"]*) //
let system = System.create "system" (Configuration.defaultConfig())
type Tweet=
    |TweetMsg of String
    |HashTag of List<String>
    |Mentions of List<IActorRef>

type TweeterEngine =
    |Sample of String
    |Register of IActorRef
    |TweetMsg of IActorRef * Tweet
    |Subscribe of IActorRef
    |QuerySubs
    |QueryTag of String
    |QueryMentions of IActorRef
    |Logout
let Server(mailbox:Actor<_>)=
    let SubscribedTo=new Dictionary<IActorRef,List<IActorRef>>()
    let Subscribers=new Dictionary<IActorRef,List<IActorRef>>()
    let TweetDictionary=new Dictionary<IActorRef,List<Tweet>>()
    let RegisteredAccounts=new Dictionary<IActorRef,bool>()
    let tweetMsgMap = new Dictionary<IActorRef,HashSet<Tweet>>()
    let mentionsMap = new Dictionary<IActorRef,HashSet<Tweet>>()
    let hashTagMap = new Dictionary<String,HashSet<Tweet>>()
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
            |Sample(s)->
                printfn "Sample Message %s" s
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

let server = spawn system "Server" Server

printfn "%A" server

Console.ReadKey()|>ignore