#time
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#load "MessageType.fs"
open MessageType
open System
open Akka.Actor
open Akka.FSharp
open Akka.Remote
open System.Collections.Generic
open Akka.Serialization

let config =  
    Configuration.parse
        @"akka {
            actor.serializers{
                json  = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                bytes = ""Akka.Serialization.ByteArraySerializer""

            }
            actor.serialization-bindings {
                ""System.Byte[]"" = bytes
                ""System.Object"" = json 
                
            }
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            
            remote.helios.tcp {
            hostname = 127.0.0.1
            port = 9001
            }

        }"

let system = System.create "RemoteFSharp" config
//let system = System.create "system" (Configuration.defaultConfig())
let sampleActor(mailbox:Actor<_>)=
    let rec loop()=actor{
        let! msg=mailbox.Receive();
        printfn "%A" msg
        return! loop()
    }
    loop()
let sample=spawne system "sample" <@ sampleActor @>[]
let Server(mailbox:Actor<_>)=
    let SubscriberList=new List<IActorRef>()
    let SubscribedTo=new Dictionary<IActorRef,HashSet<IActorRef>>()
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
                printfn "Sample Message"
                mailbox.Sender()<!"Done"
            |Register(actorRef)->
                printfn "Registered"
                RegisteredAccounts.Add(actorRef,true)
                SubscribedTo.Add(actorRef,null)
                let NodeRandom = new Random()
                let mutable ran = NodeRandom.Next(0,RegisteredAccounts.Count)
                while SubscribedTo.Item(actorRef).Count < ran do
                    let mutable subsRan=NodeRandom.Next(0,RegisteredAccounts.Count)
                    let tempActorRef=SubscriberList.Item(subsRan)
                    if((SubscribedTo.Item(actorRef).Contains(tempActorRef))=false) then
                        SubscribedTo.Item(actorRef).Add(SubscriberList.Item(subsRan))
                
                SubscriberList.Add(actorRef)
                mailbox.Sender()<!SubscriptionDone(actorRef)
                


            |TweetMsg(actorRef,tweetMsg)->
                tweetMsgMap.Item(actorRef).Add(tweetMsg)|>ignore
                let hTag=tweetMsg.HashTag
                for i in hTag do
                    hashTagMap.Item(i).Add(tweetMsg)|>ignore
                let mentions=tweetMsg.Mentions
                for i in mentions do
                    mentionsMap.Item(i).Add(tweetMsg)|>ignore
                printfn "TweetMessage"
            |Subscribe(actorRef)->
                SubscribedTo.Item(mailbox.Sender()).Add(actorRef)
                Subscribers.Item(actorRef).Add(mailbox.Sender())
                printfn "subscribe to a specific client"
            |QuerySubs->
                let tweetList=new List<Tweet>()
                let followingList=SubscribedTo.Item(mailbox.Sender())
                for i in followingList do
                    for j in tweetMsgMap.Item(i) do
                        tweetList.Add(j)
                    
                printfn "Query subscribers"
            |QueryTag(tag)->
                let tweetList=new List<Tweet>()
                for i in hashTagMap.Item(tag) do
                    tweetList.Add(i)
                printfn "Query tags"
            |QueryMentions(actorRef)->
                let tweetList=new List<Tweet>()
                for i in mentionsMap.Item(actorRef) do
                    tweetList.Add(i)
                printfn "Query Mentions"
            |Logout->
                RegisteredAccounts.Item(mailbox.Sender())=false|>ignore
                printfn "logout"
        return! loop()
    }
    loop()

let server = spawne system "Server" <@ Server @>[]
//let serv=system.ActorSelection("akka://system/user/Server")
//server<!Sample("hello")

printfn "%A" sample

//printfn "Waiting on port 9001"
// while true do
Console.ReadKey() |> ignore
0