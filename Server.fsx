#time
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: MathNet.Numerics"
#load "MessageType.fs"
open MathNet.Numerics
open MessageType
open System
open Akka.Actor
open Akka.FSharp
open Akka.Remote
open System.Collections.Generic
open Akka.Serialization




let mutable maxSubs=0;

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
            send-buffer-size = 5120000b
            receive-buffer-size = 5120000b
            maximum-frame-size = 1024000b
            tcp-keepalive = on
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
    let mutable SubscriberList=new List<IActorRef>()
    let mutable SubscribedTo=new Dictionary<IActorRef,HashSet<IActorRef>>()
    let mutable Subscribers=new Dictionary<IActorRef,HashSet<IActorRef>>()
    let mutable TweetDictionary=new Dictionary<IActorRef,List<Tweet>>()
    let mutable RegisteredAccounts=new Dictionary<IActorRef,bool>()
    let mutable tweetMsgMap = new Dictionary<IActorRef,HashSet<Tweet>>()
    let mutable mentionsMap = new Dictionary<IActorRef,HashSet<Tweet>>()
    let mutable hashTagMap = new Dictionary<String,HashSet<Tweet>>()
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
          
            |Sample(s)->
                printfn "Sample Message"
                mailbox.Sender()<!"Done"
            |Register(actorRef)->
                printfn "Registered"
                RegisteredAccounts.Add(actorRef,true)
                //let tempHash=new HashSet<IActorRef>()
                
                //SubscribedTo.Add(actorRef,tempHash)
                //let NodeRandom = new Random()
                //let mutable ran = NodeRandom.Next(0,(SubscriberList.Count/4))
                
                ////printfn "ran %i" ran
                //while SubscribedTo.Item(actorRef).Count < ran do
                //    //let mutable subsRan=NodeRandom.Next(0,(SubscriberList.Count/4))
                //    let mutable subsRan = Distributions.Zipf.Sample(0.0,SubscriberList.Count-1)
                //    //printfn "subsRan %i" subsRan
                //    printfn "list size %i" SubscriberList.Count
                //    let tempActorRef=SubscriberList.Item(subsRan)
                //    if((SubscribedTo.Item(actorRef).Contains(tempActorRef))=false) then
                //        SubscribedTo.Item(actorRef).Add(tempActorRef)|>ignore
                //        let mutable tempSubs=new HashSet<IActorRef>()
                //        if(Subscribers.ContainsKey(tempActorRef)=false)then
                //            Subscribers.Add(tempActorRef,tempSubs)
                //        Subscribers.Item(tempActorRef).Add(actorRef)|>ignore
                //        maxSubs<- max (Subscribers.Item(tempActorRef).Count) maxSubs
                SubscriberList.Add(actorRef)
                mailbox.Sender()<!SubscriptionDone(actorRef)
                


            |TweetMsg(actorRef,tweetMsg)->
                let tempMsgMap=new HashSet<Tweet>()
                let tempHashTagMap = new HashSet<Tweet>()
                let tempMentionMap = new HashSet<Tweet>()
                if(tweetMsgMap.ContainsKey(actorRef)=false) then
                    tweetMsgMap.Add(actorRef,tempMsgMap)
                
                tweetMsgMap.Item(actorRef).Add(tweetMsg)|>ignore
                let hTag=tweetMsg.HashTag
                for i in hTag do
                    if(hashTagMap.ContainsKey(i)=false)then
                        hashTagMap.Add(i,tempHashTagMap)
                    hashTagMap.Item(i).Add(tweetMsg)|>ignore
                let mentions=tweetMsg.Mentions
                for i in mentions do
                    if(mentionsMap.ContainsKey(i)=false)then
                        mentionsMap.Add(i,tempMentionMap)
                    mentionsMap.Item(i).Add(tweetMsg)|>ignore
                //printfn "TweetMessage %A" tweetMsg
            |Subscribe(num)->
                //let tempSt=new HashSet<IActorRef>()
                //let tempS=new HashSet<IActorRef>()
                //if(SubscribedTo.ContainsKey(mailbox.Sender())=false)then
                //    SubscribedTo.Add(mailbox.Sender(),tempSt)
                //let mutable subsRan = Distributions.Zipf.Sample(2.0,SubscriberList.Count-1)
                //while(SubscribedTo.Item(mailbox.Sender()).Contains(SubscriberList.Item(subsRan)))do
                //    subsRan <- Distributions.Zipf.Sample(2.0,SubscriberList.Count-1)
                //SubscribedTo.Item(mailbox.Sender()).Add(SubscriberList.Item(subsRan))|>ignore
                //if(Subscribers.ContainsKey(SubscriberList.Item(subsRan))=false)then
                //    Subscribers.Add(SubscriberList.Item(subsRan),tempS)
                //Subscribers.Item(SubscriberList.Item(subsRan)).Add(mailbox.Sender())|>ignore
                let NodeRandom = new Random()
                let tempSt=new HashSet<IActorRef>()
                let tempS=new HashSet<IActorRef>()
                if(Subscribers.ContainsKey(mailbox.Sender())=false)then
                    Subscribers.Add(mailbox.Sender(),tempS)
                while(Subscribers.Item(mailbox.Sender()).Count <=num)do
                    let tempRandom=NodeRandom.Next(0,SubscriberList.Count)
                    Subscribers.Item(mailbox.Sender()).Add(SubscriberList.Item(tempRandom))
                    if(SubscribedTo.ContainsKey(SubscriberList.Item(tempRandom))=false)then
                        SubscribedTo.Add(SubscriberList.Item(tempRandom),tempSt)
                    SubscribedTo.Item(SubscriberList.Item(tempRandom)).Add(mailbox.Sender())
                //printfn "subscribe to a specific client"
            |QuerySubs->
                let tweetList=new List<Tweet>()
                if(SubscribedTo.ContainsKey(mailbox.Sender())=true)then
                    let followingList=SubscribedTo.Item(mailbox.Sender())
                    for i in followingList do
                        if(tweetMsgMap.ContainsKey(i)<>false)then
                            for j in tweetMsgMap.Item(i) do
                                tweetList.Add(j)
                    //mailbox.Sender()<!PrintTweets(tweetList)    
                printfn "Query subscribers"
            |QueryTag(tag)->
                let tweetList=new List<Tweet>()
                if(hashTagMap.ContainsKey(tag)<>false)then
                    for i in hashTagMap.Item(tag) do
                        tweetList.Add(i)
                //mailbox.Sender()<!PrintTweets(tweetList) 
                printfn "Query tags"
            |QueryMentions(actorRef)->
                let tweetList=new List<Tweet>()
                if(mentionsMap.ContainsKey(actorRef)<>false)then
                    for i in mentionsMap.Item(actorRef) do
                        tweetList.Add(i)
                //mailbox.Sender()<!PrintTweets(tweetList) 
                printfn "Query Mentions"
            |Logout->
                RegisteredAccounts.Item(mailbox.Sender())=false|>ignore
                printfn "logout"
            |GetSubscriberRanksInfo->
                printfn "user   Subs"
                for i in Subscribers do
                    printfn "%i" (i.Value.Count)
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